using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Cinema_Management.Models;

[Table("Combos")]
public class Combo
{
    [Key]
    public int ComboID { get; set; }

    [Required(ErrorMessage = "Ten mon khong duoc de trong.")]
    [StringLength(150, ErrorMessage = "Ten mon toi da 150 ky tu.")]
    public string ComboName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    [Range(0, 999999999, ErrorMessage = "Gia mon khong duoc am.")]
    public decimal ComboPrice { get; set; }

    public ICollection<BookingCombo> BookingCombos { get; set; } = new List<BookingCombo>();

    public int Quantity {get; set;}

}
