namespace Cinema_Management.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Showtimes
{
    [Key]
        public int ShowtimeID { get; set; }

        [Required]
        public int MovieID { get; set; }

        [Required]
        public int RoomID { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime Date { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal BasePrice { get; set; }

        public MovieViewModel? Movie { get; set; }
        public Room? Room { get; set; }

        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();


}