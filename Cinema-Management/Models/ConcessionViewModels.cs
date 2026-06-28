namespace Cinema_Management.Models;

public class ConcessionsViewModel
{
    public IReadOnlyList<Combo> Items { get; set; } = Array.Empty<Combo>();
    public Combo Form { get; set; } = new();
    public decimal MonthlyRevenue { get; set; }
    public int TotalTransactions { get; set; }
    public decimal AverageOrderValue { get; set; }
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
