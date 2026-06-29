using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    [Range(0.01, 999999999, ErrorMessage = "Gia mon phai lon hon 0.")]
    public decimal ComboPrice { get; set; }

}
