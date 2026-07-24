using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cinema_Management.Models
//Model tương ứng với room
{
    [Table("Rooms")]
    public class Room
    {
        [Key]
        public int RoomID { get; set; }

        [Required]
        [StringLength(100)]
        public string RoomName { get; set; } = string.Empty;

        public ICollection<Seat> Seats { get; set; } = new List<Seat>();
        public ICollection<Showtimes> Showtimes { get; set; } = new List<Showtimes>();
    }
}
