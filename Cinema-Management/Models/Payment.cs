using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cinema_Management.Models;

[Table("Payments")]
public class Payment
{
    [Key]
    public int PaymentID { get; set; }

    public int BookingID { get; set; }

    public int MethodID { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    [Required]
    [MaxLength(10)]
    public string Status { get; set; } = "Pending";
}
