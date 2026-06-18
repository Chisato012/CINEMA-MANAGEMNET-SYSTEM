namespace Cinema_Management.Models;
using System.ComponentModel.DataAnnotations;

public class MovieGenre
{
    public int MovieID { get; set; }
    public MovieViewModel Movie { get; set; }

    public int GenreID { get; set; }
    public Genre Genre { get; set; }
}