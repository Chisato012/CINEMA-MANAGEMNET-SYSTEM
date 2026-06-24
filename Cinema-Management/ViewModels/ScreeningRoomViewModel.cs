namespace Cinema_Management.ViewModels;

public sealed class ScreeningRoomStateViewModel
{
    public int RoomID { get; set; }

    public int? SelectedShowtimeID { get; set; }

    public List<ScreeningRoomShowtimeViewModel> Showtimes { get; set; } = new();

    public List<ScreeningRoomSeatRowViewModel> Rows { get; set; } = new();

    public ScreeningRoomSeatCountsViewModel Counts { get; set; } = new();
}

public sealed class ScreeningRoomShowtimeViewModel
{
    public int ShowtimeID { get; set; }

    public string MovieTitle { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public string Label => $"{StartTime:dd/MM HH:mm} - {MovieTitle}";
}

public sealed class ScreeningRoomSeatRowViewModel
{
    public string RowLabel { get; set; } = string.Empty;

    public List<ScreeningRoomSeatViewModel> Seats { get; set; } = new();
}

public sealed class ScreeningRoomSeatViewModel
{
    public int SeatID { get; set; }

    public string SeatCode { get; set; } = string.Empty;

    public string SeatNumber { get; set; } = string.Empty;

    public string SeatType { get; set; } = string.Empty;

    public string ReservationState { get; set; } = "available";

    public int ColumnSpan { get; set; } = 1;

    public bool IsPlaceholder { get; set; }
}

public sealed class ScreeningRoomSeatCountsViewModel
{
    public int Total { get; set; }

    public int Regular { get; set; }

    public int Vip { get; set; }

    public int Couple { get; set; }

    public int Reserved { get; set; }

    public int Pending { get; set; }

    public int Available { get; set; }
}
