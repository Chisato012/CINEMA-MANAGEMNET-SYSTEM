namespace Cinema_Management.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Showtimes
{
    [Key]
    public int ShowtimeID{get ; set;}

    public DateTime StartTime{get; set;}

    public DateTime EndTime{get; set;}

    public DateTime Date{get; set;}

    public int RoomID { get; set; }

    [NotMapped]
    public int HallNumber { get; set; }

    public decimal BasePrice { get; set; }

    [Column("MovieID")]
    public int MovieID{get; set;}

    [NotMapped]
    public int MovieId
    {
        get => MovieID;
        set => MovieID = value;
    }

    [ForeignKey(nameof(MovieID))]
    public MovieViewModel movie{get;set;}
}
