using Cinema_Management.Data;
using Cinema_Management.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json; // Dùng để chuyển danh sách combo thành JSON khi lưu Session.

namespace Cinema_Management.Controllers;

public class BookingController : Controller
{
    private readonly ApplicationDbContext _context;


    //Khai báo biến _context để truy cập vào cơ sở dữ liệu thông qua ApplicationDbContext
    public BookingController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Booking/SelectShowtime dùng để fill dữ liệu lên UI
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
        movie.SelectedDate = movie.AvailableDates.FirstOrDefault() ?? DateTime.Today.ToString("yyyy-MM-dd");

        //Các giờ có thể có suất chiếu của phim dựa trên ngày đã chọn
        movie.AvailableTimes = movie.ShowtimeChoices
            .Where(s => s.Date == movie.SelectedDate) //Lấy ra ngày đc chọn để lọc ra các suất chiếu có cùng ngày
            .Select(s => s.Time)
            .Distinct()
            .ToList();

        movie.SelectedTime = movie.AvailableTimes.FirstOrDefault() ?? string.Empty;
        movie.CinemaFormat = movie.AvailableFormats.FirstOrDefault() ?? string.Empty;

        //Tìm kiếm ShowTimeId dựa trên ngày, giờ và định dạng đã chọn
        movie.ShowtimeId = movie.ShowtimeChoices
            .FirstOrDefault(s =>
                s.Date == movie.SelectedDate &&
                s.Time == movie.SelectedTime &&
                s.Format == movie.CinemaFormat)
            ?.ShowtimeId;

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


}