using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cinema_Management.Models
{
    [Table("Bookings")]
    public class Booking        
    {
        [Key]
        public int BookingID;

        [Required]
        public int UserID { get; set; }

        [Required]
        public DateTime BookingDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(10)]
        public string Status { get; set; } = "Pending";

        // Navigation property
        [ForeignKey("UserID")]
        public User? User { get; set; }
        
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
        // public ICollection<BookingCombo> BookingCombos { get; set; } = new List<BookingCombo>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}

