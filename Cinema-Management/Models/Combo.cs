using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cinema_Management.Models;

[Table("Combos")]
public class Combo
{
    [Key]
    public int ComboID { get; set; }

    [Required]
    [MaxLength(150)]
    public string ComboName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    [Range(0.01, double.MaxValue)]
    public decimal ComboPrice { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(1000)]
    public string? ImageUrl { get; set; }

    [Required]
    [MaxLength(30)]
    public string Category { get; set; } = "Other";

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    public bool IsActive { get; set; } = true;
}
