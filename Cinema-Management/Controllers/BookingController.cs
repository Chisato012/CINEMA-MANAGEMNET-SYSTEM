using Cinema_Management.Data;
using Cinema_Management.Models;
using Cinema_Management.Models.Sepay;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json; // Dùng để chuyển danh sách combo thành JSON khi lưu Session.
using System.Globalization;
using System.Text.RegularExpressions;

namespace Cinema_Management.Controllers;

public class BookingController : Controller
{
    private static readonly Regex PaymentReferenceRegex = new(
        @"(?<![A-Z0-9])COSMOS(?:\d{20}|[A-F0-9]{26})(?![A-Z0-9])",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly JsonSerializerOptions WebhookJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;


    //Khai báo biến _context để truy cập vào cơ sở dữ liệu thông qua ApplicationDbContext
    public BookingController(
        ApplicationDbContext context,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _context = context;
        _environment = environment;
        _configuration = configuration;

    }
    //===== CODE INDEX CỦA QUANG ==========
    public IActionResult Index(int? movieId, DateTime? date)
    {
        var today = DateTime.Today;
        var selectedDate = date?.Date;
        var lastDate = today.AddDays(13);

        var showtimeQuery = _context.Showtimes
            .AsNoTracking()
            .AsSplitQuery()
            .Include(s => s.Movie!)
                .ThenInclude(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
            .Include(s => s.Movie!)
                .ThenInclude(m => m.Language)
            .Include(s => s.Room!)
                .ThenInclude(r => r.Seats)
            .Include(s => s.Tickets)
            .Where(s => s.Date == today);

        if (movieId.HasValue)
        {
            showtimeQuery = showtimeQuery.Where(s => s.MovieID == movieId.Value);
        }

        var showtimes = showtimeQuery
            .OrderBy(s => s.Movie!.Title)
            .ThenBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .ToList();

        var movieFilterTitle = movieId.HasValue
            ? _context.Movies
                .AsNoTracking()
                .Where(movie => movie.MovieId == movieId.Value)
                .Select(movie => movie.Title)
                .FirstOrDefault()
            : null;

        if (movieId.HasValue && string.IsNullOrWhiteSpace(movieFilterTitle))
        {
            return NotFound();
        }

        var model = new BookingSchedulePageViewModel
        {
            Today = today,
            SelectedDate = selectedDate,
            MovieId = movieId,
            MovieFilterTitle = movieFilterTitle,
            DateOptions = Enumerable.Range(0, 14)
                .Select(offset => BuildDateOption(today.AddDays(offset), today, selectedDate))
                .ToList(),
            Movies = showtimes
                .Where(s => s.Movie != null)
                .GroupBy(s => s.MovieID)
                .Select(group =>
                {
                    var movie = group.First().Movie!;
                    var scheduleItems = group.Select(BuildScheduleShowtime).ToList();

                    return new BookingScheduleMovieViewModel
                    {
                        MovieId = movie.MovieId,
                        Title = movie.Title,
                        PosterUrl = string.IsNullOrWhiteSpace(movie.PosterURL)
                            ? "/img/poster/poster1.png"
                            : movie.PosterURL,
                        Genres = string.Join(", ", movie.MovieGenres.Select(mg => mg.Genre.Name)),
                        DurationMinutes = movie.Duration,
                        AgeRating = movie.AgeRating,
                        Formats = scheduleItems
                            .Select(item => item.Format)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList(),
                        Showtimes = scheduleItems
                    };
                })
                .OrderBy(movie => movie.Title)
                .ToList()
        };

        return View(model);
    }

    // GET: Booking/SelectShowtime dùng để fill dữ liệu lên UI
    private static BookingScheduleDateOption BuildDateOption(DateTime date, DateTime today, DateTime? selectedDate)
    {
        return new BookingScheduleDateOption
        {
            Date = date,
            Value = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateLabel = date.ToString("dd/MM", CultureInfo.InvariantCulture),
            WeekdayLabel = WeekdayLabel(date),
            IsToday = date.Date == today.Date,
            IsSelected = selectedDate.HasValue && date.Date == selectedDate.Value.Date
        };
    }

    private static BookingScheduleShowtimeViewModel BuildScheduleShowtime(Showtimes showtime)
    {
        var totalSeats = showtime.Room?.Seats.Count ?? 0;
        var remainingSeats = Math.Max(0, totalSeats - showtime.Tickets.Count);
        var isLate = showtime.StartTime.TimeOfDay >= TimeSpan.FromHours(22);
        var isLowAvailability = remainingSeats > 0 && (remainingSeats <= 20 || (totalSeats > 0 && remainingSeats <= totalSeats * 0.15));

        return new BookingScheduleShowtimeViewModel
        {
            ShowtimeId = showtime.ShowtimeID,
            MovieId = showtime.MovieID,
            Date = showtime.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateLabel = showtime.Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            Time = showtime.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture),
            Format = ResolveFormat(showtime),
            RoomName = showtime.Room?.RoomName ?? $"Phòng {showtime.RoomID}",
            RemainingSeats = remainingSeats,
            TotalSeats = totalSeats,
            IsLate = isLate,
            IsLowAvailability = isLowAvailability,
            IsSoldOut = remainingSeats <= 0,
            AvailabilityLabel = remainingSeats <= 0
                ? "Hết vé"
                : isLowAvailability
                    ? "Còn ít ghế"
                    : $"{remainingSeats} ghế trống"
        };
    }

    private static string ResolveFormat(Showtimes showtime)
    {
        var roomName = showtime.Room?.RoomName ?? string.Empty;
        var languageName = showtime.Movie?.Language?.LanguageName ?? string.Empty;
        var presentation = roomName.Contains("IMAX", StringComparison.OrdinalIgnoreCase) ? "IMAX 2D" : "2D";
        var voice = languageName.Contains("vi", StringComparison.OrdinalIgnoreCase)
                    || languageName.Contains("viet", StringComparison.OrdinalIgnoreCase)
                    || languageName.Contains("việt", StringComparison.OrdinalIgnoreCase)
            ? "LỒNG TIẾNG"
            : "PHỤ ĐỀ";

        return $"{presentation} {voice}";
    }

    private static string WeekdayLabel(DateTime date)
    {
        return date.DayOfWeek switch
        {
            DayOfWeek.Monday => "T2",
            DayOfWeek.Tuesday => "T3",
            DayOfWeek.Wednesday => "T4",
            DayOfWeek.Thursday => "T5",
            DayOfWeek.Friday => "T6",
            DayOfWeek.Saturday => "T7",
            _ => "CN"
        };
    }


    //====== CODE CÁC BƯỚC THANH TOÁN CỦA AN =========
    public IActionResult SelectShowtime(int movieId)
    {
        // Truy vấn thông tin phim
        var movie = _context.Movies
            .Where(m => m.MovieId == movieId)
            .Select(m => new BookingViewModel
            {
                MovieId = m.MovieId,
                MovieTitle = m.Title,
                Synopsis = m.Synopsis,
                AgeRating = m.AgeRating,
                DurationMinutes = m.Duration,
                PosterURL = m.PosterURL,
                Genre = string.Join(", ", m.MovieGenres.Select(mg => mg.Genre.Name)),
                Director = string.Join(",", m.MovieDirectors.Select(md => md.Person.FullName)), //truy vấn thông tin đạo diễn từ model MovieDirectors và nối các tên đạo diễn bằng dấu phẩy
                Cast = string.Join(",", m.MovieCasts.Select(mc => mc.Person.FullName)) //truy vấn thông tin diễn viên từ model MovieCasts và nối các tên diễn viên bằng dấu

            })
            .FirstOrDefault();

        if (movie == null)
        {
            return NotFound();
        }

        // Truy vấn thông tin các suất chiếu của phim
        var showtimes = _context.Showtimes
            .Where(s => s.MovieID == movieId && s.Date >= DateTime.Today)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .ToList();

        movie.AvailableFormats = new List<string> { "2D" };

        //Lưu vào model các suất chiếu có sẵn để hiển thị cho người dùng chọn
        movie.ShowtimeChoices = showtimes.Select(s => new ShowtimeChoiceViewModel
        {
            ShowtimeId = s.ShowtimeID,
            Date = s.Date.ToString("yyyy-MM-dd"),
            Time = s.StartTime.ToString("HH:mm"),
            Format = "2D" //Giả sử tất cả các suất chiếu đều là 2D, nếu có nhiều định dạng khác nhau thì cần truy vấn từ cơ sở dữ liệu
        }).ToList();

        //Các ngày có thể có suất chiếu của phim
        movie.AvailableDates = movie.ShowtimeChoices.Select(s => s.Date).Distinct().ToList();
        movie.SelectedDate = string.Empty;

        //Các giờ có thể có suất chiếu của phim dựa trên ngày đã chọn
        movie.AvailableTimes = movie.ShowtimeChoices
            .Where(s => s.Date == movie.SelectedDate) //Lấy ra ngày đc chọn để lọc ra các suất chiếu có cùng ngày
            .Select(s => s.Time)
            .Distinct()
            .ToList();

        movie.SelectedTime = movie.AvailableTimes.FirstOrDefault() ?? string.Empty;
        movie.CinemaFormat = movie.AvailableFormats.FirstOrDefault() ?? string.Empty;

        movie.ShowtimeId = null;

        //Gửi thông tin phim đến view booking để hiển thị thông tin phim và cho phép người dùng đặt vé
        return View(movie);
    }

    //Post: Booking/SelectShowtime
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SelectShowtime(SelectShowtimeRequest request)
    {
        var showtime = _context.Showtimes
            .FirstOrDefault(s => s.ShowtimeID == request.ShowtimeId && s.MovieID == request.MovieId);

        if (showtime == null)
        {
            return NotFound();
        }

        HttpContext.Session.SetInt32("SelectedShowtimeId", showtime.ShowtimeID);
        HttpContext.Session.SetInt32("SelectedMovieId", request.MovieId);
        HttpContext.Session.SetString("SelectedFormat", request.Format);
        HttpContext.Session.SetString("SelectedDate", showtime.Date.ToString("yyyy-MM-dd"));
        HttpContext.Session.SetString("SelectedTime", showtime.StartTime.ToString("HH:mm"));
        if (!string.IsNullOrWhiteSpace(request.OfferCode))
        {
            HttpContext.Session.SetString("SelectedOfferCode", request.OfferCode.Trim().ToUpperInvariant());
        }
        else
        {
            HttpContext.Session.Remove("SelectedOfferCode");
        }
        return RedirectToAction("SelectSeats");
    }

    //Get 
    public IActionResult SelectSeats()
    {

        //Lấy ra các thông tin đã lưu trong session để hiển thị cho người dùng chọn ghế
        var selectedShowtimeId = HttpContext.Session.GetInt32("SelectedShowtimeId");
        var selectedMovieId = HttpContext.Session.GetInt32("SelectedMovieId");
        var selectedFormat = HttpContext.Session.GetString("SelectedFormat");
        var selectedDate = HttpContext.Session.GetString("SelectedDate");
        var selectedTime = HttpContext.Session.GetString("SelectedTime");
        var selectedOfferCode = HttpContext.Session.GetString("SelectedOfferCode");

        if (selectedShowtimeId == null || selectedMovieId == null || string.IsNullOrEmpty(selectedFormat) ||
            string.IsNullOrEmpty(selectedDate) || string.IsNullOrEmpty(selectedTime))
        {
            return RedirectToAction("SelectShowtime", new { movieId = selectedMovieId });
        }

        //Lấy ra showtime có room có ShowtimeID giống nhau
        var showtime = _context.Showtimes.Include(s => s.Room).FirstOrDefault(s => s.ShowtimeID == selectedShowtimeId);

        var roomId = showtime.RoomID;

        //lấy danh sách ghế ở trong Room
        var seats = _context.Seats.Where(s => s.RoomID == roomId).OrderBy(s => s.SeatCode).ToList();

        //Laasy ticket thuộc suất chiếu này để bt ghế nào đã bị lấy
        var occupiedSeatCodes = _context.Tickets
        .Where(t => t.ShowtimeID == selectedShowtimeId.Value)
        .Select(t => t.Seat.SeatCode)
        .ToList();


        var seatTypePricing = _context.SeatTypePricings.ToDictionary(st => st.SeatType, st => st.Multiplier);

        //lấy ra thông tin gửi sang step-2
        var model = _context.Movies.Where(m => m.MovieId == selectedMovieId.Value)
            .Select(m => new BookingViewModel
            {
                MovieId = m.MovieId,
                MovieTitle = m.Title,
                SelectedDate = selectedDate,
                SelectedTime = selectedTime,
                CinemaFormat = selectedFormat,
                ShowtimeId = selectedShowtimeId,
                OfferCode = selectedOfferCode,

                SeatChoices = seats.Select(seats => new SeatChoiceViewModel
                {
                    SeatId = seats.SeatID,
                    SeatCode = seats.SeatCode,
                    SeatType = seats.SeatType,
                    IsOccupied = occupiedSeatCodes.Contains(seats.SeatCode),
                    Price = showtime.BasePrice * (seatTypePricing.ContainsKey(seats.SeatType) ? seatTypePricing[seats.SeatType] : 1.00m),
                    IsSelected = false
                }).ToList(),

                OccupiedSeats = occupiedSeatCodes
            }).FirstOrDefault();

        if (model == null)
        {
            return NotFound();
        }
        return View(model);
    }

    //Post: Booking/SelectSeats nhận các ghế đc chọn
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SelectSeats(List<string> selectedSeats)
    {
        //lấy lại showtimeId và movieId từ session để xác nhận thông tin
        var selectedShowtimeId = HttpContext.Session.GetInt32("SelectedShowtimeId");
        var selectedMovieId = HttpContext.Session.GetInt32("SelectedMovieId");

        //Lấy suất chiếu từ cơ sở dữ liệu dựa trên showtimeId và movieId
        var showtime = _context.Showtimes.Include(s => s.Room).FirstOrDefault(s => s.ShowtimeID == selectedShowtimeId && s.MovieID == selectedMovieId);
        //Lấy danh sách ghế trong phòng chiếu
        var seats = _context.Seats.Where(s => s.RoomID == showtime.RoomID).OrderBy(s => s.SeatCode).ToList();
        //Lấy danh sách ghế đã được đặt trong suất chiếu này
        var occupiedSeatCodes = _context.Tickets
            .Where(t => t.ShowtimeID == selectedShowtimeId.Value)
            .Select(t => t.Seat.SeatCode)
            .ToList();

        //Ghế đc chọn
        selectedSeats = selectedSeats
        .Where(code => seats.Any(s => s.SeatCode == code))
        .Where(code => !occupiedSeatCodes.Contains(code))
        .Distinct()
        .ToList();

        //Lấy ra seatTypePricing để tính giá vé dựa trên loại ghế
        var seatTypePricing = _context.SeatTypePricings.ToDictionary(st => st.SeatType, st => st.Multiplier);


        var model = _context.Movies
            .Where(m => m.MovieId == selectedMovieId.Value)
            .Select(m => new BookingViewModel
            {
                //Lấy ra các thông tin
                MovieId = m.MovieId,
                MovieTitle = m.Title,

                SelectedDate = HttpContext.Session.GetString("SelectedDate"),
                SelectedTime = HttpContext.Session.GetString("SelectedTime"),
                CinemaFormat = HttpContext.Session.GetString("SelectedFormat"),
                OfferCode = HttpContext.Session.GetString("SelectedOfferCode"),
                ShowtimeId = selectedShowtimeId,

                //Gán ghế đã chọn
                SelectedSeats = selectedSeats,

                //Gán giá vé
                StandardTicketPrice = showtime.BasePrice * (seatTypePricing.ContainsKey("Regular") ? seatTypePricing["Regular"] : 1.00m),
                VipTicketPrice = showtime.BasePrice * (seatTypePricing.ContainsKey("VIP") ? seatTypePricing["VIP"] : 1.00m),
                SweetboxTicketPrice = showtime.BasePrice * (seatTypePricing.ContainsKey("Couple") ? seatTypePricing["Couple"] : 1.00m),

                //Tạo ra các SeatChoices
                SeatChoices = seats.Select(seat => new SeatChoiceViewModel
                {
                    SeatId = seat.SeatID,
                    SeatCode = seat.SeatCode,
                    SeatType = seat.SeatType,
                    IsOccupied = occupiedSeatCodes.Contains(seat.SeatCode),
                    Price = showtime.BasePrice * (seatTypePricing.ContainsKey(seat.SeatType) ? seatTypePricing[seat.SeatType] : 1.00m), //Lấy ra gia vé
                    //
                    IsSelected = selectedSeats.Contains(seat.SeatCode),

                }).ToList(),

                OccupiedSeats = occupiedSeatCodes

            }).FirstOrDefault();

        if (model == null)
        {
            return NotFound();
        }

        HttpContext.Session.SetString("SelectedSeats", string.Join(",", selectedSeats));


        return RedirectToAction("SelectConcessions");
    }


    //Get thông tin gửi đến view tương ứng
    public IActionResult SelectConcessions()
    {

        //Lấy ra lại các thông tin từ session
        var selectedMovieId = HttpContext.Session.GetInt32("SelectedMovieId");
        var selectedShowtimeId = HttpContext.Session.GetInt32("SelectedShowtimeId");
        var selectedDate = HttpContext.Session.GetString("SelectedDate");
        var selectedTime = HttpContext.Session.GetString("SelectedTime");
        var selectedFormat = HttpContext.Session.GetString("SelectedFormat");
        var selectedSeatsRaw = HttpContext.Session.GetString("SelectedSeats");

        //Nếu chưa chọn phim
        if (!selectedMovieId.HasValue || !selectedShowtimeId.HasValue)
        {
            return RedirectToAction("Index", "Home");
        }

        //Nếu chưa chọn ghế
        if (string.IsNullOrWhiteSpace(selectedSeatsRaw))
        {
            return RedirectToAction(nameof(SelectSeats));
        }

        //Chuyển chuỗi các Code ghế 
        var selectedSeats = selectedSeatsRaw
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .ToList();



        //Lấy ra suất chiếu
        var showtime = _context.Showtimes.Include(s => s.Room)
            .FirstOrDefault(s => s.MovieID == selectedMovieId.Value && s.ShowtimeID == selectedShowtimeId.Value);

        if (showtime == null)
        {
            return NotFound();
        }

        //Lấy ra các ghế
        var seats = _context.Seats.Where(s => s.RoomID == showtime.RoomID)
            .OrderBy(s => s.SeatCode).ToList();

        //Các ghế đã chọn
        var occupiedSeatCodes = _context.Tickets.Where(t => t.ShowtimeID == selectedShowtimeId.Value)
            .Select(t => t.Seat.SeatCode)
            .ToList();

        //Giá x cho từng loại ghế
        var seatTypePricing = _context.SeatTypePricings.ToDictionary(st => st.SeatType, st => st.Multiplier);

        //Combo
        // Đọc danh sách combo đã chọn trước đó từ Session, nếu người dùng quay lại Step 3.
        var selectedComboJson = HttpContext.Session.GetString("SelectedConcessions");

        // Chuyển JSON thành danh sách; nếu Session chưa có thì tạo danh sách rỗng.
        var savedSelections = string.IsNullOrWhiteSpace(selectedComboJson)
            ? new List<ConcessionRequest>()
            : JsonSerializer.Deserialize<List<ConcessionRequest>>(selectedComboJson) ?? [];

        // Tạo Dictionary để tìm số lượng đã chọn nhanh theo ComboId.
        var savedQuantities = savedSelections
            .GroupBy(item => item.ComboId)
            .ToDictionary(group => group.Key, group => group.First().Quantity);

        var concession = _context.Combos.AsNoTracking().OrderBy(c => c.ComboName)
            .ToList().Select(combo => new ConcessionItemViewModel
            {
                Id = combo.ComboID,                     // Mã combo lấy từ DB.
                Name = combo.ComboName,                 // Tên combo lấy từ DB.
                Price = combo.ComboPrice,               // Giá luôn lấy từ DB.
                SelectedQuantity = savedQuantities      // Khôi phục số lượng nếu đã chọn.
                    .GetValueOrDefault(combo.ComboID, 0)
            }).ToList();

        var model = _context.Movies.Where(m => m.MovieId == selectedMovieId.Value)
            .Select(s => new BookingViewModel
            {
                MovieId = s.MovieId,
                MovieTitle = s.Title,

                SelectedDate = selectedDate ?? "",
                SelectedTime = selectedTime ?? "",
                CinemaFormat = selectedFormat ?? "2D",
                SelectedSeats = selectedSeats,
                ShowtimeId = selectedShowtimeId,
                OccupiedSeats = occupiedSeatCodes,

                //Gán giá vé
                StandardTicketPrice = showtime.BasePrice * (seatTypePricing.ContainsKey("Regular") ? seatTypePricing["Regular"] : 1.00m),
                VipTicketPrice = showtime.BasePrice * (seatTypePricing.ContainsKey("VIP") ? seatTypePricing["VIP"] : 1.00m),
                SweetboxTicketPrice = showtime.BasePrice * (seatTypePricing.ContainsKey("Couple") ? seatTypePricing["Couple"] : 1.00m),

                SeatChoices = seats.Select(seat => new SeatChoiceViewModel
                {
                    SeatId = seat.SeatID,
                    SeatCode = seat.SeatCode,
                    SeatType = seat.SeatType,
                    IsOccupied = occupiedSeatCodes.Contains(seat.SeatCode),
                    IsSelected = selectedSeats.Contains(seat.SeatCode),
                    //lấy ra giá dựa vào baseprice x với chỉ số ghế với seattype là 1 dictionary
                    Price = showtime.BasePrice * (seatTypePricing.ContainsKey(seat.SeatType) ? seatTypePricing[seat.SeatType] : 1.00m),
                }).ToList(),

                Concessions = concession


            }).FirstOrDefault();
        if (model == null)
        {
            return NotFound();
        }

        return View(model);

    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    //Post cho Chọn đồ ăn
    public IActionResult SelectConcessions(SelectConcessionsRequest request)
    {
        // Kiểm tra hành trình đặt vé vẫn còn trong Session.
        var selectedShowtimeId = HttpContext.Session.GetInt32("SelectedShowtimeId");
        var selectedSeatsRaw = HttpContext.Session.GetString("SelectedSeats");

        if (!selectedShowtimeId.HasValue || string.IsNullOrWhiteSpace(selectedSeatsRaw))
        {
            return RedirectToAction(nameof(SelectSeats));
        }

        //Chuẩn hoá dữ liệu và gửi lên combo
        var requestedItems = request.Items
            .Where(item => item.ComboId > 0 && item.Quantity > 0)
            .GroupBy(item => item.ComboId)
            .Select(item => new ConcessionRequest
            {
                ComboId = item.Key, //Bằng Key trong từ điển
                Quantity = Math.Clamp(item.First().Quantity, 1, 10)

            }).ToList();

        // Chỉ chấp nhận những ComboId thật sự tồn tại trong DB.
        var requestedIds = requestedItems.Select(item => item.ComboId).ToList(); //Lấy ra các ID
        var validComboIds = _context.Combos
            .Where(combo => requestedIds.Contains(combo.ComboID))
            .Select(combo => combo.ComboID)
            .ToHashSet();

        var validSelections = requestedItems
            .Where(item => validComboIds.Contains(item.ComboId))
            .ToList();

        // Chỉ lưu mã combo và số lượng vào Session
        var selectedComboJson = JsonSerializer.Serialize(validSelections);
        HttpContext.Session.SetString("SelectedConcessions", selectedComboJson);

        return RedirectToAction("Checkout");
    }

    //Get checkout
    public IActionResult Checkout()
    {
        //Lấy ra lại các thông tin từ session
        var selectedMovieId = HttpContext.Session.GetInt32("SelectedMovieId");
        var selectedShowtimeId = HttpContext.Session.GetInt32("SelectedShowtimeId");
        var selectedDate = HttpContext.Session.GetString("SelectedDate");
        var selectedTime = HttpContext.Session.GetString("SelectedTime");
        var selectedFormat = HttpContext.Session.GetString("SelectedFormat");
        var selectedSeatsRaw = HttpContext.Session.GetString("SelectedSeats");
        var selectedCombo = HttpContext.Session.GetString("SelectedConcessions");

        //Nếu chưa chọn phim
        if (!selectedMovieId.HasValue || !selectedShowtimeId.HasValue)
        {
            return RedirectToAction("Index", "Home");
        }

        //Nếu chưa chọn ghế
        if (string.IsNullOrWhiteSpace(selectedSeatsRaw))
        {
            return RedirectToAction(nameof(SelectSeats));
        }

        //Chuyển chuỗi các Code ghế 
        var selectedSeats = selectedSeatsRaw
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .ToList();

        //Lấy ra showtime
        var showtime = _context.Showtimes.Include(s => s.Room).FirstOrDefault(s => s.MovieID == selectedMovieId.Value && s.ShowtimeID == selectedShowtimeId.Value);

        if (showtime == null)
        {
            return NotFound();
        }

        //lấy ra các ghế
        var seats = _context.Seats.Where(s => s.RoomID == showtime.RoomID);

        //Các ghế đã chọn
        var occupiedSeatCodes = _context.Tickets.Where(t => t.ShowtimeID == selectedShowtimeId.Value)
            .Select(t => t.Seat.SeatCode)
            .ToList();

        //lấy ra giá nhân
        var seatTypePricing = _context.SeatTypePricings.ToDictionary(st => st.SeatType, st => st.Multiplier);

        // Chuyển JSON thành danh sách; nếu Session chưa có thì tạo danh sách rỗng.
        var savedSelections = string.IsNullOrWhiteSpace(selectedCombo)
            ? new List<ConcessionRequest>()
            : JsonSerializer.Deserialize<List<ConcessionRequest>>(selectedCombo) ?? [];

        // Tạo Dictionary để tìm số lượng đã chọn nhanh theo ComboId.
        var savedQuantities = savedSelections
            .GroupBy(item => item.ComboId)
            .ToDictionary(group => group.Key, group => group.First().Quantity);

        var concession = _context.Combos.AsNoTracking().OrderBy(c => c.ComboName).ToList()
            .Select(c => new ConcessionItemViewModel
            {
                Id = c.ComboID,
                Name = c.ComboName,
                Price = c.ComboPrice,
                SelectedQuantity = savedQuantities.GetValueOrDefault(c.ComboID, 0)

            }).ToList();


        //Tạo model gửi đến view check out
        var model = _context.Movies.Where(m => m.MovieId == selectedMovieId)
            .Select(m => new BookingViewModel
            {
                MovieId = m.MovieId,
                MovieTitle = m.Title,
                SelectedDate = selectedDate ?? "",
                SelectedTime = selectedTime ?? "",
                CinemaFormat = selectedFormat ?? "",
                SelectedSeats = selectedSeats,
                OccupiedSeats = occupiedSeatCodes,

                //Giá vé
                StandardTicketPrice = showtime.BasePrice * (seatTypePricing.ContainsKey("Regular") ? seatTypePricing["Regular"] : 1.00m),
                VipTicketPrice = showtime.BasePrice * (seatTypePricing.ContainsKey("VIP") ? seatTypePricing["VIP"] : 1.00m),
                SweetboxTicketPrice = showtime.BasePrice * (seatTypePricing.ContainsKey("Couple") ? seatTypePricing["Couple"] : 1.00m),

                SeatChoices = seats.Select(s => new SeatChoiceViewModel
                {
                    SeatId = s.SeatID,
                    SeatCode = s.SeatCode,
                    SeatType = s.SeatType,
                    IsOccupied = occupiedSeatCodes.Contains(s.SeatCode),
                    IsSelected = selectedSeats.Contains(s.SeatCode),
                    Price = showtime.BasePrice * (seatTypePricing.ContainsKey(s.SeatType) ? seatTypePricing[s.SeatType] : 1.00m),
                }).ToList(),
                Concessions = concession,

            }).FirstOrDefault();

        if (model == null)
        {
            return NotFound();
        }

        return View(model);

    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartPayment() // GENERATE PAYMENT CODE
    {
        var userId = HttpContext.Session.GetInt32("UserID");
        var movieId = HttpContext.Session.GetInt32("SelectedMovieId");
        var showtimeId = HttpContext.Session.GetInt32("SelectedShowtimeId");
        var selectedSeats = HttpContext.Session.GetString("SelectedSeats");
        var selectedComboJson = HttpContext.Session.GetString("SelectedConcessions");

        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!movieId.HasValue || !showtimeId.HasValue || string.IsNullOrWhiteSpace(selectedSeats))
        {
            return RedirectToAction(nameof(SelectSeats));
        }

        // SEPAY DYNAMIC PAYMENT: create a pending payment record before showing QR.
        var result = await CreatePaymentIntentAsync(
            userId.Value,
            movieId.Value,
            showtimeId.Value,
            selectedSeats,
            selectedComboJson);

        if (!result.Succeeded ||
            string.IsNullOrWhiteSpace(result.PaymentReference) ||
            !result.ExpiresAtUtc.HasValue)
        {
            TempData["PaymentError"] = result.ErrorMessage ?? "Không thể tạo phiên thanh toán.";
            return RedirectToAction(nameof(Checkout));
        }

        HttpContext.Session.SetString("PaymentReference", result.PaymentReference);
        HttpContext.Session.SetString("PaymentExpiresAtUtc", result.ExpiresAtUtc.Value.ToString("O"));

        return RedirectToAction(nameof(Payment));

    }

    public async Task<IActionResult> Payment()
    {
        //Lấy ra lại các thông tin từ session
        var selectedMovieId = HttpContext.Session.GetInt32("SelectedMovieId");
        var selectedShowtimeId = HttpContext.Session.GetInt32("SelectedShowtimeId");
        var selectedDate = HttpContext.Session.GetString("SelectedDate");
        var selectedTime = HttpContext.Session.GetString("SelectedTime");
        var selectedFormat = HttpContext.Session.GetString("SelectedFormat");
        var selectedSeatsRaw = HttpContext.Session.GetString("SelectedSeats");
        var selectedCombo = HttpContext.Session.GetString("SelectedConcessions");
        var paymentReference = HttpContext.Session.GetString("PaymentReference");

        if (string.IsNullOrWhiteSpace(paymentReference))
        {
            return RedirectToAction(nameof(Checkout));
        }

        // SEPAY DYNAMIC PAYMENT: reload pending payment from DB because webhook has no Session.
        var paymentIntent = await GetPaymentIntentAsync(paymentReference);

        if (paymentIntent == null)
        {
            TempData["PaymentError"] = "Phiên không tồn tại.";
            return RedirectToAction(nameof(Checkout));
        }

        if (paymentIntent.Status == "Success" && paymentIntent.BookingID.HasValue)
        {
            ClearBookingSession();
            return RedirectToAction(nameof(PaymentSuccess), new { bookingId = paymentIntent.BookingID.Value });
        }

        if (paymentIntent.ExpiresAtUtc <= DateTime.UtcNow)
        {
            await MarkPaymentIntentExpiredAsync(paymentIntent.PaymentIntentID);
            TempData["PaymentError"] = "Phiên thanh toán đã hết hạn.";
            return RedirectToAction(nameof(Checkout));
        }

        //Nếu chưa chọn phim
        if (!selectedMovieId.HasValue || !selectedShowtimeId.HasValue)
        {
            return RedirectToAction("Index", "Home");
        }

        //Nếu chưa chọn ghế
        if (string.IsNullOrWhiteSpace(selectedSeatsRaw))
        {
            return RedirectToAction(nameof(SelectSeats));
        }

        //Chuyển chuỗi các Code ghế 
        var selectedSeats = selectedSeatsRaw
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .ToList();

        //Lấy ra showtime
        var showtime = _context.Showtimes.Include(s => s.Room).FirstOrDefault(s => s.MovieID == selectedMovieId.Value && s.ShowtimeID == selectedShowtimeId.Value);

        if (showtime == null)
        {
            return NotFound();
        }

        //lấy ra các ghế
        var seats = _context.Seats.Where(s => s.RoomID == showtime.RoomID);

        //Các ghế đã chọn
        var occupiedSeatCodes = _context.Tickets.Where(t => t.ShowtimeID == selectedShowtimeId.Value)
            .Select(t => t.Seat.SeatCode)
            .ToList();

        //lấy ra giá nhân
        var seatTypePricing = _context.SeatTypePricings.ToDictionary(st => st.SeatType, st => st.Multiplier);

        // Chuyển JSON thành danh sách; nếu Session chưa có thì tạo danh sách rỗng.
        var savedSelections = string.IsNullOrWhiteSpace(selectedCombo)
            ? new List<ConcessionRequest>()
            : JsonSerializer.Deserialize<List<ConcessionRequest>>(selectedCombo) ?? [];

        // Tạo Dictionary để tìm số lượng đã chọn nhanh theo ComboId.
        var savedQuantities = savedSelections
            .GroupBy(item => item.ComboId)
            .ToDictionary(group => group.Key, group => group.First().Quantity);

        var concession = _context.Combos.AsNoTracking().OrderBy(c => c.ComboName).ToList()
            .Select(c => new ConcessionItemViewModel
            {
                Id = c.ComboID,
                Name = c.ComboName,
                Price = c.ComboPrice,
                SelectedQuantity = savedQuantities.GetValueOrDefault(c.ComboID, 0)

            }).ToList();

        //Tạo ra model để tính các tổng tiền

        var model = _context.Movies.Where(m => m.MovieId == selectedMovieId)
            .Select(m => new BookingViewModel
            {
                MovieId = m.MovieId,
                MovieTitle = m.Title,
                SelectedDate = selectedDate ?? "",
                SelectedTime = selectedTime ?? "",
                CinemaFormat = selectedFormat ?? "",
                SelectedSeats = selectedSeats,
                OccupiedSeats = occupiedSeatCodes,

                //Giá vé
                StandardTicketPrice = showtime.BasePrice * (seatTypePricing.ContainsKey("Regular") ? seatTypePricing["Regular"] : 1.00m),
                VipTicketPrice = showtime.BasePrice * (seatTypePricing.ContainsKey("VIP") ? seatTypePricing["VIP"] : 1.00m),
                SweetboxTicketPrice = showtime.BasePrice * (seatTypePricing.ContainsKey("Couple") ? seatTypePricing["Couple"] : 1.00m),

                SeatChoices = seats.Select(s => new SeatChoiceViewModel
                {
                    SeatId = s.SeatID,
                    SeatCode = s.SeatCode,
                    SeatType = s.SeatType,
                    IsOccupied = occupiedSeatCodes.Contains(s.SeatCode),
                    IsSelected = selectedSeats.Contains(s.SeatCode),
                    Price = showtime.BasePrice * (seatTypePricing.ContainsKey(s.SeatType) ? seatTypePricing[s.SeatType] : 1.00m),
                }).ToList(),
                Concessions = concession,
            }).FirstOrDefault();


        if (model == null)
        {
            return NotFound();
        }

        var paymetModel = new PaymentPageViewModel
        {
            PaymentReference = paymentReference,
            MovieTitle = model.MovieTitle,
            SelectedDate = model.SelectedDate,
            SelectedTime = model.SelectedTime,
            CinemaFormat = model.CinemaFormat,
            SelectedSeats = model.SelectedSeats,

            Concessions = model.Concessions
                .Where(combo => combo.SelectedQuantity > 0)
                .ToList(),

            TicketSubtotal = model.TicketSubtotal,
            ConcessionSubtotal = model.ConcessionSubtotal,

            TotalAmount = paymentIntent.ExpectedAmount,
            QrImageUrl = BuildVietQrImageUrl(paymentIntent.ExpectedAmount, paymentIntent.PaymentReference),
            ExpiresAtUtc = paymentIntent.ExpiresAtUtc,
            ShowDevelopmentPaymentButton = _environment.IsDevelopment()
        };

        return View("Payment", paymetModel);


    }


    //========== CODE SEPAY CỦA HƯNG ========== 
    // POST: Booking/CompleteDevPayment
    // Action giả lập thanh toán thành công trong môi trường Development.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteDevPayment(string paymentReference)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var sessionReference = HttpContext.Session.GetString("PaymentReference");

        if (string.IsNullOrWhiteSpace(paymentReference) ||
            string.IsNullOrWhiteSpace(sessionReference) ||
            paymentReference != sessionReference)
        {
            return BadRequest("Mã thanh toán không hợp lệ.");
        }

        var intent = await GetPaymentIntentAsync(paymentReference, asNoTracking: false);

        if (intent == null)
        {
            TempData["PaymentError"] = "Phiên thanh toán không tồn tại.";
            return RedirectToAction(nameof(Checkout));
        }

        // SEPAY DYNAMIC PAYMENT: dev button uses the same completion path as webhook.
        var result = await CompletePaymentIntentAsync(
            intent,
            "Development",
            sePayTransactionId: null,
            sePayReferenceCode: null,
            sePayContent: "Development payment simulation",
            rawPayload: null);

        if (!result.Succeeded || !result.BookingId.HasValue)
        {
            TempData["PaymentError"] = result.ErrorMessage ?? "Không thể hoàn tất thanh toán.";
            return RedirectToAction(nameof(Payment));
        }

        ClearBookingSession();
        TempData["PaymentSuccess"] = "Thanh toán thành công.";

        return RedirectToAction(nameof(PaymentSuccess), new { bookingId = result.BookingId.Value });
    }

    [HttpPost("/webhooks/sepay")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SePayWebhook([FromBody] SePayWebhookRequest? request)
    {
        if (!IsAuthorizedSePayWebhook())
        {
            return Unauthorized(new { success = false, message = "Unauthorized" });
        }

        if (request == null || request.Id <= 0)
        {
            return BadRequest(new { success = false, message = "Invalid payload" });
        }

        if (!string.Equals(request.TransferType, "in", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new { success = true });
        }

        var intent = await FindPaymentIntentFromWebhookAsync(request);

        if (intent == null)
        {
            return BadRequest(new { success = false, message = "Payment reference not found" });
        }

        if (intent.SePayTransactionID == request.Id &&
            intent.Status == "Success" &&
            intent.BookingID.HasValue)
        {
            return Ok(new { success = true });
        }

        var duplicateTransaction = await _context.PaymentIntents
            .AsNoTracking()
            .AnyAsync(item =>
                item.PaymentIntentID != intent.PaymentIntentID &&
                item.SePayTransactionID == request.Id);

        if (duplicateTransaction)
        {
            return Ok(new { success = true });
        }

        if (request.TransferAmount != NormalizeVndAmount(intent.ExpectedAmount))
        {
            return BadRequest(new { success = false, message = "Transfer amount does not match expected amount" });
        }

        var rawPayload = JsonSerializer.Serialize(request, WebhookJsonOptions);
        var result = await CompletePaymentIntentAsync(
            intent,
            "SePay",
            request.Id,
            request.ReferenceCode,
            request.Content,
            rawPayload);

        if (!result.Succeeded)
        {
            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        return Ok(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> PaymentStatus(string paymentReference)
    {
        if (string.IsNullOrWhiteSpace(paymentReference))
        {
            return BadRequest(new { status = "Invalid" });
        }

        var userId = HttpContext.Session.GetInt32("UserID");
        var intent = await GetPaymentIntentAsync(paymentReference, userId);

        if (intent == null)
        {
            return NotFound(new { status = "NotFound" });
        }

        return Json(new
        {
            status = intent.Status,
            bookingId = intent.BookingID
        });
    }

    // SEPAY DYNAMIC PAYMENT: helpers below are the only new payment-flow logic.
    // They keep webhook completion independent from browser Session.
    private async Task<CreatePaymentIntentResult> CreatePaymentIntentAsync(
        int userId,
        int movieId,
        int showtimeId,
        string selectedSeats,
        string? selectedComboJson)
    {
        var (draft, errorMessage) = await BuildBookingDraftAsync(
            userId,
            movieId,
            showtimeId,
            selectedSeats,
            selectedComboJson);

        if (draft == null)
        {
            return new CreatePaymentIntentResult(false, null, null, errorMessage);
        }

        var nowUtc = DateTime.UtcNow;
        var paymentReference = await GeneratePaymentReferenceAsync();
        var expiresAtUtc = nowUtc.AddMinutes(10);
        var expectedAmount = NormalizeVndAmount(draft.TotalAmount);

        _context.PaymentIntents.Add(new PaymentIntent
        {
            UserID = userId,
            MovieID = movieId,
            ShowtimeID = showtimeId,
            PaymentReference = paymentReference,
            ExpectedAmount = expectedAmount,
            Status = "Pending",
            SelectedSeatCodes = string.Join(",", draft.SelectedSeatCodes),
            SelectedCombosJson = selectedComboJson,
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = expiresAtUtc
        });

        await _context.SaveChangesAsync();

        return new CreatePaymentIntentResult(true, paymentReference, expiresAtUtc, null);
    }

    private async Task<PaymentIntent?> GetPaymentIntentAsync(
        string paymentReference,
        int? userId = null,
        bool asNoTracking = true)
    {
        var query = _context.PaymentIntents
            .Where(item => item.PaymentReference == paymentReference);

        if (userId.HasValue)
        {
            query = query.Where(item => item.UserID == userId.Value);
        }

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync();
    }

    private async Task<PaymentCompletionResult> CompletePaymentIntentAsync(
        PaymentIntent intent,
        string paymentMethodName,
        long? sePayTransactionId,
        string? sePayReferenceCode,
        string? sePayContent,
        string? rawPayload)
    {
        if (intent.Status == "Success" && intent.BookingID.HasValue)
        {
            return new PaymentCompletionResult(true, intent.BookingID.Value, null);
        }

        if (intent.ExpiresAtUtc <= DateTime.UtcNow)
        {
            intent.Status = "Expired";
            await _context.SaveChangesAsync();
            return new PaymentCompletionResult(false, null, "Phien thanh toan da het han.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var (draft, errorMessage) = await BuildBookingDraftAsync(
                intent.UserID,
                intent.MovieID,
                intent.ShowtimeID,
                intent.SelectedSeatCodes,
                intent.SelectedCombosJson);

            if (draft == null)
            {
                await transaction.RollbackAsync();
                return new PaymentCompletionResult(false, null, errorMessage ?? "Thong tin dat ve khong hop le.");
            }

            var paidAmount = NormalizeVndAmount(intent.ExpectedAmount);

            if (NormalizeVndAmount(draft.TotalAmount) != paidAmount)
            {
                await transaction.RollbackAsync();
                return new PaymentCompletionResult(false, null, "So tien thanh toan khong con khop voi du lieu dat ve.");
            }

            var selectedSeatIds = draft.SelectedSeats.Select(seat => seat.SeatID).ToList();
            var occupied = await _context.Tickets
                .AnyAsync(ticket =>
                    ticket.ShowtimeID == intent.ShowtimeID &&
                    selectedSeatIds.Contains(ticket.SeatID));

            if (occupied)
            {
                await transaction.RollbackAsync();
                return new PaymentCompletionResult(false, null, "Mot hoac nhieu ghe vua duoc nguoi khac dat.");
            }

            var paymentMethod = await GetOrCreatePaymentMethodAsync(paymentMethodName);
            var nowUtc = DateTime.UtcNow;

            var booking = new Booking
            {
                UserID = intent.UserID,
                BookingDate = nowUtc,
                TotalAmount = paidAmount,
                Status = "Confirmed"
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            _context.Tickets.AddRange(draft.SelectedSeats.Select(seat =>
                new Ticket
                {
                    BookingID = booking.BookingID,
                    ShowtimeID = intent.ShowtimeID,
                    SeatID = seat.SeatID,
                    TicketCode = $"TKT-{Guid.NewGuid():N}"
                }));

            _context.BookingCombos.AddRange(draft.SelectedCombos.Select(combo =>
                new BookingCombo
                {
                    BookingID = booking.BookingID,
                    ComboID = combo.ComboID,
                    Quantity = draft.ComboQuantities[combo.ComboID],
                    UnitPrice = combo.ComboPrice
                }));

            _context.Payments.Add(new Payment
            {
                BookingID = booking.BookingID,
                MethodID = paymentMethod.MethodID,
                Amount = paidAmount,
                PaymentDate = nowUtc,
                Status = "Success"
            });

            intent.BookingID = booking.BookingID;
            intent.Status = "Success";
            intent.SePayTransactionID = sePayTransactionId;
            intent.SePayReferenceCode = sePayReferenceCode;
            intent.SePayContent = sePayContent;
            intent.WebhookPayload = rawPayload;
            intent.PaidAtUtc = nowUtc;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new PaymentCompletionResult(true, booking.BookingID, null);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return new PaymentCompletionResult(false, null, "Khong the hoan tat dat ve. Ghe co the vua duoc dat.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<(BookingDraft? Draft, string? ErrorMessage)> BuildBookingDraftAsync(
        int userId,
        int movieId,
        int showtimeId,
        string selectedSeatsRaw,
        string? selectedComboJson)
    {
        var selectedSeatCodes = selectedSeatsRaw
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(code => code.Trim())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (selectedSeatCodes.Count is < 1 or > 5)
        {
            return (null, "So luong ghe khong hop le.");
        }

        var showtime = await _context.Showtimes
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.ShowtimeID == showtimeId &&
                item.MovieID == movieId);

        if (showtime == null)
        {
            return (null, "Khong tim thay suat chieu.");
        }

        var selectedSeats = await _context.Seats
            .AsNoTracking()
            .Where(seat =>
                seat.RoomID == showtime.RoomID &&
                selectedSeatCodes.Contains(seat.SeatCode))
            .ToListAsync();

        if (selectedSeats.Count != selectedSeatCodes.Count)
        {
            return (null, "Danh sach ghe khong hop le.");
        }

        var seatTypePricing = await _context.SeatTypePricings
            .AsNoTracking()
            .ToDictionaryAsync(
                item => item.SeatType,
                item => item.Multiplier);

        var seatPrices = selectedSeats.ToDictionary(
            seat => seat.SeatID,
            seat =>
            {
                var multiplier = seatTypePricing.TryGetValue(
                    seat.SeatType,
                    out var configuredMultiplier)
                    ? configuredMultiplier
                    : 1.00m;

                return showtime.BasePrice * multiplier;
            });

        List<ConcessionRequest> selectedComboRequests;

        try
        {
            selectedComboRequests = string.IsNullOrWhiteSpace(selectedComboJson)
                ? []
                : JsonSerializer.Deserialize<List<ConcessionRequest>>(selectedComboJson) ?? [];
        }
        catch (JsonException)
        {
            return (null, "Du lieu combo khong hop le.");
        }

        var comboQuantities = selectedComboRequests
            .Where(item => item.ComboId > 0 && item.Quantity > 0)
            .GroupBy(item => item.ComboId)
            .ToDictionary(
                group => group.Key,
                group => Math.Clamp(group.First().Quantity, 1, 10));

        var selectedComboIds = comboQuantities.Keys.ToList();
        var selectedCombos = selectedComboIds.Count == 0
            ? new List<Combo>()
            : await _context.Combos
                .AsNoTracking()
                .Where(combo => selectedComboIds.Contains(combo.ComboID))
                .ToListAsync();

        if (selectedCombos.Count != selectedComboIds.Count)
        {
            return (null, "Danh sach combo khong hop le.");
        }

        var ticketSubtotal = seatPrices.Values.Sum();
        var concessionSubtotal = selectedCombos.Sum(combo =>
            combo.ComboPrice * comboQuantities[combo.ComboID]);

        return (new BookingDraft(
            userId,
            movieId,
            showtimeId,
            selectedSeatCodes,
            selectedSeats,
            selectedCombos,
            comboQuantities,
            ticketSubtotal,
            concessionSubtotal), null);
    }

    private async Task<PaymentMethod> GetOrCreatePaymentMethodAsync(string methodName)
    {
        var paymentMethod = await _context.PaymentMethods
            .FirstOrDefaultAsync(method => method.MethodName == methodName);

        if (paymentMethod != null)
        {
            return paymentMethod;
        }

        paymentMethod = new PaymentMethod
        {
            MethodName = methodName
        };

        _context.PaymentMethods.Add(paymentMethod);
        await _context.SaveChangesAsync();

        return paymentMethod;
    }

    private async Task<string> GeneratePaymentReferenceAsync()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var reference = $"COSMOS{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100000, 999999)}";
            var exists = await _context.PaymentIntents
                .AsNoTracking()
                .AnyAsync(item => item.PaymentReference == reference);

            if (!exists)
            {
                return reference;
            }
        }

        return $"COSMOS{Guid.NewGuid():N}"[..32].ToUpperInvariant();
    }

    private string BuildVietQrImageUrl(decimal amount, string paymentReference)
    {
        var sePaySection = _configuration.GetSection("SePay");
        var accountNumber = sePaySection["BankAccountNumber"]?.Trim();
        var bankCode = sePaySection["BankCode"]?.Trim();

        if (string.IsNullOrWhiteSpace(accountNumber) || string.IsNullOrWhiteSpace(bankCode))
        {
            return "/img/poster/QR.png";
        }

        var template = sePaySection["QrTemplate"]?.Trim();
        var accountHolder = sePaySection["AccountHolder"]?.Trim();
        var storeName = sePaySection["StoreName"]?.Trim();
        var amountText = ((long)NormalizeVndAmount(amount)).ToString();

        var query = new List<string>
        {
            $"acc={Uri.EscapeDataString(accountNumber)}",
            $"bank={Uri.EscapeDataString(bankCode)}",
            $"amount={Uri.EscapeDataString(amountText)}",
            $"des={Uri.EscapeDataString(paymentReference)}"
        };

        if (!string.IsNullOrWhiteSpace(template))
        {
            query.Add($"template={Uri.EscapeDataString(template)}");
        }

        if (!string.IsNullOrWhiteSpace(accountHolder))
        {
            query.Add($"holder={Uri.EscapeDataString(accountHolder)}");
        }

        if (!string.IsNullOrWhiteSpace(storeName))
        {
            query.Add($"store={Uri.EscapeDataString(storeName)}");
        }

        return $"https://vietqr.app/img?{string.Join("&", query)}";
    }

    private static decimal NormalizeVndAmount(decimal amount)
    {
        return decimal.Truncate(amount);
    }

    private bool IsAuthorizedSePayWebhook()
    {
        var configuredApiKey = _configuration["SePay:WebhookApiKey"];

        if (string.IsNullOrWhiteSpace(configuredApiKey))
        {
            return true;
        }

        var authorizationHeader = Request.Headers["Authorization"].ToString();
        return string.Equals(
            authorizationHeader,
            $"Apikey {configuredApiKey}",
            StringComparison.Ordinal);
    }

    private async Task<PaymentIntent?> FindPaymentIntentFromWebhookAsync(SePayWebhookRequest request)
    {
        var paymentReference =
            ExtractPaymentReference(request.Code) ??
            ExtractPaymentReference(request.Content) ??
            ExtractPaymentReference(request.Description);

        if (string.IsNullOrWhiteSpace(paymentReference))
        {
            return null;
        }

        return await _context.PaymentIntents
            .FirstOrDefaultAsync(item => item.PaymentReference == paymentReference);
    }

    private static string? ExtractPaymentReference(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var match = PaymentReferenceRegex.Match(text.ToUpperInvariant());
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }

    private async Task MarkPaymentIntentExpiredAsync(int paymentIntentId)
    {
        var intent = await _context.PaymentIntents
            .FirstOrDefaultAsync(item => item.PaymentIntentID == paymentIntentId);

        if (intent != null && intent.Status == "Pending")
        {
            intent.Status = "Expired";
            await _context.SaveChangesAsync();
        }
    }

    private sealed record CreatePaymentIntentResult(
        bool Succeeded,
        string? PaymentReference,
        DateTime? ExpiresAtUtc,
        string? ErrorMessage);

    private sealed record PaymentCompletionResult(
        bool Succeeded,
        int? BookingId,
        string? ErrorMessage);

    private sealed record BookingDraft(
        int UserId,
        int MovieId,
        int ShowtimeId,
        List<string> SelectedSeatCodes,
        List<Seat> SelectedSeats,
        List<Combo> SelectedCombos,
        Dictionary<int, int> ComboQuantities,
        decimal TicketSubtotal,
        decimal ConcessionSubtotal)
    {
        public decimal TotalAmount => TicketSubtotal + ConcessionSubtotal;
    }


// POST: Booking/CancelBooking
// Huỷ tiến trình đặt vé hiện tại và xóa dữ liệu Booking trong Session.
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CancelBooking()
{
    // Lấy người dùng và mã thanh toán của tiến trình hiện tại.
    var userId = HttpContext.Session.GetInt32("UserID");
    var paymentReference = HttpContext.Session.GetString("PaymentReference");

    // Chỉ cập nhật PaymentIntent khi đã đăng nhập và đã tạo phiên QR.
    if (userId.HasValue && !string.IsNullOrWhiteSpace(paymentReference))
    {
        var paymentIntent = await _context.PaymentIntents.FirstOrDefaultAsync(intent => intent.PaymentReference == paymentReference && intent.UserID == userId.Value);

        // Không được huỷ giao dịch đã thanh toán thành công.
        if (paymentIntent != null && paymentIntent.Status == "Pending" && !paymentIntent.BookingID.HasValue)
        {
            // Dùng Expired
            paymentIntent.Status = "Expired";

            // Đặt thời hạn về hiện tại để webhook không hoàn tất intent này.
            paymentIntent.ExpiresAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
    }

    // Hàm này đã có sẵn trong BookingController.
    // Chỉ xóa dữ liệu đặt vé, không xóa Session đăng nhập.
    ClearBookingSession();

    TempData["BookingMessage"] = "Đã huỷ quá trình đặt vé.";

    return RedirectToAction("Index", "Home");
}
    private void ClearBookingSession()
    {
        var bookingSessionKeys = new[]
        {
            "SelectedMovieId",
            "SelectedShowtimeId",
            "SelectedDate",
            "SelectedTime",
            "SelectedFormat",
            "SelectedSeats",
            "SelectedConcessions",
            "PaymentReference",
            "PaymentExpiresAtUtc"
        };

        foreach (var key in bookingSessionKeys)
        {
            HttpContext.Session.Remove(key);
        }
    }

    public async Task<IActionResult> PaymentSuccess(int bookingId)
    {
        var userId = HttpContext.Session.GetInt32("UserID");
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        // Lấy Booking cùng vé, ghế, combo và payment.
        var booking = await _context.Bookings
            .AsNoTracking()
            .Include(item => item.Tickets)
                .ThenInclude(ticket => ticket.Seat)
            .Include(item => item.Tickets)
                .ThenInclude(ticket => ticket.Showtime)
            .Include(item => item.BookingCombos)
                .ThenInclude(item => item.Combo)
            .Include(item => item.Payments)
                .ThenInclude(payment => payment.PaymentMethod)
            .FirstOrDefaultAsync(item =>
                item.BookingID == bookingId &&
                item.UserID == userId.Value);
        if (booking == null)
        {
            return NotFound();
        }
        return View(booking);

    }


}
