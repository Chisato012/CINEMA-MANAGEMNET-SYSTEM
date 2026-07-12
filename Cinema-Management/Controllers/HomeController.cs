using System.Diagnostics;
using System.Globalization;
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
    private readonly IOfferService _offerService;

    public HomeController(ApplicationDbContext context, IOfferService offerService)
    {
        _context = context;
        _offerService = offerService;
    }

    public IActionResult Index()
    {
        // Truy vấn dữ liệu và map sang View Model
        var movies = _context.Movies
            .Select(m => new MovieViewModel
            {
                MovieId = m.MovieId,
                Title = m.Title,
                Duration = m.Duration,
                PosterURL = m.PosterURL,
                // Gom tên các thể loại nối với nhau bằng dấu phẩy
                Genre = string.Join(", ", m.MovieGenres.Select(mg => mg.Genre.Name))
            })
            .ToList();

        // Gửi danh sách này sang View
        return View(movies);
    }

    public IActionResult Movie()
    {
        var movies = _context.Movies
            .OrderBy(m => m.Title)
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
            .ToList();

        return View(movies);
    }

    public IActionResult Details(int id)
    {
        var movie = _context.Movies
            .Where(m => m.MovieId == id)
            .Select(m => new MovieViewModel
            {
                // VẾ TRÁI (MovieViewModel) = VẾ PHẢI (Entity/Database)
                MovieId = m.MovieId,
                Title = m.Title,
                Duration = m.Duration,
                PosterURL = m.PosterURL,
                ReleaseDate = m.ReleaseDate,
                AgeRating = m.AgeRating,
                Synopsis = m.Synopsis,
                Trailer = m.Trailer,

                Showtimes = m.Showtimes,

                // Load thông tin từ 3 bảng khác
                Language = m.Language,
                Country = m.Country,

                // Format 
                Genre = string.Join(", ", m.MovieGenres.Select(mg => mg.Genre.Name)),
                MovieDirector = string.Join(", ", m.MovieDirectors.Select(md => md.Person.FullName)),
                MovieCast = string.Join(", ", m.MovieCasts.Select(mc => mc.Person.FullName))
            })
            .FirstOrDefault();

        if (movie == null)
        {
            return NotFound();
        }

        return View(movie);
    }

    public IActionResult Offers()
    {
        var today = DateTime.Today;
        var offers = _offerService.GetOffers(today);

        var model = new OffersPageViewModel
        {
            Offers = offers,
            FeaturedOffers = offers
                .Where(offer => offer.IsFeatured && offer.Status == "active")
                .Take(4)
                .ToList(),
            ExpiringSoonOffers = offers
                .Where(offer => offer.IsExpiringSoon)
                .OrderBy(offer => offer.EndDate)
                .ToList(),
            QuickBookingMovies = BuildQuickBookingMovies(today)
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult ValidateOfferCode(string? code)
    {
        var result = _offerService.ValidateCode(code, DateTime.Today);

        return Json(new
        {
            result.IsValid,
            result.Status,
            result.Message,
            Offer = result.Offer == null
                ? null
                : new
                {
                    result.Offer.Id,
                    result.Offer.Title,
                    result.Offer.Code,
                    result.Offer.DisplayValue,
                    result.Offer.ValidityLabel,
                    result.Offer.Summary
                }
        });
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


    // Giá vé Controller
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
