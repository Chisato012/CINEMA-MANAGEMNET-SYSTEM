using Microsoft.AspNetCore.Mvc;
using Cinema_Management.Data;
using Cinema_Management.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;

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

     
    public IActionResult Analytics(string? month, int? movieId)
    {
        
        DateTime? fromDate = null;
        DateTime? toDate = null;
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

         
         
        var paymentRows = _context.Payments.AsNoTracking()
            .Where(p => p.Status == "Success"
                && p.Booking != null
                && p.Booking.Status == "Confirmed");

         
        if (fromDate.HasValue && toDate.HasValue)
        {
            paymentRows = paymentRows.Where(p => p.PaymentDate >= fromDate.Value && p.PaymentDate < toDate.Value);
        }

         
        if (movieId.HasValue)
        {
            paymentRows = paymentRows.Where(p =>
                p.Booking!.Tickets.Any(t => t.Showtime != null && t.Showtime.MovieID == movieId.Value));
        }

         
         
        var ticketRows = _context.Tickets.AsNoTracking()
            .Where(t => t.Booking != null && t.Booking.Status == "Confirmed" && t.Booking.Payments.Any(p => p.Status == "Success"))
            .Select(t => new
            {
                t.TicketID,

                 
                MovieId = t.Showtime!.MovieID,

                 
                PaidAt = t.Booking!.Payments
                .Where(p => p.Status == "Success")
                .Min(p => p.PaymentDate)
            });

         
        if (fromDate.HasValue && toDate.HasValue)
        {
            ticketRows = ticketRows.Where(t => t.PaidAt >= fromDate.Value && t.PaidAt < toDate.Value);
        }

         
        if (movieId.HasValue)
        {
            ticketRows = ticketRows.Where(t => t.MovieId == movieId.Value);
        }

         
         
        var timelinePayments = _context.Payments.AsNoTracking()
            .Where(p => p.Status == "Success"
                && p.Booking != null
                && p.Booking.Status == "Confirmed");

         
        if (movieId.HasValue)
        {
            timelinePayments = timelinePayments.Where(p =>
                p.Booking!.Tickets.Any(t => t.Showtime != null && t.Showtime.MovieID == movieId.Value));
        }
         
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
         
        var movies = _context.Movies.AsNoTracking()
            .OrderBy(m => m.Title)
            .Select(m => new AnalyticsMovieOption
            {
                MovieId = m.MovieId,
                Title = m.Title
            })
            .ToList();

         
        var model = new AdminAnalyticsViewModel
        {
             
            SelectedMonth = month,
            SelectedMovieId = movieId,

             
            TotalRevenue = paymentRows.Sum(p => (decimal?)p.Amount) ?? 0m,

             
            TotalTicketsSold = ticketRows.Count(),

             
            ConfirmedBookings = paymentRows.Select(p => p.BookingID).Distinct().Count(),

             
            Months = months,
            Movies = movies,

             
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
        .OrderBy(u => u.UserID);  

        var totalCount = StaffQuery.Count();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);  

        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        var staffList = StaffQuery
        .Skip((page - 1) * pageSize) 
        .Take(pageSize)  
        .ToList();

        ViewBag.TotalCount = totalCount;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.PageSize = pageSize;

        return View(staffList);

    }

    public IActionResult Settings()
    {
        
        var userId = HttpContext.Session.GetInt32("UserID");

        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        
        var admin = _context.Users.FirstOrDefault(u => u.UserID == userId.Value && u.Role == "Admin");

        if (admin == null)
        {
            TempData["ErrorMessage"] = "Không thấy tài khoản Admin";
            return RedirectToAction("Index");
        }

         
        return View(admin);
    }

     
    public IActionResult UpdateSettings(string FullName, string Email, string? Password, string? newPassword)
    {
         
        var userId = HttpContext.Session.GetInt32("UserID");

        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

         
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

         
        admin.FullName = FullName.Trim();
        admin.Email = Email.Trim();

         
        if (!string.IsNullOrWhiteSpace(newPassword))
        {
             
            if (string.IsNullOrWhiteSpace(Password))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập mật khẩu cũ";
                return RedirectToAction("Settings");
            }

             
            var oldPasswordCorrect = BCrypt.Net.BCrypt.Verify(Password, admin.PasswordHash);

            if (!oldPasswordCorrect)
            {
                TempData["ErrorMessage"] = "Mật khẩu cũ không đúng";
                return RedirectToAction("Settings");
            }

             
             
            admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        }

        _context.SaveChanges();

         
        HttpContext.Session.SetString("UserEmail", admin.Email);

        TempData["SuccessMessage"] = "Cập nhật tài khoản thành công";
        return RedirectToAction("Settings");

    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateStaff(string? FullName, string? Email, string? PhoneNumber, string? PasswordHash, DateTime? DOB, int page = 1)
    {
         
        var normalizedFullName = FullName?.Trim() ?? string.Empty;
        var normalizedEmail = Email?.Trim() ?? string.Empty;

         
        if (string.IsNullOrWhiteSpace(normalizedFullName))
        {
            ModelState.AddModelError("FullName", "Tên không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            ModelState.AddModelError("Email", "Email không được để trống.");
        }
        else if (!new EmailAddressAttribute().IsValid(normalizedEmail))
        {
            ModelState.AddModelError("Email", "Email không đúng định dạng.");
        }
        else if (_context.Users.Any(u => u.Email == normalizedEmail))
        {
            ModelState.AddModelError("Email", "Email đã tồn tại!");
        }

        if (string.IsNullOrWhiteSpace(PasswordHash))
        {
            ModelState.AddModelError("PasswordHash", "Mật khẩu không được để trống.");
        }
        else if (!Regex.IsMatch(PasswordHash, @"^(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$"))
        {
            ModelState.AddModelError("PasswordHash", "Mật khẩu cần ít nhất 8 ký tự, gồm 1 chữ hoa, 1 số và 1 ký tự đặc biệt.");
        }

        string namePattern = @"^[\p{L}\s]+$";
         
        if (!string.IsNullOrWhiteSpace(normalizedFullName) && !Regex.IsMatch(normalizedFullName, namePattern))
        {
            ModelState.AddModelError("FullName", "Tên chỉ được chứa chữ cái và khoảng trắng.");
        }

        const string phonePattern = @"^0[0-9]{9}$";
        if (!string.IsNullOrWhiteSpace(PhoneNumber) && !Regex.IsMatch(PhoneNumber.Trim(), phonePattern))
        {
            ModelState.AddModelError("PhoneNumber", "Số điện thoại không hợp lệ. Vui lòng nhập số điện thoại bắt đầu bằng '0' và có 10 chữ số.");
        }
        if (DOB.HasValue && (DOB.Value.Date > DateTime.Today.AddYears(-16) || DOB.Value.Date < new DateTime(1930, 1, 1)))
        {
            ModelState.AddModelError("DOB", "Ngày sinh không hợp lệ. Vui lòng chọn ngày sinh trước ngày hiện tại và lớn hơn 16 tuổi");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.OpenModal = "create";

             
            ViewBag.CreateFullName = FullName;
            ViewBag.CreateEmail = Email;
            ViewBag.CreatePhoneNumber = PhoneNumber;
            ViewBag.CreateDOB = DOB?.ToString("yyyy-MM-dd");

            var staffList = LoadStaffPage(page);
            return View("Staff", staffList);
        }

        var newUser = new User
        {
            FullName = normalizedFullName,
            Email = normalizedEmail,
            PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(PasswordHash!),
            Status = true,
            Role = "Staff",
            DOB = DOB
        };

        _context.Users.Add(newUser);
        _context.SaveChanges();

        TempData["SuccessMessage"] = "Tạo tài khoản thành công!";
         
        return RedirectToAction(nameof(Staff), new { page });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateStaff(int? UserID, string? FullName, string? Email, string? PhoneNumber, string? PasswordHash, DateTime? DOB, int page = 1)
    {
         
        User? staff = null;
        if (!UserID.HasValue)
        {
            ModelState.AddModelError("UserID", "Không xác định được nhân viên cần cập nhật.");
        }
        else
        {
            staff = _context.Users.FirstOrDefault(u => u.UserID == UserID.Value && u.Role == "Staff");
            if (staff == null)
            {
                ModelState.AddModelError("UserID", "Không tìm thấy tài khoản nhân viên!");
            }
        }

        
        var normalizedFullName = FullName?.Trim() ?? string.Empty;
        var normalizedEmail = Email?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedFullName))
        {
            ModelState.AddModelError("FullName", "Tên không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            ModelState.AddModelError("Email", "Email không được để trống.");
        }
        else if (!new EmailAddressAttribute().IsValid(normalizedEmail))
        {
            ModelState.AddModelError("Email", "Email không đúng định dạng.");
        }
        else if (_context.Users.Any(u => u.Email == normalizedEmail && u.UserID != UserID))
        {
            ModelState.AddModelError("Email", "Email đã tồn tại!");
        }

        string namePattern = @"^[\p{L}\s]+$";
        if (!string.IsNullOrWhiteSpace(normalizedFullName) && !Regex.IsMatch(normalizedFullName, namePattern))
        {
            ModelState.AddModelError("FullName", "Tên chỉ được chứa chữ cái và khoảng trắng.");
        }

        const string phonePattern = @"^0[0-9]{9}$";
        if (!string.IsNullOrWhiteSpace(PhoneNumber) && !Regex.IsMatch(PhoneNumber.Trim(), phonePattern))
        {
            ModelState.AddModelError("PhoneNumber", "Số điện thoại không hợp lệ. Vui lòng nhập số điện thoại bắt đầu bằng '0' và có 10 chữ số.");
        }
        if (!string.IsNullOrWhiteSpace(PasswordHash) &&
            !Regex.IsMatch(PasswordHash, @"^(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$"))
        {
            ModelState.AddModelError("PasswordHash", "Mật khẩu cần ít nhất 8 ký tự, gồm 1 chữ hoa, 1 số và 1 ký tự đặc biệt.");
        }

        if (DOB.HasValue && (DOB.Value.Date > DateTime.Today.AddYears(-16) || DOB.Value.Date < new DateTime(1930, 1, 1)))
        {
            ModelState.AddModelError("DOB", "Ngày sinh không hợp lệ. Vui lòng chọn ngày sinh trước ngày hiện tại và lớn hơn 16 tuổi");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.OpenModal = "edit";

             
            ViewBag.EditUserID = UserID;
            ViewBag.EditFullName = FullName;
            ViewBag.EditEmail = Email;
            ViewBag.EditPhoneNumber = PhoneNumber;
            ViewBag.EditDOB = DOB?.ToString("yyyy-MM-dd");

            var staffList = LoadStaffPage(page);
            return View("Staff", staffList);
        }

         
        staff!.FullName = normalizedFullName;
        staff.Email = normalizedEmail;
        staff.PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim();
        staff.Role = "Staff";
        staff.DOB = DOB;

        if (!string.IsNullOrWhiteSpace(PasswordHash))
        {
            staff.PasswordHash = BCrypt.Net.BCrypt.HashPassword(PasswordHash);
        }

        _context.SaveChanges();

        TempData["SuccessMessage"] = "Cập nhật tài khoản thành công!";
        return RedirectToAction("Staff", new { page });
    }

    private List<User> LoadStaffPage(int page)
    {
        const int pageSize = 5;

        var staffQuery = _context.Users
            .Where(u => u.Role == "Staff")
            .OrderBy(u => u.UserID);

        var totalCount = staffQuery.Count();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        page = Math.Max(page, 1);

        if (totalPages > 0)
            page = Math.Min(page, totalPages);

        ViewBag.TotalCount = totalCount;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.PageSize = pageSize;

        return staffQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }
}
