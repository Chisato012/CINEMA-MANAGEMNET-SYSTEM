namespace Cinema_Management.Models;

public class AdminAnalyticsViewModel
{
    public string? SelectedMonth { get; set; }
    public int? SelectedMovieId { get; set; }

    public decimal TotalRevenue { get; set; }
    public int TotalTicketsSold { get; set; }
    public int ConfirmedBookings { get; set; }

    public decimal AverageRevenuePerTicket =>
        TotalTicketsSold == 0 ? 0 : TotalRevenue / TotalTicketsSold;

    public List<AnalyticsMonthOption> Months { get; set; } = new();
    public List<AnalyticsMovieOption> Movies { get; set; } = new();

    public List<MonthlyRevenueItem> RevenueByMonth { get; set; } = new();
    public List<MonthlyTicketItem> TicketsByMonth { get; set; } = new();
    public List<MovieRevenueItem> RevenueByMovie { get; set; } = new();
}

public class AnalyticsMonthOption
{
    public string Value { get; set; } = string.Empty; // yyyy-MM
    public string Text { get; set; } = string.Empty;
}

public class AnalyticsMovieOption
{
    public int MovieId { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class MonthlyRevenueItem
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}

public class MonthlyTicketItem
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Label { get; set; } = string.Empty;
    public int TicketCount { get; set; }
}

public class MovieRevenueItem
{
    public int MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}