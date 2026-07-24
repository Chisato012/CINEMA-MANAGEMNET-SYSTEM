namespace Cinema_Management.Models;

public sealed class OffersPageViewModel
{
    public IReadOnlyList<OfferViewModel> Offers { get; init; } = [];
    public IReadOnlyList<OfferViewModel> FeaturedOffers { get; init; } = [];
    public IReadOnlyList<OfferViewModel> ExpiringSoonOffers { get; init; } = [];
    public IReadOnlyList<OfferQuickBookingMovieViewModel> QuickBookingMovies { get; init; } = [];
}

public sealed record class OfferViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? Code { get; init; }
    public string Category { get; init; } = string.Empty;
    public string CategoryLabel { get; init; } = string.Empty;
    public string? Badge { get; init; }
    public string DiscountType { get; init; } = string.Empty;
    public decimal? DiscountValue { get; init; }
    public decimal? MaximumDiscount { get; init; }
    public decimal? MinimumOrder { get; init; }
    public string DisplayValue { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public bool IsFeatured { get; init; }
    public bool IsOnlineOnly { get; init; }
    public bool IsMemberOnly { get; init; }
    public bool IsStackable { get; init; }
    public IReadOnlyList<string> Terms { get; init; } = [];
    public string Status { get; init; } = "active";
    public string StatusLabel { get; init; } = string.Empty;
    public string ValidityLabel { get; init; } = string.Empty;
    public string RemainingLabel { get; init; } = string.Empty;
    public bool IsExpiringSoon { get; init; }
    public bool CanApply => Status == "active";
}

public sealed class OfferQuickBookingMovieViewModel
{
    public int MovieId { get; init; }
    public string Title { get; init; } = string.Empty;
    public IReadOnlyList<OfferQuickBookingShowtimeViewModel> Showtimes { get; init; } = [];
}

public sealed class OfferQuickBookingShowtimeViewModel
{
    public int ShowtimeId { get; init; }
    public string Date { get; init; } = string.Empty;
    public string DateLabel { get; init; } = string.Empty;
    public string Time { get; init; } = string.Empty;
    public string Format { get; init; } = "2D";
    public string RoomName { get; init; } = string.Empty;
}

public sealed class OfferValidationResult
{
    public bool IsValid { get; init; }
    public string Status { get; init; } = "invalid";
    public string Message { get; init; } = string.Empty;
    public OfferViewModel? Offer { get; init; }
}
