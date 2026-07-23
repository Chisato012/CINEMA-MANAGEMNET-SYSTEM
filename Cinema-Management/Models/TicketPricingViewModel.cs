namespace Cinema_Management.Models;

public sealed class TicketPricingViewModel
{
    public required IReadOnlyList<MoviePricingViewModel> MovieFormats { get; init; }

    public required IReadOnlyList<string> Footnotes { get; init; }
}

public sealed class MoviePricingViewModel
{
    public required string Id { get; init; }

    public required string TabLabel { get; init; }

    public required IReadOnlyList<SeatPricingViewModel> SeatPrices { get; init; }
}

public sealed class SeatPricingViewModel
{
    public required SeatType SeatType { get; init; }

    public required decimal BasePrice { get; init; }

    public required decimal NormalDay { get; init; }

    public required decimal WeekendOrHoliday { get; init; }
}

public enum SeatType
{
    Standard,
    Vip,
    Sweetbox
}
