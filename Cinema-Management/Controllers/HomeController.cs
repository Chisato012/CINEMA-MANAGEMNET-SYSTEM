
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

}