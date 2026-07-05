using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Cinema_Management.Models;
using Cinema_Management.Data;
using Microsoft.EntityFrameworkCore;

namespace Cinema_Management.Controllers;

public class HomeController : Controller
{
    private const string SessionKey = "CosmosCinema.Booking";
    private const int MaximumSeats = 8;
    private static readonly JsonSerializerOptions SessionJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
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

        if(movie == null)
        {
            return NotFound();
        }

        return View(movie);
    }

    [HttpGet]
    public IActionResult Booking(int? movieId, int? showtimeId)
    {
        if (movieId is null && showtimeId is null && string.IsNullOrWhiteSpace(HttpContext.Session.GetString(SessionKey)))
        {
            TempData["AlertError"] = "Hãy chọn phim trước khi đặt vé!";
            return RedirectToAction(nameof(Index));
        }

        var booking = GetOrCreateBooking(movieId, showtimeId);
        if (IsExpired(booking))
        {
            HttpContext.Session.Remove(SessionKey);
            TempData["AlertError"] = "Phiên đặt vé đã hết hạn. Vui lòng chọn lại phim.";
            return RedirectToAction(nameof(Index));
        }

        return View(booking);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveSchedule([FromBody] ScheduleRequest request)
    {
        if (!TryGetActiveBooking(out var booking))
        {
            return StatusCode(440, new { message = "Phiên đặt vé đã hết hạn. Vui lòng chọn lại phim.", redirectUrl = Url.Action(nameof(Index), "Home") });
        }

        if (!DateOnly.TryParseExact(request.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date) || date < DateOnly.FromDateTime(DateTime.Today))
        {
            return BadRequest(new { message = "Please select a valid future show date." });
        }

        var showtime = _context.Showtimes
            .Include(s => s.Room)
            .Where(s => s.MovieID == booking.MovieId && s.Date == date.ToDateTime(TimeOnly.MinValue))
            .AsEnumerable()
            .FirstOrDefault(s =>
                s.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture) == request.Time &&
                GetRoomFormatLabel(s.Room) == request.Format);

        if (showtime is null)
        {
            return BadRequest(new { message = "The selected screening is not available." });
        }

        var screeningChanged = booking.ShowtimeId != showtime.ShowtimeID;

        booking.ShowtimeId = showtime.ShowtimeID;
        booking.SelectedDate = request.Date;
        booking.SelectedTime = request.Time;
        booking.CinemaFormat = request.Format;
        booking.OccupiedSeats = LoadOccupiedSeats(showtime.ShowtimeID);
        booking.CurrentStep = 2;

        if (screeningChanged)
        {
            booking.SelectedSeats.Clear();
        }

        SaveBooking(booking);
        return Json(Snapshot(booking));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleSeat([FromBody] SeatRequest request)
    {
        if (!TryGetActiveBooking(out var booking))
        {
            return StatusCode(440, new { message = "Phiên đặt vé đã hết hạn. Vui lòng chọn lại phim.", redirectUrl = Url.Action(nameof(Index), "Home") });
        }

        var seatId = request.SeatId?.Trim().ToUpperInvariant() ?? string.Empty;
        var roomId = GetSelectedRoomId(booking);

        if (roomId is null || !SeatExists(roomId.Value, seatId))
        {
            return BadRequest(new { message = "That seat does not exist." });
        }

        if (booking.IsPaid)
        {
            return BadRequest(new { message = "This booking has already been paid." });
        }

        if (booking.ShowtimeId is not null)
        {
            booking.OccupiedSeats = LoadOccupiedSeats(booking.ShowtimeId.Value);
        }

        if (booking.OccupiedSeats.Contains(seatId, StringComparer.OrdinalIgnoreCase))
        {
            return Conflict(new { message = $"Seat {seatId} has already been taken." });
        }

        if (!booking.SelectedSeats.Remove(seatId))
        {
            if (booking.SelectedSeats.Count >= MaximumSeats)
            {
                return BadRequest(new { message = $"A booking can contain at most {MaximumSeats} seats." });
            }

            booking.SelectedSeats.Add(seatId);
            booking.SelectedSeats = booking.SelectedSeats
                .OrderBy(value => value[0])
                .ThenBy(value => int.Parse(value[1..], CultureInfo.InvariantCulture))
                .ToList();
        }

        booking.CurrentStep = Math.Max(booking.CurrentStep, 2);
        SaveBooking(booking);
        return Json(Snapshot(booking));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateConcession([FromBody] ConcessionRequest request)
    {
        if (!TryGetActiveBooking(out var booking))
        {
            return StatusCode(440, new { message = "Phiên đặt vé đã hết hạn. Vui lòng chọn lại phim.", redirectUrl = Url.Action(nameof(Index), "Home") });
        }

        if (request.Quantity is < 0 or > 10)
        {
            return BadRequest(new { message = "Quantity must be between 0 and 10." });
        }

        if (booking.SelectedSeats.Count == 0)
        {
            return BadRequest(new { message = "Select at least one seat before adding concessions." });
        }

        var item = booking.Concessions.SingleOrDefault(item =>
            string.Equals(item.Id, request.ProductId, StringComparison.Ordinal));
        if (item is null)
        {
            return NotFound(new { message = "Concession item not found." });
        }

        item.Quantity = request.Quantity;
        booking.CurrentStep = 3;
        SaveBooking(booking);
        return Json(Snapshot(booking));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetStep([FromBody] StepRequest request)
    {
        if (!TryGetActiveBooking(out var booking))
        {
            return StatusCode(440, new { message = "Phiên đặt vé đã hết hạn. Vui lòng chọn lại phim.", redirectUrl = Url.Action(nameof(Index), "Home") });
        }

        if (request.Step is < 1 or > 4)
        {
            return BadRequest(new { message = "Invalid booking step." });
        }

        if (request.Step >= 3 && booking.SelectedSeats.Count == 0)
        {
            return BadRequest(new { message = "Select at least one seat to continue." });
        }

        booking.CurrentStep = request.Step;
        SaveBooking(booking);
        return Json(Snapshot(booking));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ConfirmPayment()
    {
        if (!TryGetActiveBooking(out var booking))
        {
            return StatusCode(440, new { message = "Phiên đặt vé đã hết hạn. Vui lòng chọn lại phim.", redirectUrl = Url.Action(nameof(Index), "Home") });
        }

        if (booking.SelectedSeats.Count == 0)
        {
            return BadRequest(new { message = "Your booking has no seats." });
        }

        if (!booking.IsPaid)
        {
            booking.IsPaid = true;
            booking.CurrentStep = 4;
            booking.ConfirmationNumber = $"COS-{DateTime.UtcNow:yyyyMMdd}-{RandomNumberGenerator.GetInt32(100000, 999999)}";
            SaveBooking(booking);
        }

        return Json(new
        {
            success = true,
            booking.ConfirmationNumber,
            total = booking.GrandTotal,
            message = "Payment confirmed. Your tickets are ready."
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Cancel()
    {
        HttpContext.Session.Remove(SessionKey);
        return Json(new { success = true, redirectUrl = Url.Action(nameof(Index), "Home") });
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
        
    // Booking Controller
    private BookingViewModel GetOrCreateBooking(int? movieId = null, int? showtimeId = null)
    {
        var json = HttpContext.Session.GetString(SessionKey);
        if (!string.IsNullOrWhiteSpace(json))
        {
            var existing = JsonSerializer.Deserialize<BookingViewModel>(json, SessionJsonOptions);
            if (existing is not null && movieId is null && showtimeId is null)
            {
                if (IsExpired(existing))
                {
                    HttpContext.Session.Remove(SessionKey);
                    return existing;
                }

                RefreshBookingChoices(existing);
                SaveBooking(existing);
                return existing;
            }

            if (existing is not null &&
                (movieId is null || existing.MovieId == movieId) &&
                (showtimeId is null || existing.ShowtimeId == showtimeId))
            {
                if (IsExpired(existing))
                {
                    HttpContext.Session.Remove(SessionKey);
                    if (movieId is null && showtimeId is null)
                    {
                        return existing;
                    }

                    return BuildBookingFromDatabase(movieId, showtimeId);
                }

                RefreshBookingChoices(existing);
                SaveBooking(existing);
                return existing;
            }
        }

        var booking = BuildBookingFromDatabase(movieId, showtimeId);
        SaveBooking(booking);
        return booking;
    }

    private BookingViewModel BuildBookingFromDatabase(int? movieId, int? showtimeId)
    {
        var today = DateTime.Today;
        var showtimeQuery = _context.Showtimes
            .Include(s => s.Movie)
                .ThenInclude(m => m!.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
            .Include(s => s.Movie)
                .ThenInclude(m => m!.MovieCasts)
                    .ThenInclude(mc => mc.Person)
            .Include(s => s.Movie)
                .ThenInclude(m => m!.MovieDirectors)
                    .ThenInclude(md => md.Person)
            .Include(s => s.Room)
            .Where(s => s.Date >= today);

        if (showtimeId is not null)
        {
            showtimeQuery = showtimeQuery.Where(s => s.ShowtimeID == showtimeId);
        }
        else if (movieId is not null)
        {
            showtimeQuery = showtimeQuery.Where(s => s.MovieID == movieId);
        }

        var showtime = showtimeQuery
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .FirstOrDefault();

        var movie = showtime?.Movie;
        if (movie is null && movieId is not null)
        {
            movie = _context.Movies
                .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
                .Include(m => m.MovieCasts).ThenInclude(mc => mc.Person)
                .Include(m => m.MovieDirectors).ThenInclude(md => md.Person)
                .FirstOrDefault(m => m.MovieId == movieId);
        }

        if (movie is null)
        {
            return new BookingViewModel();
        }

        var booking = new BookingViewModel
        {
            BookingExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
            MovieId = movie.MovieId,
            ShowtimeId = showtime?.ShowtimeID,
            MovieTitle = movie.Title,
            Director = string.Join(", ", (movie.MovieDirectors ?? []).Select(md => md.Person.FullName)),
            Cast = string.Join(", ", (movie.MovieCasts ?? []).Select(mc => mc.Person.FullName)),
            Synopsis = movie.Synopsis,
            AgeRating = movie.AgeRating,
            DurationMinutes = movie.Duration,
            PosterURL = movie.PosterURL,
            Genre = string.Join(", ", (movie.MovieGenres ?? []).Select(mg => mg.Genre.Name)),
            SelectedDate = (showtime?.Date ?? today).ToString("yyyy-MM-dd"),
            SelectedTime = showtime?.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture) ?? string.Empty,
            CinemaFormat = GetRoomFormatLabel(showtime?.Room),
            OccupiedSeats = showtime is null ? [] : LoadOccupiedSeats(showtime.ShowtimeID),
            Concessions = LoadConcessions()
        };

        ApplyPrices(booking, showtime);
        RefreshBookingChoices(booking);

        return booking;
    }

    private void RefreshBookingChoices(BookingViewModel booking)
    {
        var today = DateTime.Today;
        var showtimes = _context.Showtimes
            .Include(s => s.Room)
            .Where(s => s.MovieID == booking.MovieId && s.Date >= today)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .ToList();

        booking.AvailableDates = showtimes
            .Select(s => s.Date.ToString("yyyy-MM-dd"))
            .Distinct()
            .ToList();

        booking.AvailableTimes = showtimes
            .Select(s => s.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture))
            .Distinct()
            .ToList();

        booking.AvailableFormats = showtimes
            .Select(s => GetRoomFormatLabel(s.Room))
            .Distinct()
            .ToList();

        booking.ShowtimeChoices = showtimes
            .Select(s => new ShowtimeChoiceViewModel
            {
                ShowtimeId = s.ShowtimeID,
                Date = s.Date.ToString("yyyy-MM-dd"),
                Time = s.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture),
                Format = GetRoomFormatLabel(s.Room)
            })
            .ToList();

        if (booking.ShowtimeId is not null)
        {
            booking.OccupiedSeats = LoadOccupiedSeats(booking.ShowtimeId.Value);
        }

        if (booking.Concessions.Count == 0)
        {
            booking.Concessions = LoadConcessions();
        }
    }

    private void ApplyPrices(BookingViewModel booking, Showtimes? showtime)
    {
        if (showtime is null)
        {
            return;
        }

        var pricing = _context.SeatTypePricings.ToDictionary(item => item.SeatType, item => item.Multiplier);
        var regularMultiplier = pricing.GetValueOrDefault("Regular", 1m);
        var coupleMultiplier = pricing.GetValueOrDefault("Couple", 1.6m);

        booking.StandardTicketPrice = showtime.BasePrice * regularMultiplier;
        booking.SweetboxTicketPrice = showtime.BasePrice * coupleMultiplier;
    }

    private List<ConcessionItemViewModel> LoadConcessions()
    {
        var combos = _context.Combos
            .OrderBy(c => c.ComboName)
            .ToList();

        return combos.Count == 0
            ? new BookingViewModel().Concessions
            : combos.Select(combo => new ConcessionItemViewModel
            {
                Id = combo.ComboID.ToString(CultureInfo.InvariantCulture),
                Name = combo.ComboName,
                Description = "Cinema combo",
                Icon = string.Empty,
                Price = combo.ComboPrice
            }).ToList();
    }

    private int? GetSelectedRoomId(BookingViewModel booking) =>
        booking.ShowtimeId is null
            ? null
            : _context.Showtimes
                .Where(s => s.ShowtimeID == booking.ShowtimeId.Value)
                .Select(s => (int?)s.RoomID)
                .FirstOrDefault();

    private bool SeatExists(int roomId, string seatId) =>
        _context.Seats.Any(seat => seat.RoomID == roomId && seat.SeatCode == seatId);

    private List<string> LoadOccupiedSeats(int showtimeId) =>
        _context.Tickets
            .Include(ticket => ticket.Seat)
            .Include(ticket => ticket.Booking)
            .Where(ticket => ticket.ShowtimeID == showtimeId && ticket.Booking != null && ticket.Booking.Status != "Cancelled")
            .Select(ticket => ticket.Seat!.SeatCode)
            .ToList();

    private void SaveBooking(BookingViewModel booking) =>
        HttpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(booking, SessionJsonOptions));

    private bool TryGetActiveBooking(out BookingViewModel booking)
    {
        booking = GetOrCreateBooking();
        if (!IsExpired(booking))
        {
            return true;
        }

        HttpContext.Session.Remove(SessionKey);
        return false;
    }

    private static bool IsExpired(BookingViewModel booking) =>
        booking.BookingExpiresAtUtc <= DateTime.UtcNow;

    private static string GetRoomFormatLabel(Room? room)
    {
        var roomName = room?.RoomName ?? string.Empty;
        if (string.IsNullOrWhiteSpace(roomName))
        {
            return "STANDARD";
        }

        if (roomName.Contains("IMAX", StringComparison.OrdinalIgnoreCase))
        {
            return "IMAX";
        }

        if (roomName.Contains("VIP", StringComparison.OrdinalIgnoreCase))
        {
            return "VIP";
        }

        if (roomName.Contains("Deluxe", StringComparison.OrdinalIgnoreCase))
        {
            return "DELUXE";
        }

        if (roomName.Contains("2D", StringComparison.OrdinalIgnoreCase))
        {
            return "2D";
        }

        if (roomName.Contains("3D", StringComparison.OrdinalIgnoreCase))
        {
            return "3D";
        }

        return roomName;
    }

    private static object Snapshot(BookingViewModel booking) => new
    {
        currentStep = booking.CurrentStep,
        bookingExpiresAtUtc = booking.BookingExpiresAtUtc,
        selectedDate = booking.SelectedDate,
        selectedTime = booking.SelectedTime,
        cinemaFormat = booking.CinemaFormat,
        selectedSeats = booking.SelectedSeats,
        standardTicketPrice = booking.StandardTicketPrice,
        sweetboxTicketPrice = booking.SweetboxTicketPrice,
        ticketSubtotal = booking.TicketSubtotal,
        convenienceFee = booking.ConvenienceFee,
        concessions = booking.Concessions.Select(item => new
        {
            item.Id,
            item.Name,
            item.Quantity,
            lineTotal = item.Price * item.Quantity
        }),
        concessionSubtotal = booking.ConcessionSubtotal,
        preTaxTotal = booking.PreTaxTotal,
        tax = booking.Tax,
        grandTotal = booking.GrandTotal
    };
}
