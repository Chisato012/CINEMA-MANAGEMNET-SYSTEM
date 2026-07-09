using Cinema_Management.Data;
using Cinema_Management.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        
        if(model == null)
        {
            return NotFound();
        }
        return View(model);
    }

    //Post: Booking/SelectSeats
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

        if(model == null)
        {
            return NotFound();
        }

        return View("SelectSeats", model);
    }




}