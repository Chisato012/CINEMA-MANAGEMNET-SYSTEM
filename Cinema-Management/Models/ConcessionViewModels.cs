using System.ComponentModel.DataAnnotations;

namespace Cinema_Management.Models;

public class ConcessionsViewModel
{
    public IReadOnlyList<Combo> Items { get; set; } = Array.Empty<Combo>();
    public ConcessionForm Form { get; set; } = new();
    public IReadOnlyList<string> Categories { get; set; } = ConcessionCategories.All;
}

public class ConcessionForm
{
    public int ComboID { get; set; }

    [Required(ErrorMessage = "Ten mon la bat buoc.")]
    [StringLength(150, ErrorMessage = "Ten mon toi da 150 ky tu.")]
    public string ComboName { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Gia phai lon hon 0.")]
    public decimal ComboPrice { get; set; }

    [StringLength(500, ErrorMessage = "Mo ta toi da 500 ky tu.")]
    public string? Description { get; set; }

    [StringLength(1000, ErrorMessage = "Duong dan anh toi da 1000 ky tu.")]
    public string? ImageUrl { get; set; }

    [Required(ErrorMessage = "Danh muc la bat buoc.")]
    public string Category { get; set; } = "Other";

    [Range(0, int.MaxValue, ErrorMessage = "Ton kho phai lon hon hoac bang 0.")]
    public int StockQuantity { get; set; }
}

public class ConcessionSaleRequest
{
    public List<ConcessionSaleItem> Items { get; set; } = new();
}

public class ConcessionSaleItem
{
    public int ComboID { get; set; }
    public int Quantity { get; set; }
}

public static class ConcessionCategories
{
    public static readonly string[] All = { "Drinks", "Popcorn", "Combos", "Other" };

    public static bool IsValid(string? category) =>
        !string.IsNullOrWhiteSpace(category)
        && All.Contains(category.Trim(), StringComparer.OrdinalIgnoreCase);

    public static string Normalize(string category) =>
        All.First(c => string.Equals(c, category.Trim(), StringComparison.OrdinalIgnoreCase));
}
