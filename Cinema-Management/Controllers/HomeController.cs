
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Cinema_Management.Models;
using Cinema_Management.Data;
using System.Linq;

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
}