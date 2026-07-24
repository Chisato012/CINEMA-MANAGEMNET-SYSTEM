namespace Cinema_Management.Models;
using System.ComponentModel.DataAnnotations;

public class MovieDirectors
{
    public int MovieID { get; set; }
    public MovieViewModel Movie { get; set; } = null!;

    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;
}
