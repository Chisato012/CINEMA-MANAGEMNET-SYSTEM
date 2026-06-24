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

    public int HallNumber { get; set; }

    public decimal BasePrice { get; set; }

    public int MovieId{get; set;}

    [ForeignKey("MovieId")]
    public MovieViewModel movie{get;set;}


}
