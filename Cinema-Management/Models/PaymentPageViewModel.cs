namespace Cinema_Management.Models;

public sealed class PaymentPageViewModel
{
    public string PaymentReference { get; set; } = string.Empty;
    public string MovieTitle { get; set; } = string.Empty;

    public string CinemaFormat {get; set;} =string.Empty;
    public string SelectedDate { get; set; } = string.Empty;
    public string SelectedTime { get; set; } = string.Empty;
    public List<string> SelectedSeats { get; set; } = [];

    public List<ConcessionItemViewModel> Concessions { get; set; } = [];

    public decimal TicketSubtotal { get; set; }
    public decimal ConcessionSubtotal { get; set; }
    public decimal TotalAmount { get; set; }

    public string QrImageUrl { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }

    // Chỉ hiện nút giả lập trong môi trường Development.
    public bool ShowDevelopmentPaymentButton { get; set; }
}