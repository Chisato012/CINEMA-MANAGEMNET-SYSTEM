using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cinema_Management.Models;

[Table("Reviews")]
public class Review
{
    [Key]
    public int ReviewID { get; set; }

    public int UserID { get; set; }

    public int MovieID { get; set; }

    public int? ParentReviewID { get; set; }

    [Required]
    [Column("Content")]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;

    [Column(TypeName = "decimal(3,2)")]
    public decimal? Rating { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Visible";

    public User? User { get; set; }

    public MovieViewModel? Movie { get; set; }
}
