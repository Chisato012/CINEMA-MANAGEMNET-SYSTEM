using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cinema_Management.Models;

[Table("Combos")]
public class Combo
{
    [Key]
    public int ComboID { get; set; }

    [Required(ErrorMessage = "Tên món không được để trống.")]
    [StringLength(150, ErrorMessage = "Tên món tối đa 150 ký tự.")]
    public string ComboName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    [Range(1, 99999999, ErrorMessage = "Giá món phải lớn hơn 0.")]
    public decimal ComboPrice { get; set; }

    [Range(0, int.MaxValue,
        ErrorMessage = "Số lượng không được nhỏ hơn 0.")]
    public int Quantity { get; set; }

    public ICollection<BookingCombo> BookingCombos { get; set; }
        = new List<BookingCombo>();
}