using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cinema_Management.Models
{
    [Table("SeatTypePricing")]
    public class SeatTypePricing
    {
        [Key]
        [Required]
        [StringLength(10)]
        public string SeatType { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(4,2)")]
        public decimal Multiplier { get; set; } = 1.00m;

        public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    }
}
