using System.ComponentModel.DataAnnotations;

namespace Cinema_Management.Models;

public class Genre
{
    [Key]
    public int GenreID { get; set; }
    public string Name { get; set; }

    // Navigation property
    public ICollection<MovieGenre> MovieGenres { get; set; }
}