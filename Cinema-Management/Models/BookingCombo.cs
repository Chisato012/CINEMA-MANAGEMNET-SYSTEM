using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cinema_Management.Models;

[Table("BookingCombos")]
public sealed class BookingCombo
{
    public int BookingID { get; set; } // Khóa ngoại đến Bookings.
    public int ComboID { get; set; }   // Khóa ngoại đến Combos.

    [Range(1, 10)]
    public int Quantity { get; set; }  // Số lượng khách đã mua.

    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; } // Giá tại thời điểm thanh toán.

    public Booking? Booking { get; set; }
    public Combo? Combo { get; set; }
}