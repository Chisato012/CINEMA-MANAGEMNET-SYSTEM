
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Cinema_Management.Models;
using Cinema_Management.Data;
using System.Linq;
using System.ComponentModel.Design;
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
                MovieDirector = string.Join(", ", m.MovieDirectors.Select(md => md.person.FullName)),
                MovieCast = string.Join(", ", m.MovieCasts.Select(mc => mc.person.FullName))
            })
            .FirstOrDefault();

        if(movie == null)
        {
            return NotFound();
        }

        return View(movie);
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