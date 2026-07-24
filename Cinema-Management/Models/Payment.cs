
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cinema_Management.Models
{
    [Table("Payments")]
    public class Payment
    {
        [Key]
        public int PaymentID { get; set; }

        [Required]
        public int BookingID { get; set; }

        [Required]
        public int MethodID { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        [Required]
        [StringLength(10)]
        public string Status { get; set; } = "Pending";

        public Booking? Booking { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
    }
}

