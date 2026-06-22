using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Cinema_Management.Data;
using Cinema_Management.Models;

namespace Cinema_Management.Controllers;


public class StaffController : Controller
{
    private readonly ApplicationDbContext _context;
    public StaffController(ApplicationDbContext context) => _context = context;

    private static readonly string[] AgeRatings = { "P", "K", "T13", "T16", "T18", "C" };
    

    public async Task<IActionResult> Index()
    {
        ViewBag.ActiveTab = "scheduling";
        var movies = await _context.Movies
            .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
            .OrderByDescending(m => m.ReleaseDate)
            .ToListAsync();
        return View(movies);
    }

    public IActionResult Halls()
    {
        ViewBag.ActiveTab = "halls";
        return View();
    }

    public IActionResult Concessions()
    {
        ViewBag.ActiveTab = "concessions";
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateMovie(MovieViewModel movie)
    {
        if (!ModelState.IsValid) { return RedirectToAction(nameof(Index)); }
        // movie.IsActive = true; // Property không tồn tại trong MovieViewModel
        _context.Movies.Add(movie);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditMovie(MovieViewModel movie)
    {
        if (!ModelState.IsValid) { return RedirectToAction(nameof(Index)); }
        var existing = await _context.Movies.FindAsync(movie.MovieId);
        if (existing == null) return NotFound();
        
        existing.Title = movie.Title;
        existing.Duration = movie.Duration;
        existing.AgeRating = movie.AgeRating;
        existing.Synopsis = movie.Synopsis;
        existing.PosterURL = movie.PosterURL;
        
        _context.Update(existing);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // [HttpPost, ValidateAntiForgeryToken]
    // public async Task<IActionResult> DeactivateMovie(int id)
    // {
    //     var movie = await _context.Movies.FindAsync(id);
    //     if (movie == null) return NotFound();
    //     movie.IsActive = false; // soft delete - giữ lịch sử
    //     await _context.SaveChangesAsync();
    //     return RedirectToAction(nameof(Index));
    // }

    // private async Task LoadDropdowns(MovieViewModel? movie = null)
    // {
    //     ViewBag.Countries = new SelectList(await _context.Countries.ToListAsync(), "CountryID", "CountryName", movie?.CountryID);
    //     ViewBag.Languages = new SelectList(await _context.Languages.ToListAsync(), "LanguageID", "LanguageName", movie?.LanguageID);
    //     ViewBag.AgeRatings = new SelectList(AgeRatings, movie?.AgeRating);
    // }
}