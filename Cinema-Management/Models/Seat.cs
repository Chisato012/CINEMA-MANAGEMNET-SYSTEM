using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cinema_Management.Models
{
    [Table("Seats")]
    public class Seat
    {
        [Key]
        public int SeatID { get; set; }

        [Required]
        public int RoomID { get; set; }

        [Required]
        [StringLength(10)]
        public string SeatCode { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string SeatType { get; set; } = string.Empty;

        public Room? Room { get; set; }
        public SeatTypePricing? SeatTypePricing { get; set; }

        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
