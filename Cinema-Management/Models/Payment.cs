using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cinema_Management.Models;

[Table("Payments")]
public class Payment
{
    [Key]
    public int PaymentID { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(10)]
    public string Status { get; set; } = string.Empty;

    public DateTime PaymentDate { get; set; }
}
