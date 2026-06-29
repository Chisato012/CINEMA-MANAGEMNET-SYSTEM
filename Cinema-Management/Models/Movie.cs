using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cinema_Management.Models;

[Table("Movies")]
public class Movie
{
    [Key]
    public int MovieID { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public DateTime ReleaseDate { get; set; }

    [StringLength(10)]
    public string AgeRating { get; set; } = "--";

    public short Duration { get; set; }

    [StringLength(1000)]
    public string Synopsis { get; set; } = "Chưa có tóm tắt";

    [Required]
    [StringLength(1000)]
    public string PosterURL { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Trailer { get; set; } = string.Empty;

    public int? CountryID { get; set; }

    [ForeignKey(nameof(CountryID))]
    public Country? Country { get; set; }

    public int? LanguageID { get; set; }

    [ForeignKey(nameof(LanguageID))]
    public Language? Language { get; set; }

    public ICollection<MovieGenre> MovieGenres { get; set; }
        = new List<MovieGenre>();

    public ICollection<MovieCasts> MovieCasts { get; set; }
        = new List<MovieCasts>();

    public ICollection<MovieDirectors> MovieDirectors { get; set; }
        = new List<MovieDirectors>();

    public ICollection<Showtimes> Showtimes { get; set; }
        = new List<Showtimes>();
}