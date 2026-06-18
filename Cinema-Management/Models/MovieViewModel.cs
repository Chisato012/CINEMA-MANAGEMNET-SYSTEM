namespace Cinema_Management.Models;
using System.ComponentModel.DataAnnotations;

public class MovieViewModel
{
   [Key]
    public int MovieId { get; set; }
    public string Title { get; set; }
    public short Duration { get; set; }
    public string PosterURL { get; set; }
    public string Genre { get; set; }

    // Navigation property
    public ICollection<MovieGenre> MovieGenres { get; set; }
}
