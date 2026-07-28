namespace Cinema_Management.Models;

public sealed class StaffScheduleViewModel
{
    public List<MovieViewModel> Movies { get; set; } = new();

    public List<StaffRoomSummaryViewModel> Rooms { get; set; } = new();
}

public sealed class StaffRoomSummaryViewModel
{
    public int RoomID { get; set; }

    public string RoomName { get; set; } = string.Empty;

    public int SeatCount { get; set; }

    public string DisplayType { get; set; } = "STANDARD";

    public string TagCssClass { get; set; } = "tag-standard";
}

public sealed class StaffHallsViewModel
{
    public List<StaffHallViewModel> Rooms { get; set; } = new();
}

public sealed class StaffHallViewModel
{
    public int RoomID { get; set; }

    public string RoomName { get; set; } = string.Empty;

    public int SeatCount { get; set; }

    public int RegularCount { get; set; }

    public int VipCount { get; set; }

    public int CoupleCount { get; set; }

    public int OrderedCount { get; set; }

    public int ReservationCount { get; set; }

    public int ColumnCount { get; set; }

    public List<StaffSeatRowViewModel> Rows { get; set; } = new();

    public List<StaffReservationViewModel> Reservations { get; set; } = new();
}

public sealed class StaffSeatRowViewModel
{
    public string RowLabel { get; set; } = string.Empty;

    public List<StaffSeatViewModel> Seats { get; set; } = new();
}

public sealed class StaffSeatViewModel
{
    public int SeatID { get; set; }

    public string SeatCode { get; set; } = string.Empty;

    public string SeatNumber { get; set; } = string.Empty;

    public int ColumnSpan { get; set; } = 1;

    public string SeatType { get; set; } = string.Empty;

    public string SeatStatus { get; set; } = string.Empty;

    public bool IsPlaceholder { get; set; }
}

public sealed class StaffReservationViewModel
{
    public int BookingID { get; set; }

    public int RoomID { get; set; }

    public string RoomName { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public string MovieTitle { get; set; } = string.Empty;

    public DateTime BookingDate { get; set; }

    public DateTime ShowtimeStart { get; set; }

    public string SeatCodes { get; set; } = string.Empty;

    public int TicketCount { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = string.Empty;
}
