using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cinema_Management.Models
{
    [Table("Tickets")]
    public class Ticket
    {
        [Key]
        public int TicketID { get; set; }

        [Required]
        public int BookingID { get; set; }

        [Required]
        public int ShowtimeID { get; set; }

        [Required]
        public int SeatID { get; set; }

        [Required]
        [StringLength(100)]
        public string TicketCode { get; set; } = string.Empty;

        public Booking? Booking { get; set; }
        public Showtimes? Showtime { get; set; }
        public Seat? Seat { get; set; }
    }
}
