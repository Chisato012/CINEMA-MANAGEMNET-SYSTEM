using Microsoft.AspNetCore.Mvc;
using Cinema_Management.Data;
using Cinema_Management.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Cinema_Management.Controllers;

public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        ViewBag.AdminEmail = HttpContext.Session.GetString("UserEmail") ?? "Admin";
        return View();
    }

    //Action cho trang doanh thu
    public IActionResult Analytics(string? month, int? movieId)
    {
        //Khai báo 2 biến thời gian
        DateTime? fromDate = null;
        DateTime? toDate = null;
        //Kiểm tra điều kiện
        if (!string.IsNullOrWhiteSpace(month) &&
        DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedMonth))
        {
            fromDate = new DateTime(parsedMonth.Year, parsedMonth.Month, 1);
            toDate = fromDate.Value.AddMonths(1);
        }
        else
        {
            month = null;
        }

        // paymentRows là query chính để tính tổng doanh thu.
        // Chỉ lấy Payment đã thanh toán thành công và Booking đã xác nhận.
        var paymentRows = _context.Payments.AsNoTracking()
            .Where(p => p.Status == "Success"
                && p.Booking != null
                && p.Booking.Status == "Confirmed");

        // Nếu người dùng chọn tháng, lọc payment theo PaymentDate trong tháng đó.
        if (fromDate.HasValue && toDate.HasValue)
        {
            paymentRows = paymentRows.Where(p => p.PaymentDate >= fromDate.Value && p.PaymentDate < toDate.Value);
        }

        // Nếu người dùng chọn phim, chỉ lấy các payment thuộc booking có vé của phim đó.
        if (movieId.HasValue)
        {
            paymentRows = paymentRows.Where(p =>
                p.Booking!.Tickets.Any(t => t.Showtime != null && t.Showtime.MovieID == movieId.Value));
        }

        //ticketRows là query chính để đếm số vé bán ra.
        // Chỉ tính vé thuộc Booking đã Confirmed và có ít nhất một Payment Success.
        var ticketRows = _context.Tickets.AsNoTracking()
            .Where(t => t.Booking != null && t.Booking.Status == "Confirmed" && t.Booking.Payments.Any(p => p.Status == "Success"))
            .Select(t => new
            {
                t.TicketID,

                // Lấy MovieID thông qua quan hệ Ticket -> Showtime -> Movie.
                MovieId = t.Showtime!.MovieID,

                // Lấy ngày thanh toán thành công đầu tiên của booking để dùng lọc theo tháng.
                PaidAt = t.Booking!.Payments
                .Where(p => p.Status == "Success")
                .Min(p => p.PaymentDate)
            });

        // Nếu có lọc tháng, chỉ đếm vé có ngày thanh toán nằm trong tháng đó.
        if (fromDate.HasValue && toDate.HasValue)
        {
            ticketRows = ticketRows.Where(t => t.PaidAt >= fromDate.Value && t.PaidAt < toDate.Value);
        }

        // Nếu có lọc phim, chỉ đếm vé của phim được chọn.
        if (movieId.HasValue)
        {
            ticketRows = ticketRows.Where(t => t.MovieId == movieId.Value);
        }

        // timelinePayments dùng để vẽ/thống kê doanh thu theo từng tháng.
        // Query này không lọc theo selected month, vì cần nhìn được biểu đồ nhiều tháng.
        var timelinePayments = _context.Payments.AsNoTracking()
            .Where(p => p.Status == "Success"
                && p.Booking != null
                && p.Booking.Status == "Confirmed");

        // Nếu chọn phim, biểu đồ doanh thu theo tháng cũng chỉ hiện doanh thu của phim đó.
        if (movieId.HasValue)
        {
            timelinePayments = timelinePayments.Where(p =>
                p.Booking!.Tickets.Any(t => t.Showtime != null && t.Showtime.MovieID == movieId.Value));
        }
        // revenueByMonth gom nhóm Payment theo năm/tháng và tính tổng Amount.
        var revenueByMonth = timelinePayments
            .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Revenue = g.Sum(p => p.Amount)
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToList()
            .Select(x => new MonthlyRevenueItem
            {
                Year = x.Year,
                Month = x.Month,
                Label = $"{x.Month:00}/{x.Year}",
                Revenue = x.Revenue
            })
            .ToList();

        // months dùng để render dropdown chọn tháng.
        // Chỉ lấy các tháng thật sự có payment thành công.
        var months = _context.Payments.AsNoTracking()
            .Where(p => p.Status == "Success"
                && p.Booking != null
                && p.Booking.Status == "Confirmed")
            .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month })
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Month)
            .ToList()
            .Select(x => new AnalyticsMonthOption
            {
                Value = $"{x.Year}-{x.Month:00}",
                Text = $"Tháng {x.Month:00}/{x.Year}"
            })
            .ToList();
        // movies dùng để render dropdown chọn phim.
        var movies = _context.Movies.AsNoTracking()
            .OrderBy(m => m.Title)
            .Select(m => new AnalyticsMovieOption
            {
                MovieId = m.MovieId,
                Title = m.Title
            })
            .ToList();

        // model là dữ liệu tổng hợp cuối cùng gửi sang Analytics.cshtml.
        var model = new AdminAnalyticsViewModel
        {
            // Lưu lại filter hiện tại để view biết option nào đang được chọn.
            SelectedMonth = month,
            SelectedMovieId = movieId,

            // Tổng doanh thu sau khi áp dụng filter tháng/phim.
            TotalRevenue = paymentRows.Sum(p => (decimal?)p.Amount) ?? 0m,

            // Tổng số vé bán ra sau khi áp dụng filter tháng/phim.
            TotalTicketsSold = ticketRows.Count(),

            // Tổng số booking đã thanh toán thành công, dùng Distinct để tránh 1 booking có nhiều payment bị đếm trùng.
            ConfirmedBookings = paymentRows.Select(p => p.BookingID).Distinct().Count(),

            // Dữ liệu dropdown.
            Months = months,
            Movies = movies,

            // Dữ liệu biểu đồ/bảng doanh thu theo tháng.
            RevenueByMonth = revenueByMonth
        };
        return View(model);

    }

    public IActionResult Staff(int page = 1)
    {
        const int pageSize = 5;
        if (page < 1)
        {
            page = 1;
        }
        var StaffQuery = _context.Users
        .Where(u => u.Role == "Staff")
        .OrderBy(u => u.UserID); //truy vấn từ usersDB

        var totalCount = StaffQuery.Count();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize); //tìm ra tổng số trang

        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        var staffList = StaffQuery
        .Skip((page - 1) * pageSize) //skip ra số nhân viên tối đa 5
        .Take(pageSize) //lấy ra 5 nhân viên
        .ToList();

        ViewBag.TotalCount = totalCount;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.PageSize = pageSize;

        return View(staffList);

    }

    public IActionResult Settings()
    {
        //lấy ra ID từ session
        var userId = HttpContext.Session.GetInt32("UserID");

        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        //Tìm tk admin trong bảng user
        var admin = _context.Users.FirstOrDefault(u => u.UserID == userId.Value && u.Role == "Admin");

        if (admin == null)
        {
            TempData["ErrorMessage"] = "Không thấy tài khoản Admin";
            return RedirectToAction("Index");
        }

        //gửi dữ liệu sang view
        return View(admin);
    }

    //Hàm sửa tài khoản cho admin, các tham số truyền vào lấy từ view thông qua thuộc tính thẻ name
    public IActionResult UpdateSettings(string FullName, string Email, string? Password, string? newPassword)
    {
        //lấy ra ID từ session
        var userId = HttpContext.Session.GetInt32("UserID");

        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        //Tìm tk admin trong bảng user
        var admin = _context.Users.FirstOrDefault(u => u.UserID == userId.Value && u.Role == "Admin");

        if (admin == null)
        {
            TempData["ErrorMessage"] = "Không thấy tài khoản Admin";
            return RedirectToAction("Index");
        }

        if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email))
        {
            TempData["ErrorMessage"] = "Tên và email không được để trống";
            return RedirectToAction("Settings");
        }

        var emailExists = _context.Users.Any(u => u.Email == Email && u.UserID != admin.UserID);

        if (emailExists)
        {
            TempData["ErrorMessage"] = "Email này đã tồn tại!";
            return RedirectToAction("Settings");
        }

        //cập nhật thông tin
        admin.FullName = FullName.Trim();
        admin.Email = Email.Trim();

        //nếu muốn đổi mật khẩu mới
        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            //bắt buộc nhập mk cũ
            if (string.IsNullOrWhiteSpace(Password))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập mật khẩu cũ";
                return RedirectToAction("Settings");
            }

            //Kiểm tra xem mật khẩu cũ đúng không
            var oldPasswordCorrect = BCrypt.Net.BCrypt.Verify(Password, admin.PasswordHash);

            if (!oldPasswordCorrect)
            {
                TempData["ErrorMessage"] = "Mật khẩu cũ không đúng";
                return RedirectToAction("Settings");
            }

            //Hash mật khẩu mới trước khi lưu
            //Mật khẩu lấy từ DB cũ                             //mật khẩu lấy từ view
            admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        }

        _context.SaveChanges();

        //cập nhật lại email nếu admin đổi email
        HttpContext.Session.SetString("UserEmail", admin.Email);

        TempData["SuccessMessage"] = "Cập nhật tài khoản thành công";
        return RedirectToAction("Settings");

    }

    [HttpPost]
    public IActionResult CreateStaff(string FullName, string Email, string? PhoneNumber, string PasswordHash, bool Status, string Role, DateTime? DOB)
    {
        if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(PasswordHash))
        {
            TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin bắt buộc!";
            return RedirectToAction("Staff");
        }

        var exists = _context.Users.Any(u => u.Email == Email);
        if (exists)
        {
            TempData["ErrorMessage"] = "Email đã tồn tại!";
            return RedirectToAction("Staff");
        }

        var newUser = new User
        {
            FullName = FullName.Trim(),
            Email = Email.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(PasswordHash),
            Status = Status,
            Role = string.IsNullOrWhiteSpace(Role) ? "Staff" : Role,
            DOB = DOB
        };

        _context.Users.Add(newUser);
        _context.SaveChanges();

        TempData["SuccessMessage"] = "Tạo tài khoản thành công!";
        return RedirectToAction("Staff");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateStaff(int UserID, string FullName, string Email, string? PhoneNumber, string? PasswordHash, bool Status, string Role, DateTime? DOB, int page = 1)
    {
        if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email))
        {
            TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin bắt buộc!";
            return RedirectToAction("Staff", new { page });
        }

        var staff = _context.Users.FirstOrDefault(u => u.UserID == UserID);
        if (staff == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy tài khoản nhân viên!";
            return RedirectToAction("Staff", new { page });
        }

        var emailExists = _context.Users.Any(u => u.Email == Email && u.UserID != UserID);
        if (emailExists)
        {
            TempData["ErrorMessage"] = "Email đã tồn tại!";
            return RedirectToAction("Staff", new { page });
        }

        staff.FullName = FullName.Trim();
        staff.Email = Email.Trim();
        staff.PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim();
        staff.Status = Status;
        staff.Role = string.IsNullOrWhiteSpace(Role) ? "Staff" : Role;
        staff.DOB = DOB;

        if (!string.IsNullOrWhiteSpace(PasswordHash))
        {
            staff.PasswordHash = BCrypt.Net.BCrypt.HashPassword(PasswordHash);
        }

        _context.SaveChanges();

        TempData["SuccessMessage"] = "Cập nhật tài khoản thành công!";
        return RedirectToAction("Staff", new { page });
    }
}
