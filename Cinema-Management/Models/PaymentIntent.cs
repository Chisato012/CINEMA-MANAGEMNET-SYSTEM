using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cinema_Management.Models;

// SEPAY DYNAMIC PAYMENT: stores a pending payment before SePay webhook confirms it.
[Table("PaymentIntents")]
public class PaymentIntent
{
    [Key]
    public int PaymentIntentID { get; set; }

    [Required]
    public int UserID { get; set; }

    [Required]
    public int MovieID { get; set; }

    [Required]
    public int ShowtimeID { get; set; }

    public int? BookingID { get; set; }

    [Required]
    [StringLength(100)]
    public string PaymentReference { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal ExpectedAmount { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    [Required]
    [StringLength(500)]
    public string SelectedSeatCodes { get; set; } = string.Empty;

    public string? SelectedCombosJson { get; set; }

    public long? SePayTransactionID { get; set; }

    [StringLength(255)]
    public string? SePayReferenceCode { get; set; }

    public string? SePayContent { get; set; }

    public string? WebhookPayload { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? PaidAtUtc { get; set; }

    public User? User { get; set; }
    public MovieViewModel? Movie { get; set; }
    public Showtimes? Showtime { get; set; }
    public Booking? Booking { get; set; }
}
