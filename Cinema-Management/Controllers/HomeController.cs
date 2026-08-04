using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Cinema_Management.Models;
using Cinema_Management.Data;
using Cinema_Management.Services;
using Microsoft.EntityFrameworkCore;

namespace Cinema_Management.Controllers;

public class HomeController : Controller
{

    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
         
        var movies = _context.Movies
            .Select(m => new MovieViewModel
            {
                MovieId = m.MovieId,
                Title = m.Title,
                Duration = m.Duration,
                PosterURL = m.PosterURL,
                ReleaseDate = m.ReleaseDate,
                 
                Genre = string.Join(", ", m.MovieGenres.Select(mg => mg.Genre.Name))
            })
            .ToList();

        ApplyReviewSummaries(movies);

         
        return View(movies);
    }

    public IActionResult Movie()
    {
        var today = DateTime.Today;
        var freshMovieIds = _context.Movies
            .AsNoTracking()
            .OrderBy(_ => Guid.NewGuid())
            .Select(m => m.MovieId)
            .Take(4)
            .ToList();

        var movieOrder = freshMovieIds
            .Select((movieId, index) => new { movieId, index })
            .ToDictionary(item => item.movieId, item => item.index);

        var movies = BuildMovieQuery()
            .Where(m => freshMovieIds.Contains(m.MovieId))
            .ToList()
            .OrderBy(m => movieOrder.TryGetValue(m.MovieId, out var index) ? index : int.MaxValue)
            .Select(MapMovie)
            .ToList();

        var allNowShowingMovies = BuildMovieQuery()
            .Where(m => m.ReleaseDate <= today)
            .OrderBy(m => m.Title)
            .ToList()
            .Select(MapMovie)
            .ToList();

        var allComingSoonMovies = BuildMovieQuery()
            .Where(m => m.ReleaseDate > today)
            .OrderBy(m => m.ReleaseDate)
            .ThenBy(m => m.Title)
            .ToList()
            .Select(MapMovie)
            .ToList();

        ApplyReviewSummaries(movies);
        ApplyReviewSummaries(allNowShowingMovies);
        ApplyReviewSummaries(allComingSoonMovies);

        ViewBag.AllNowShowingMovies = allNowShowingMovies.Any()
            ? allNowShowingMovies
            : movies.OrderBy(movie => movie.Title).ToList();
        ViewBag.AllComingSoonMovies = allComingSoonMovies;

        return View(movies);
    }

    public IActionResult Details(int id)
    {
        var movie = _context.Movies
            .Where(m => m.MovieId == id)
            .Select(m => new MovieViewModel
            {
                 
                MovieId = m.MovieId,
                Title = m.Title,
                Duration = m.Duration,
                PosterURL = m.PosterURL,
                ReleaseDate = m.ReleaseDate,
                AgeRating = m.AgeRating,
                Synopsis = m.Synopsis,
                Trailer = m.Trailer,

                Showtimes = m.Showtimes,

                 
                Language = m.Language,
                Country = m.Country,

                 
                Genre = string.Join(", ", m.MovieGenres.Select(mg => mg.Genre.Name)),
                MovieDirector = string.Join(", ", m.MovieDirectors.Select(md => md.Person.FullName)),
                MovieCast = string.Join(", ", m.MovieCasts.Select(mc => mc.Person.FullName))
            })
            .FirstOrDefault();

        if (movie == null)
        {
            return NotFound();
        }

        ApplyReviewSummaries(new[] { movie });
        movie.Reviews = BuildMovieReviews(movie.MovieId);
        ViewBag.CanReview = ResolveCurrentUserId().HasValue;

        return View(movie);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddReview(MovieReviewFormViewModel model)
    {
        var userId = ResolveCurrentUserId();
        var movieId = model.MovieId;
        var comment = Request.Form["Comment"].ToString().Trim();
        var ratingText = Request.Form["Rating"].ToString().Trim();

        if (userId == null)
        {
            TempData["AlertError"] = "Vui lòng đăng nhập để gửi đánh giá phim.";
            return RedirectToAction(nameof(Details), new { id = movieId });
        }

        if (!_context.Movies.Any(movie => movie.MovieId == movieId))
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(comment)
            || !TryParseReviewRating(ratingText, out var rating))
        {
            TempData["AlertError"] = "Vui lòng chọn điểm đánh giá và nhập nội dung trước khi gửi.";
            return RedirectToAction(nameof(Details), new { id = movieId });
        }

        rating = Math.Round(rating, 2);
        if (rating < 0m || rating > 5m)
        {
            TempData["AlertError"] = "Điểm đánh giá phải nằm trong khoảng từ 0 đến 5.";
            return RedirectToAction(nameof(Details), new { id = movieId });
        }

        var review = new Review
        {
            MovieID = movieId,
            UserID = userId.Value,
            ParentReviewID = null,
            Content = comment,
            Rating = rating,
            CreatedAt = DateTime.UtcNow,
            Status = "Visible"
        };

        _context.Reviews.Add(review);

        try
        {
            _context.SaveChanges();
            TempData["AlertSuccess"] = "Đánh giá của bạn đã được gửi.";
        }
        catch (DbUpdateException)
        {
            TempData["AlertError"] = "Chưa thể lưu đánh giá. Vui lòng thử lại.";
        }

        return RedirectToAction(nameof(Details), new { id = movieId });
    }

    

   
    private IQueryable<MovieViewModel> BuildMovieQuery()
    {
        return _context.Movies
            .AsNoTracking()
            .AsSplitQuery()
            .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
            .Include(m => m.MovieDirectors)
                .ThenInclude(md => md.Person)
            .Include(m => m.MovieCasts)
                .ThenInclude(mc => mc.Person)
            .Include(m => m.Showtimes)
                .ThenInclude(s => s.Room)
            .Include(m => m.Language)
            .Include(m => m.Country);
    }

    private static MovieViewModel MapMovie(MovieViewModel movie)
    {
        return new MovieViewModel
        {
            MovieId = movie.MovieId,
            Title = movie.Title,
            Duration = movie.Duration,
            PosterURL = movie.PosterURL,
            ReleaseDate = movie.ReleaseDate,
            AgeRating = movie.AgeRating,
            Synopsis = movie.Synopsis,
            Trailer = movie.Trailer,
            Showtimes = movie.Showtimes
                .OrderBy(showtime => showtime.Date)
                .ThenBy(showtime => showtime.StartTime)
                .ToList(),
            Language = movie.Language,
            Country = movie.Country,
            Genre = string.Join(", ", movie.MovieGenres.Select(movieGenre => movieGenre.Genre.Name)),
            MovieDirector = string.Join(", ", movie.MovieDirectors.Select(movieDirector => movieDirector.Person.FullName)),
            MovieCast = string.Join(", ", movie.MovieCasts.Select(movieCast => movieCast.Person.FullName)),
            AverageRating = movie.AverageRating,
            ReviewCount = movie.ReviewCount,
            ReviewSummary = movie.ReviewSummary
        };
    }

    private static bool TryParseReviewRating(string ratingText, out decimal rating)
    {
        if (decimal.TryParse(
                ratingText,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out rating))
        {
            return true;
        }

        return decimal.TryParse(
            ratingText,
            NumberStyles.Number,
            CultureInfo.CurrentCulture,
            out rating);
    }

    private int? ResolveCurrentUserId()
    {
        var sessionUserId = HttpContext.Session.GetInt32("UserID");
        if (sessionUserId.HasValue && UserExists(sessionUserId.Value))
        {
            return sessionUserId.Value;
        }

        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(claimValue, out var claimUserId) && UserExists(claimUserId))
        {
            return claimUserId;
        }

        return null;
    }

    private bool UserExists(int userId)
    {
        return _context.Users.Any(user => user.UserID == userId && user.Status);
    }

    private void ApplyReviewSummaries(IEnumerable<MovieViewModel> movies)
    {
        var movieList = movies
            .Where(movie => movie != null)
            .ToList();

        if (!movieList.Any())
        {
            return;
        }

        var summaries = BuildReviewSummaryLookup(movieList.Select(movie => movie.MovieId));

        foreach (var movie in movieList)
        {
            summaries.TryGetValue(movie.MovieId, out var summary);
            summary ??= new MovieReviewSummaryViewModel();

            movie.ReviewSummary = summary;
            movie.AverageRating = summary.AverageRating;
            movie.ReviewCount = summary.TotalRatings;
        }
    }

    private Dictionary<int, MovieReviewSummaryViewModel> BuildReviewSummaryLookup(IEnumerable<int> movieIds)
    {
        var ids = movieIds
            .Distinct()
            .ToList();

        if (!ids.Any())
        {
            return new Dictionary<int, MovieReviewSummaryViewModel>();
        }

        return _context.Reviews
            .AsNoTracking()
            .Where(review =>
                ids.Contains(review.MovieID)
                && review.ParentReviewID == null
                && review.Status == "Visible"
                && review.Rating.HasValue)
            .GroupBy(review => review.MovieID)
            .Select(group => new
            {
                MovieId = group.Key,
                AverageRating = group.Average(review => review.Rating!.Value),
                TotalRatings = group.Count()
            })
            .ToList()
            .ToDictionary(
                row => row.MovieId,
                row => new MovieReviewSummaryViewModel
                {
                    AverageRating = Math.Round(row.AverageRating, 2),
                    TotalRatings = row.TotalRatings
                });
    }

    private List<MovieReviewViewModel> BuildMovieReviews(int movieId)
    {
        return _context.Reviews
            .AsNoTracking()
            .Include(review => review.User)
            .Where(review =>
                review.MovieID == movieId
                && review.ParentReviewID == null
                && review.Status == "Visible"
                && review.Rating.HasValue)
            .OrderByDescending(review => review.CreatedAt)
            .Select(review => new MovieReviewViewModel
            {
                ReviewID = review.ReviewID,
                Rating = review.Rating!.Value,
                Comment = review.Content,
                CreatedAt = review.CreatedAt,
                UserFullName = review.User != null ? review.User.FullName : "COSMOS member"
            })
            .ToList();
    }

    private List<OfferQuickBookingMovieViewModel> BuildQuickBookingMovies(DateTime today)
    {
        var lastDate = today.AddDays(13);

        return _context.Showtimes
            .AsNoTracking()
            .Include(showtime => showtime.Movie)
            .Include(showtime => showtime.Room)
            .Where(showtime => showtime.Date >= today && showtime.Date <= lastDate)
            .OrderBy(showtime => showtime.Movie!.Title)
            .ThenBy(showtime => showtime.Date)
            .ThenBy(showtime => showtime.StartTime)
            .ToList()
            .Where(showtime => showtime.Movie != null)
            .GroupBy(showtime => showtime.MovieID)
            .Select(group => new OfferQuickBookingMovieViewModel
            {
                MovieId = group.Key,
                Title = group.First().Movie!.Title,
                Showtimes = group.Select(showtime => new OfferQuickBookingShowtimeViewModel
                {
                    ShowtimeId = showtime.ShowtimeID,
                    Date = showtime.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    DateLabel = showtime.Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                    Time = showtime.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture),
                    Format = ResolveQuickBookingFormat(showtime),
                    RoomName = showtime.Room?.RoomName ?? $"Phòng {showtime.RoomID}"
                }).ToList()
            })
            .ToList();
    }

    private static string ResolveQuickBookingFormat(Showtimes showtime)
    {
        var roomName = showtime.Room?.RoomName ?? string.Empty;
        return roomName.Contains("IMAX", StringComparison.OrdinalIgnoreCase) ? "IMAX 2D" : "2D";
    }


     
    public IActionResult TicketPricing()
    {
        var viewModel = new TicketPricingViewModel
        {
            MovieFormats =
            [
                new MoviePricingViewModel
                {
                    Id = "2D",
                    TabLabel = "Phim 2D",
                    SeatPrices =
                    [
                        new SeatPricingViewModel
                        {
                            SeatType = SeatType.Standard,
                            BasePrice = 65_000,
                            NormalDay = 65_000,
                            WeekendOrHoliday = 85_000
                        },
                        new SeatPricingViewModel
                        {
                            SeatType = SeatType.Vip,
                            BasePrice = 97_500,
                            NormalDay = 97_500,
                            WeekendOrHoliday = 127_500
                        },
                        new SeatPricingViewModel
                        {
                            SeatType = SeatType.Sweetbox,
                            BasePrice = 162_500,
                            NormalDay = 162_500,
                            WeekendOrHoliday = 212_500
                        }
                    ]
                },
                new MoviePricingViewModel
                {
                    Id = "IMAX",
                    TabLabel = "Phim IMAX",
                    SeatPrices =
                    [
                        new SeatPricingViewModel
                        {
                            SeatType = SeatType.Standard,
                            BasePrice = 135_000,
                            NormalDay = 135_000,
                            WeekendOrHoliday = 165_000
                        },
                        new SeatPricingViewModel
                        {
                            SeatType = SeatType.Vip,
                            BasePrice = 202_500,
                            NormalDay = 202_500,
                            WeekendOrHoliday = 247_500
                        },
                        new SeatPricingViewModel
                        {
                            SeatType = SeatType.Sweetbox,
                            BasePrice = 337_500,
                            NormalDay = 337_500,
                            WeekendOrHoliday = 412_500
                        }
                    ]
                }
            ],
            Footnotes =
            [
                "Giá vé định dạng IMAX phụ thu thêm 50.000đ tùy hạng ghế.",
                "Sweetbox là giá vé dành cho 2 người.",
                "Trẻ em dưới 1m3 được giảm 20.000đ/vé (Chỉ áp dụng mua tại quầy)."
            ]
        };

        return View("~/Views/Home/TicketPricing.cshtml", viewModel);
    }


}
