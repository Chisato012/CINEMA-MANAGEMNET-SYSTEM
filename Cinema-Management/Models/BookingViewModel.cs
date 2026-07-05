using System.Text.Json.Serialization;

namespace Cinema_Management.Models;

/// <summary>
/// Server-owned state for one booking journey. Only identifiers and quantities supplied by the
/// browser are accepted; all prices and totals are calculated from this model on the server.
/// </summary>
public sealed class BookingViewModel
{
    public string BookingId { get; set; } = Guid.NewGuid().ToString("N");
    public int CurrentStep { get; set; } = 1;
    public DateTime BookingExpiresAtUtc { get; set; } = DateTime.UtcNow.AddMinutes(10);

    public int MovieId { get; set; } = 1;
    public int? ShowtimeId { get; set; }
    public string MovieTitle { get; set; } 
    public string Director { get; set; } 
    public string Cast { get; set; } 
    public string Synopsis { get; set; } 
    public string AgeRating { get; set; }
    public int DurationMinutes { get; set; } = 166;
    public string PosterURL { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;

    public string SelectedDate { get; set; } = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
    public string SelectedTime { get; set; } 
    public string CinemaFormat { get; set; }
    public List<string> AvailableDates { get; set; } = [];
    public List<string> AvailableTimes { get; set; } = [];
    public List<string> AvailableFormats { get; set; } = [];
    public List<ShowtimeChoiceViewModel> ShowtimeChoices { get; set; } = [];

    public List<string> SelectedSeats { get; set; } = [];
    public List<string> OccupiedSeats { get; set; } =
        ["A6", "A7", "B10", "C4", "D8", "D9", "F2", "G9"];

    public decimal StandardTicketPrice { get; set; } = 18.50m;
    public decimal SweetboxTicketPrice { get; set; } = 30.00m;
    public decimal ConvenienceFeePerTicket { get; set; } = 1.50m;
    public decimal TaxRate { get; set; } = 0.08m;

    public List<ConcessionItemViewModel> Concessions { get; set; } =
    [
        new() { Id = "solo", Name = "Cinephile Solo", Description = "Large butter popcorn + 1L signature soda", Price = 14.00m, Icon = "🍿" },
        new() { Id = "duo", Name = "Director's Duo", Description = "2 premium hot dogs + 2 large sodas", Price = 22.50m, Icon = "🌭" },
        new() { Id = "popcorn", Name = "Classic Popcorn XL", Description = "Extra-large butter popcorn with free refill", Price = 9.50m, Icon = "🍿" },
        new() { Id = "soda", Name = "Signature Soda 1L", Description = "Ice-cold fountain drink of your choice", Price = 6.00m, Icon = "🥤" }
    ];

    public bool IsPaid { get; set; }
    public string? ConfirmationNumber { get; set; }

    [JsonIgnore]
    public decimal TicketSubtotal => SelectedSeats.Sum(SeatPrice);

    [JsonIgnore]
    public decimal ConvenienceFee => SelectedSeats.Count * ConvenienceFeePerTicket;

    [JsonIgnore]
    public decimal ConcessionSubtotal => Concessions.Sum(item => item.Price * item.Quantity);

    [JsonIgnore]
    public decimal PreTaxTotal => TicketSubtotal + ConvenienceFee + ConcessionSubtotal;

    [JsonIgnore]
    public decimal Tax => Math.Round(PreTaxTotal * TaxRate, 2, MidpointRounding.AwayFromZero);

    [JsonIgnore]
    public decimal GrandTotal => PreTaxTotal + Tax;

    public decimal SeatPrice(string seatId) =>
        seatId.StartsWith('H') ? SweetboxTicketPrice : StandardTicketPrice;
}

public sealed class ConcessionItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public sealed class ShowtimeChoiceViewModel
{
    public int ShowtimeId { get; set; }
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
}

public sealed class ScheduleRequest
{
    public string Date { get; init; } = string.Empty;
    public string Time { get; init; } = string.Empty;
    public string Format { get; init; } = string.Empty;
}

public sealed class SeatRequest
{
    public string SeatId { get; init; } = string.Empty;
}

public sealed class ConcessionRequest
{
    public string ProductId { get; init; } = string.Empty;
    public int Quantity { get; init; }
}

public sealed class StepRequest
{
    public int Step { get; init; }
}
