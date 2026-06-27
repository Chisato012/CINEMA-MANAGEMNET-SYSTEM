using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Cinema_Management.Data;
using Cinema_Management.Models;
using Cinema_Management.ViewModels;
using System.Text.Json.Serialization;

namespace Cinema_Management.Controllers;


public class StaffController : Controller
{
    private readonly ApplicationDbContext _context;
    public StaffController(ApplicationDbContext context) => _context = context;

    private static readonly string[] AgeRatings = { "P", "K", "T13", "T16", "T18", "C" };
    private const decimal DefaultBasePrice = 90000m;
    private const string DefaultPosterUrl = "/img/poster/yourname400x600.png";
    private const string DefaultTrailerUrl = "#";
    

    public async Task<IActionResult> Index()
    {
        ViewBag.ActiveTab = "scheduling";
        var movies = await _context.Movies
            .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
            .OrderByDescending(m => m.ReleaseDate)
            .ToListAsync();

        var viewModel = new StaffScheduleViewModel
        {
            Movies = movies,
            Rooms = await LoadRoomSummariesAsync()
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> GetSchedule(DateTime date)
    {
        var scheduleDate = date.Date;
        var showtimes = await _context.Showtimes
            .AsNoTracking()
            .Include(s => s.movie)
            .Where(s => s.Date == scheduleDate)
            .OrderBy(s => s.RoomID)
            .ThenBy(s => s.StartTime)
            .Select(s => new
            {
                s.ShowtimeID,
                s.MovieID,
                MovieTitle = s.movie.Title,
                s.RoomID,
                HallNumber = s.RoomID,
                StartTime = s.StartTime.ToString("HH:mm"),
                EndTime = s.EndTime.ToString("HH:mm"),
                Duration = EF.Functions.DateDiffMinute(s.StartTime, s.EndTime),
                s.BasePrice
            })
            .ToListAsync();

        return Ok(showtimes);
    }

    [HttpGet]
    public async Task<IActionResult> GetScreeningRoomState(int roomId, int? showtimeId)
    {
        if (roomId <= 0)
        {
            return BadRequest(new { message = "Phong chieu khong hop le." });
        }

        var roomExists = await _context.Database
            .SqlQueryRaw<int>(
                "SELECT CASE WHEN EXISTS (SELECT 1 FROM Rooms WHERE RoomID = @roomId) THEN 1 ELSE 0 END AS [Value]",
                new SqlParameter("@roomId", roomId))
            .SingleAsync();

        if (roomExists == 0)
        {
            return NotFound(new { message = "Khong tim thay phong chieu trong database." });
        }

        var showtimes = await _context.Showtimes
            .AsNoTracking()
            .Where(s => s.RoomID == roomId)
            .OrderBy(s => s.StartTime)
            .Select(s => new ScreeningRoomShowtimeViewModel
            {
                ShowtimeID = s.ShowtimeID,
                MovieTitle = s.movie.Title,
                StartTime = s.StartTime,
                EndTime = s.EndTime
            })
            .ToListAsync();

        if (showtimeId.HasValue && !showtimes.Any(s => s.ShowtimeID == showtimeId.Value))
        {
            return BadRequest(new { message = "Suat chieu khong thuoc phong chieu da chon." });
        }

        var selectedShowtimeId = showtimeId.HasValue && showtimes.Any(s => s.ShowtimeID == showtimeId.Value)
            ? showtimeId.Value
            : showtimes.FirstOrDefault()?.ShowtimeID;

        var seatRows = await LoadScreeningSeatRowsAsync(roomId, selectedShowtimeId);
        var seats = seatRows
            .OrderBy(seat => GetSeatRowLabel(seat.SeatCode))
            .ThenBy(seat => GetSeatNumberSort(seat.SeatCode))
            .Select(seat => new ScreeningRoomSeatViewModel
            {
                SeatID = seat.SeatID,
                SeatCode = seat.SeatCode,
                SeatNumber = GetSeatNumberText(seat.SeatCode),
                SeatType = seat.SeatType,
                ReservationState = seat.ReservationState,
                ColumnSpan = GetSeatColumnSpan(seat.SeatType)
            })
            .ToList();

        var counts = new ScreeningRoomSeatCountsViewModel
        {
            Total = seats.Count,
            Regular = seats.Count(seat => seat.SeatType == "Regular"),
            Vip = seats.Count(seat => seat.SeatType == "VIP"),
            Couple = seats.Count(seat => seat.SeatType == "Couple"),
            Reserved = seats
                .Where(seat => seat.ReservationState == "reserved")
                .Select(seat => seat.SeatID)
                .Distinct()
                .Count(),
            Pending = seats
                .Where(seat => seat.ReservationState == "pending")
                .Select(seat => seat.SeatID)
                .Distinct()
                .Count()
        };
        counts.Available = Math.Max(0, counts.Total - counts.Reserved - counts.Pending);
        var reservations = selectedShowtimeId.HasValue
            ? await LoadScreeningReservationsAsync(roomId, selectedShowtimeId.Value)
            : new List<ScreeningRoomReservationViewModel>();

        return Ok(new ScreeningRoomStateViewModel
        {
            RoomID = roomId,
            SelectedShowtimeID = selectedShowtimeId,
            Showtimes = showtimes,
            Rows = BuildScreeningSeatRows(seats),
            Counts = counts,
            Reservations = reservations
        });
    }

    [HttpPost]
    public async Task<IActionResult> SaveSchedule([FromBody] SaveScheduleRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value != null && x.Value.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value!.Errors
                        .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage)
                            ? e.Exception?.Message ?? "Invalid value"
                            : e.ErrorMessage)
                        .ToArray()
                );

            return BadRequest(new
            {
                message = "Dữ liệu không hợp lệ",
                errors
            });
        }

        if (request == null)
        {
            return BadRequest(new
            {
                message = "Dữ liệu không hợp lệ",
                errors = new Dictionary<string, string[]>
                {
                    ["request"] = new[] { "Request body is empty or invalid JSON." }
                }
            });
        }

        var scheduleDate = request.Date.Date;
        var roomIds = await LoadRoomIdsAsync();
        var invalidItems = request.Items
            .Select((item, index) => new { Item = item, Index = index })
            .Where(x => x.Item.MovieID <= 0 || x.Item.RoomID <= 0)
            .ToList();

        if (invalidItems.Any())
        {
            return BadRequest(new
            {
                message = "Dữ liệu không hợp lệ",
                errors = invalidItems.ToDictionary(
                    x => $"items[{x.Index}]",
                    x => new[]
                    {
                        x.Item.MovieID <= 0 ? "MovieID must be a valid integer." : "",
                        x.Item.RoomID <= 0 ? "RoomID must be a valid integer." : ""
                    }.Where(error => !string.IsNullOrWhiteSpace(error)).ToArray())
            });
        }

        var movieIds = request.Items.Select(i => i.MovieID).Distinct().ToList();
        var movies = await _context.Movies
            .Where(m => movieIds.Contains(m.MovieId))
            .Select(m => new { m.MovieId, m.Duration })
            .ToDictionaryAsync(m => m.MovieId);

        if (movies.Count != movieIds.Count)
        {
            return BadRequest(new { message = "Mot hoac nhieu phim khong ton tai trong database." });
        }

        var parsedEntries = new List<SaveScheduleEntry>();
        foreach (var item in request.Items)
        {
            var roomId = item.RoomID;
            if (!roomIds.Contains(roomId))
            {
                return BadRequest(new { message = "Phong chieu khong hop le." });
            }

            if (!TryParseScheduleDateTime(request.Date, item.StartTime, out var startTime))
            {
                return BadRequest(new { message = "Gio bat dau khong hop le." });
            }

            var movie = movies[item.MovieID];
            var endTime = TryParseScheduleDateTime(request.Date, item.EndTime, out var parsedEndTime)
                ? parsedEndTime
                : startTime.AddMinutes(movie.Duration);

            if (endTime <= startTime)
            {
                return BadRequest(new { message = "Gio ket thuc phai lon hon gio bat dau." });
            }

            parsedEntries.Add(new SaveScheduleEntry
            {
                ShowtimeID = item.ShowtimeID,
                MovieID = item.MovieID,
                RoomID = roomId,
                Date = scheduleDate,
                StartTime = startTime,
                EndTime = endTime,
                BasePrice = item.BasePrice > 0 ? item.BasePrice : DefaultBasePrice
            });
        }

        var oldShowtimes = await _context.Showtimes
            .Where(s => s.Date == scheduleDate)
            .ToListAsync();

        var incomingIds = parsedEntries
            .Where(item => item.ShowtimeID > 0)
            .Select(item => item.ShowtimeID)
            .ToList();
        var duplicateIncomingIds = incomingIds
            .GroupBy(id => id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateIncomingIds.Any())
        {
            return BadRequest(new { message = $"Trung ShowtimeID trong payload: {string.Join(", ", duplicateIncomingIds)}." });
        }

        var oldShowtimesById = oldShowtimes.ToDictionary(showtime => showtime.ShowtimeID);
        var missingShowtimeIds = incomingIds
            .Where(id => !oldShowtimesById.ContainsKey(id))
            .ToList();

        if (missingShowtimeIds.Any())
        {
            return BadRequest(new { message = $"ShowtimeID khong ton tai trong ngay dang chon: {string.Join(", ", missingShowtimeIds)}." });
        }

        var ticketedShowtimeIds = (await _context.Database
            .SqlQueryRaw<int>(
                """
                SELECT DISTINCT t.ShowtimeID AS [Value]
                FROM Tickets t
                INNER JOIN Showtimes s ON s.ShowtimeID = t.ShowtimeID
                WHERE s.[Date] = @scheduleDate
                """,
                new SqlParameter("@scheduleDate", scheduleDate))
            .ToListAsync())
            .ToHashSet();

        foreach (var entry in parsedEntries.Where(entry => entry.ShowtimeID > 0))
        {
            var existing = oldShowtimesById[entry.ShowtimeID];
            if (ticketedShowtimeIds.Contains(existing.ShowtimeID)
                && (existing.MovieID != entry.MovieID
                    || existing.RoomID != entry.RoomID
                    || existing.StartTime != entry.StartTime
                    || existing.EndTime != entry.EndTime))
            {
                return BadRequest(new { message = $"Suat chieu {existing.ShowtimeID} da co ve, khong the doi phim/phong/gio chieu." });
            }

            existing.MovieID = entry.MovieID;
            existing.RoomID = entry.RoomID;
            existing.HallNumber = entry.RoomID;
            existing.Date = entry.Date;
            existing.StartTime = entry.StartTime;
            existing.EndTime = entry.EndTime;
            existing.BasePrice = entry.BasePrice;
        }

        var finalEntries = oldShowtimes
            .Where(showtime => ticketedShowtimeIds.Contains(showtime.ShowtimeID) && !incomingIds.Contains(showtime.ShowtimeID))
            .Select(showtime => new SaveScheduleEntry
            {
                ShowtimeID = showtime.ShowtimeID,
                MovieID = showtime.MovieID,
                RoomID = showtime.RoomID,
                Date = showtime.Date,
                StartTime = showtime.StartTime,
                EndTime = showtime.EndTime,
                BasePrice = showtime.BasePrice
            })
            .Concat(parsedEntries)
            .ToList();

        var hasOverlap = finalEntries
            .GroupBy(s => s.RoomID)
            .Any(group =>
            {
                var ordered = group.OrderBy(s => s.StartTime).ToList();
                return ordered.Zip(ordered.Skip(1), (current, next) => current.EndTime > next.StartTime).Any(x => x);
            });

        if (hasOverlap)
        {
            return BadRequest(new { message = "Lich chieu bi trung gio trong cung phong chieu." });
        }

        var removableShowtimes = oldShowtimes
            .Where(showtime => !ticketedShowtimeIds.Contains(showtime.ShowtimeID) && !incomingIds.Contains(showtime.ShowtimeID))
            .ToList();
        var showtimesToAdd = parsedEntries
            .Where(item => item.ShowtimeID <= 0)
            .Select(item => new Showtimes
            {
                MovieID = item.MovieID,
                RoomID = item.RoomID,
                HallNumber = item.RoomID,
                Date = item.Date,
                StartTime = item.StartTime,
                EndTime = item.EndTime,
                BasePrice = item.BasePrice
            })
            .ToList();

        _context.Showtimes.RemoveRange(removableShowtimes);
        _context.Showtimes.AddRange(showtimesToAdd);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Khong luu duoc lich chieu vao database.",
                detail = ex.GetBaseException().Message
            });
        }

        return Ok(new
        {
            message = "Da luu lich chieu vao database.",
            count = finalEntries.Count,
            created = showtimesToAdd.Count,
            updated = parsedEntries.Count(item => item.ShowtimeID > 0),
            deleted = removableShowtimes.Count
        });
    }

    public async Task<IActionResult> Halls()
    {
        ViewBag.ActiveTab = "halls";
        var viewModel = await LoadHallsViewModelAsync();
        return View(viewModel);
    }

    public IActionResult Concessions()
    {
        ViewBag.ActiveTab = "concessions";
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateMovie(StaffMovieForm form)
    {
        if (!IsMovieFormValid(form))
        {
            TempData["AlertError"] = "Du lieu phim khong hop le.";
            return RedirectToAction(nameof(Index));
        }

        var movie = new MovieViewModel
        {
            Title = form.Title.Trim(),
            Duration = form.Duration,
            PosterURL = GetPosterUrl(form),
            Synopsis = string.IsNullOrWhiteSpace(form.Synopsis)
                ? "Chua co tom tat"
                : form.Synopsis.Trim(),
            Trailer = GetTrailerUrl(form),
            ReleaseDate = form.ReleaseDate ?? DateTime.Today,
            AgeRating = string.IsNullOrWhiteSpace(form.AgeRating)
                ? "T13"
                : form.AgeRating.Trim(),
            MovieGenres = new List<MovieGenre>(),
            MovieCasts = new List<MovieCasts>(),
            MovieDirectors = new List<MovieDirectors>(),
            Showtimes = new List<Showtimes>()
        };

        _context.Movies.Add(movie);
        await _context.SaveChangesAsync();
        await SaveMovieGenresAsync(movie.MovieId, form.Genre);
        await _context.SaveChangesAsync();

        TempData["AlertSuccess"] = "Da them phim vao database.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditMovie(StaffMovieForm form)
    {
        if (form.MovieId <= 0 || !IsMovieFormValid(form))
        {
            TempData["AlertError"] = "Du lieu phim khong hop le.";
            return RedirectToAction(nameof(Index));
        }

        var existing = await _context.Movies
            .Include(m => m.MovieGenres)
            .FirstOrDefaultAsync(m => m.MovieId == form.MovieId);

        if (existing == null)
        {
            return NotFound();
        }

        existing.Title = form.Title.Trim();
        existing.Duration = form.Duration;
        existing.AgeRating = string.IsNullOrWhiteSpace(form.AgeRating)
            ? "T13"
            : form.AgeRating.Trim();
        existing.Synopsis = string.IsNullOrWhiteSpace(form.Synopsis)
            ? "Chua co tom tat"
            : form.Synopsis.Trim();
        existing.PosterURL = GetPosterUrl(form);
        existing.Trailer = GetTrailerUrl(form);
        existing.ReleaseDate = form.ReleaseDate ?? existing.ReleaseDate;

        await SaveMovieGenresAsync(existing.MovieId, form.Genre);
        await _context.SaveChangesAsync();

        TempData["AlertSuccess"] = "Da cap nhat phim.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateMovie(int id)
    {
        var movie = await _context.Movies.FindAsync(id);
        if (movie == null)
        {
            return NotFound();
        }

        var showtimes = await _context.Showtimes
            .Where(s => s.MovieID == id)
            .ToListAsync();
        var genres = await _context.MovieGenres
            .Where(mg => mg.MovieID == id)
            .ToListAsync();
        var casts = await _context.MovieCasts
            .Where(mc => mc.MovieID == id)
            .ToListAsync();
        var directors = await _context.MovieDirectors
            .Where(md => md.MovieID == id)
            .ToListAsync();

        _context.Showtimes.RemoveRange(showtimes);
        _context.MovieGenres.RemoveRange(genres);
        _context.MovieCasts.RemoveRange(casts);
        _context.MovieDirectors.RemoveRange(directors);
        _context.Movies.Remove(movie);
        await _context.SaveChangesAsync();

        TempData["AlertSuccess"] = "Da xoa phim khoi database.";
        return RedirectToAction(nameof(Index));
    }

    // private async Task LoadDropdowns(MovieViewModel? movie = null)
    // {
    //     ViewBag.Countries = new SelectList(await _context.Countries.ToListAsync(), "CountryID", "CountryName", movie?.CountryID);
    //     ViewBag.Languages = new SelectList(await _context.Languages.ToListAsync(), "LanguageID", "LanguageName", movie?.LanguageID);
    //     ViewBag.AgeRatings = new SelectList(AgeRatings, movie?.AgeRating);
    // }

    public sealed class SaveScheduleRequest
    {
        public DateTime Date { get; set; }

        public List<SaveScheduleItem> Items { get; set; } = new();
    }

    public sealed class SaveScheduleItem
    {
        public int ShowtimeID { get; set; }

        public int MovieID { get; set; }

        public int RoomID { get; set; }

        [JsonIgnore]
        public int MovieId
        {
            get => MovieID;
            set => MovieID = value;
        }

        [JsonIgnore]
        public int RoomId
        {
            get => RoomID;
            set => RoomID = value;
        }

        [JsonIgnore]
        public int HallNumber
        {
            get => RoomID;
            set => RoomID = value;
        }

        public string StartTime { get; set; } = string.Empty;

        public string EndTime { get; set; } = string.Empty;

        public decimal BasePrice { get; set; }
    }

    private sealed class SaveScheduleEntry
    {
        public int ShowtimeID { get; set; }

        public int MovieID { get; set; }

        public int RoomID { get; set; }

        public DateTime Date { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public decimal BasePrice { get; set; }
    }

    public sealed class StaffMovieForm
    {
        public int MovieId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Synopsis { get; set; } = string.Empty;

        public string Trailer { get; set; } = string.Empty;

        public string TrailerURL { get; set; } = string.Empty;

        public string PosterURL { get; set; } = string.Empty;

        public DateTime? ReleaseDate { get; set; }

        public short Duration { get; set; }

        public string AgeRating { get; set; } = string.Empty;

        public string Genre { get; set; } = string.Empty;
    }

    private static bool IsMovieFormValid(StaffMovieForm form)
    {
        return form != null
               && !string.IsNullOrWhiteSpace(form.Title)
               && form.Duration > 0;
    }

    private static bool TryParseScheduleDateTime(DateTime scheduleDate, string value, out DateTime result)
    {
        if (TimeSpan.TryParse(value, out var timeOfDay) && !value.Contains('-') && !value.Contains('/') && !value.Contains('T'))
        {
            result = scheduleDate.Date.Add(timeOfDay);
            return true;
        }

        if (DateTime.TryParse(value, out result))
        {
            return true;
        }

        if (TimeSpan.TryParse(value, out timeOfDay))
        {
            result = scheduleDate.Date.Add(timeOfDay);
            return true;
        }

        result = default;
        return false;
    }

    private static string GetPosterUrl(StaffMovieForm form)
    {
        return string.IsNullOrWhiteSpace(form.PosterURL)
            ? DefaultPosterUrl
            : form.PosterURL.Trim();
    }

    private static string GetTrailerUrl(StaffMovieForm form)
    {
        if (!string.IsNullOrWhiteSpace(form.Trailer))
        {
            return form.Trailer.Trim();
        }

        return string.IsNullOrWhiteSpace(form.TrailerURL)
            ? DefaultTrailerUrl
            : form.TrailerURL.Trim();
    }

    private async Task SaveMovieGenresAsync(int movieId, string genreText)
    {
        var existingLinks = await _context.MovieGenres
            .Where(mg => mg.MovieID == movieId)
            .ToListAsync();
        _context.MovieGenres.RemoveRange(existingLinks);

        if (string.IsNullOrWhiteSpace(genreText))
        {
            return;
        }

        var genreNames = genreText
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var genreName in genreNames)
        {
            var genre = await _context.Genres
                .FirstOrDefaultAsync(g => g.Name.ToLower() == genreName.ToLower());

            if (genre == null)
            {
                genre = new Genre { Name = genreName };
                _context.Genres.Add(genre);
                await _context.SaveChangesAsync();
            }

            _context.MovieGenres.Add(new MovieGenre
            {
                MovieID = movieId,
                GenreID = genre.GenreID
            });
        }
    }

    private async Task<List<StaffRoomSummaryViewModel>> LoadRoomSummariesAsync()
    {
        var rooms = await _context.Database
            .SqlQueryRaw<RoomSummaryRow>(
                """
                SELECT r.RoomID, r.RoomName, COUNT(s.SeatID) AS SeatCount
                FROM Rooms r
                LEFT JOIN Seats s ON s.RoomID = r.RoomID
                GROUP BY r.RoomID, r.RoomName
                ORDER BY r.RoomID
                """)
            .ToListAsync();

        return rooms
            .Select(room => new StaffRoomSummaryViewModel
            {
                RoomID = room.RoomID,
                RoomName = room.RoomName,
                SeatCount = room.SeatCount,
                DisplayType = GetRoomDisplayType(room.RoomName),
                TagCssClass = GetRoomTagCssClass(room.RoomName)
            })
            .ToList();
    }

    private async Task<HashSet<int>> LoadRoomIdsAsync()
    {
        var roomIds = await _context.Database
            .SqlQueryRaw<int>("SELECT RoomID AS [Value] FROM Rooms")
            .ToListAsync();

        return roomIds.ToHashSet();
    }

    private async Task<StaffHallsViewModel> LoadHallsViewModelAsync()
    {
        var rooms = await LoadRoomSummariesAsync();
        var seats = await _context.Database
            .SqlQueryRaw<SeatRow>(
                """
                SELECT SeatID, RoomID, SeatCode, SeatType, SeatStatus
                FROM Seats
                ORDER BY RoomID, SeatCode
                """)
            .ToListAsync();
        var reservations = await LoadReservationRowsAsync();

        return new StaffHallsViewModel
        {
            Rooms = rooms.Select(room =>
            {
                var roomSeats = seats
                    .Where(seat => seat.RoomID == room.RoomID)
                    .OrderBy(seat => GetSeatRowLabel(seat.SeatCode))
                    .ThenBy(seat => GetSeatNumberSort(seat.SeatCode))
                    .ToList();
                var columnCount = GetSeatColumnCount(roomSeats);
                var seatRows = BuildSeatRows(roomSeats, columnCount);

                return new StaffHallViewModel
                {
                    RoomID = room.RoomID,
                    RoomName = room.RoomName,
                    SeatCount = room.SeatCount,
                    RegularCount = roomSeats.Count(seat => seat.SeatType == "Regular"),
                    VipCount = roomSeats.Count(seat => seat.SeatType == "VIP"),
                    CoupleCount = roomSeats.Count(seat => seat.SeatType == "Couple"),
                    OrderedCount = roomSeats.Count(seat => seat.SeatStatus != "Not Order"),
                    ReservationCount = reservations.Count(reservation => reservation.RoomID == room.RoomID),
                    ColumnCount = columnCount,
                    Rows = seatRows,
                    Reservations = reservations
                        .Where(reservation => reservation.RoomID == room.RoomID)
                        .OrderByDescending(reservation => reservation.ShowtimeStart)
                        .ThenByDescending(reservation => reservation.BookingID)
                        .ToList()
                };
            }).ToList()
        };
    }

    private async Task<List<StaffReservationViewModel>> LoadReservationRowsAsync()
    {
        var rows = await _context.Database
            .SqlQueryRaw<ReservationRow>(
                """
                SELECT
                    b.BookingID,
                    r.RoomID,
                    r.RoomName,
                    u.FullName AS CustomerName,
                    u.Email AS CustomerEmail,
                    m.Title AS MovieTitle,
                    b.BookingDate,
                    st.StartTime AS ShowtimeStart,
                    STRING_AGG(CONVERT(nvarchar(max), se.SeatCode), ', ') AS SeatCodes,
                    COUNT(t.TicketID) AS TicketCount,
                    b.TotalAmount,
                    b.Status
                FROM Tickets t
                INNER JOIN Bookings b ON b.BookingID = t.BookingID
                INNER JOIN Users u ON u.UserID = b.UserID
                INNER JOIN Showtimes st ON st.ShowtimeID = t.ShowtimeID
                INNER JOIN Movies m ON m.MovieID = st.MovieID
                INNER JOIN Rooms r ON r.RoomID = st.RoomID
                INNER JOIN Seats se ON se.SeatID = t.SeatID
                GROUP BY
                    b.BookingID,
                    r.RoomID,
                    r.RoomName,
                    u.FullName,
                    u.Email,
                    m.Title,
                    b.BookingDate,
                    st.StartTime,
                    b.TotalAmount,
                    b.Status
                ORDER BY st.StartTime DESC, b.BookingID DESC
                """)
            .ToListAsync();

        return rows.Select(row => new StaffReservationViewModel
            {
                BookingID = row.BookingID,
                RoomID = row.RoomID,
                RoomName = row.RoomName,
                CustomerName = row.CustomerName,
                CustomerEmail = row.CustomerEmail,
                MovieTitle = row.MovieTitle,
                BookingDate = row.BookingDate,
                ShowtimeStart = row.ShowtimeStart,
                SeatCodes = row.SeatCodes,
                TicketCount = row.TicketCount,
                TotalAmount = row.TotalAmount,
                Status = row.Status
            })
            .ToList();
    }

    private static int GetSeatColumnCount(List<SeatRow> roomSeats)
    {
        return roomSeats
            .GroupBy(seat => GetSeatRowLabel(seat.SeatCode))
            .Select(group =>
            {
                var row = group.ToList();
                return row.Any(seat => seat.SeatType == "Couple")
                ? row.Sum(GetSeatColumnSpan)
                : row.Select(seat => GetSeatNumberSort(seat.SeatCode))
                    .Where(number => number != int.MaxValue)
                    .DefaultIfEmpty(0)
                    .Max();
            })
            .DefaultIfEmpty(0)
            .Max();
    }

    private static List<StaffSeatRowViewModel> BuildSeatRows(List<SeatRow> roomSeats, int columnCount)
    {
        var sourceRows = roomSeats
            .GroupBy(seat => GetSeatRowLabel(seat.SeatCode))
            .OrderBy(group => group.Key)
            .Select(group => group
                .OrderBy(seat => GetSeatNumberSort(seat.SeatCode))
                .ToList())
            .ToList();

        return sourceRows.Select(row =>
        {
            var rowLabel = GetSeatRowLabel(row[0].SeatCode);
            var usesSequentialSpans = row.Any(seat => seat.SeatType == "Couple");
            var seats = usesSequentialSpans
                ? BuildSequentialSeatCells(row, columnCount)
                : BuildPositionedSeatCells(row, columnCount);

            return new StaffSeatRowViewModel
            {
                RowLabel = rowLabel,
                Seats = seats
            };
        }).ToList();
    }

    private static List<StaffSeatViewModel> BuildSequentialSeatCells(List<SeatRow> row, int columnCount)
    {
        var seats = row.Select(CreateSeatViewModel).ToList();
        var usedColumns = seats.Sum(seat => seat.ColumnSpan);

        while (usedColumns < columnCount)
        {
            seats.Add(CreateSeatPlaceholder());
            usedColumns++;
        }

        return seats;
    }

    private static List<StaffSeatViewModel> BuildPositionedSeatCells(List<SeatRow> row, int columnCount)
    {
        var seatsByColumn = row
            .Select(seat => new { Seat = seat, Column = GetSeatNumberSort(seat.SeatCode) })
            .Where(item => item.Column != int.MaxValue)
            .ToDictionary(item => item.Column, item => item.Seat);

        var cells = new List<StaffSeatViewModel>();
        for (var column = 1; column <= columnCount; column++)
        {
            cells.Add(seatsByColumn.TryGetValue(column, out var seat)
                ? CreateSeatViewModel(seat)
                : CreateSeatPlaceholder());
        }

        return cells;
    }

    private static StaffSeatViewModel CreateSeatViewModel(SeatRow seat)
    {
        return new StaffSeatViewModel
        {
            SeatID = seat.SeatID,
            SeatCode = seat.SeatCode,
            SeatNumber = GetSeatNumberText(seat.SeatCode),
            ColumnSpan = GetSeatColumnSpan(seat),
            SeatType = seat.SeatType,
            SeatStatus = seat.SeatStatus
        };
    }

    private static StaffSeatViewModel CreateSeatPlaceholder()
    {
        return new StaffSeatViewModel
        {
            ColumnSpan = 1,
            IsPlaceholder = true
        };
    }

    private static string GetRoomDisplayType(string roomName)
    {
        if (roomName.Contains("IMAX", StringComparison.OrdinalIgnoreCase))
        {
            return "IMAX";
        }

        if (roomName.Contains("VIP", StringComparison.OrdinalIgnoreCase))
        {
            return "VIP";
        }

        if (roomName.Contains("Deluxe", StringComparison.OrdinalIgnoreCase))
        {
            return "DELUXE";
        }

        return "STANDARD";
    }

    private static string GetRoomTagCssClass(string roomName)
    {
        return GetRoomDisplayType(roomName) switch
        {
            "IMAX" => "tag-imax",
            "VIP" => "tag-vip",
            "DELUXE" => "tag-deluxe",
            _ => "tag-standard"
        };
    }

    private static string GetSeatRowLabel(string seatCode)
    {
        var row = new string(seatCode.TakeWhile(char.IsLetter).ToArray());
        return string.IsNullOrWhiteSpace(row) ? "?" : row;
    }

    private static int GetSeatNumberSort(string seatCode)
    {
        var digits = new string(seatCode.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var number) ? number : int.MaxValue;
    }

    private static string GetSeatNumberText(string seatCode)
    {
        var number = GetSeatNumberSort(seatCode);
        return number == int.MaxValue ? seatCode : number.ToString();
    }

    private static int GetSeatColumnSpan(SeatRow seat)
    {
        return seat.SeatType == "Couple" ? 2 : 1;
    }

    private static int GetSeatColumnSpan(string seatType)
    {
        return seatType == "Couple" ? 2 : 1;
    }

    private async Task<List<ScreeningSeatRow>> LoadScreeningSeatRowsAsync(int roomId, int? showtimeId)
    {
        var roomParam = new SqlParameter("@roomId", roomId);
        var showtimeParam = new SqlParameter("@showtimeId", showtimeId.HasValue ? showtimeId.Value : DBNull.Value);

        return await _context.Database
            .SqlQueryRaw<ScreeningSeatRow>(
                """
                WITH SeatStates AS (
                    SELECT
                        s.SeatID,
                        s.SeatCode,
                        s.SeatType,
                        MAX(CASE
                            WHEN b.Status = N'Confirmed' THEN 2
                            WHEN b.Status = N'Pending' THEN 1
                            ELSE 0
                        END) AS StateRank
                    FROM Seats s
                    LEFT JOIN Tickets t
                        ON t.SeatID = s.SeatID
                        AND t.ShowtimeID = @showtimeId
                    LEFT JOIN Bookings b
                        ON b.BookingID = t.BookingID
                    WHERE s.RoomID = @roomId
                    GROUP BY s.SeatID, s.SeatCode, s.SeatType
                )
                SELECT
                    SeatID,
                    SeatCode,
                    SeatType,
                    CASE
                        WHEN StateRank = 2 THEN N'reserved'
                        WHEN StateRank = 1 THEN N'pending'
                        ELSE N'available'
                    END AS ReservationState
                FROM SeatStates
                ORDER BY SeatCode
                """,
                roomParam,
                showtimeParam)
            .ToListAsync();
    }

    private async Task<List<ScreeningRoomReservationViewModel>> LoadScreeningReservationsAsync(int roomId, int showtimeId)
    {
        var roomParam = new SqlParameter("@roomId", roomId);
        var showtimeParam = new SqlParameter("@showtimeId", showtimeId);

        var rows = await _context.Database
            .SqlQueryRaw<ScreeningReservationTicketRow>(
                """
                SELECT
                    b.BookingID,
                    u.FullName AS CustomerName,
                    u.Email AS CustomerEmail,
                    b.BookingDate,
                    b.Status,
                    se.SeatCode,
                    b.TotalAmount
                FROM Tickets t
                INNER JOIN Bookings b ON b.BookingID = t.BookingID
                INNER JOIN Users u ON u.UserID = b.UserID
                INNER JOIN Showtimes st ON st.ShowtimeID = t.ShowtimeID
                INNER JOIN Seats se ON se.SeatID = t.SeatID
                WHERE st.RoomID = @roomId
                    AND t.ShowtimeID = @showtimeId
                ORDER BY b.BookingDate DESC, b.BookingID DESC, se.SeatCode
                """,
                roomParam,
                showtimeParam)
            .ToListAsync();

        return rows
            .GroupBy(row => new
            {
                row.BookingID,
                row.CustomerName,
                row.CustomerEmail,
                row.BookingDate,
                row.Status,
                row.TotalAmount
            })
            .Select(group => new ScreeningRoomReservationViewModel
            {
                BookingID = group.Key.BookingID,
                CustomerName = group.Key.CustomerName,
                CustomerEmail = group.Key.CustomerEmail,
                BookingDate = group.Key.BookingDate,
                Status = group.Key.Status,
                SeatCodes = group
                    .Select(row => row.SeatCode)
                    .Distinct()
                    .OrderBy(GetSeatRowLabel)
                    .ThenBy(GetSeatNumberSort)
                    .ToList(),
                TicketCount = group
                    .Select(row => row.SeatCode)
                    .Distinct()
                    .Count(),
                TotalAmount = group.Key.TotalAmount
            })
            .OrderByDescending(reservation => reservation.BookingDate)
            .ThenByDescending(reservation => reservation.BookingID)
            .ToList();
    }

    private static List<ScreeningRoomSeatRowViewModel> BuildScreeningSeatRows(List<ScreeningRoomSeatViewModel> seats)
    {
        return seats
            .GroupBy(seat => GetSeatRowLabel(seat.SeatCode))
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var row = group
                    .OrderBy(seat => GetSeatNumberSort(seat.SeatCode))
                    .ToList();

                return new ScreeningRoomSeatRowViewModel
                {
                    RowLabel = group.Key,
                    Seats = BuildScreeningSeatCells(row)
                };
            })
            .ToList();
    }

    private static List<ScreeningRoomSeatViewModel> BuildScreeningSeatCells(List<ScreeningRoomSeatViewModel> row)
    {
        var cells = new List<ScreeningRoomSeatViewModel>();
        var expectedSeatNumber = 1;

        foreach (var seat in row)
        {
            var seatNumber = GetSeatNumberSort(seat.SeatCode);
            if (seatNumber != int.MaxValue)
            {
                while (expectedSeatNumber < seatNumber)
                {
                    cells.Add(CreateScreeningSeatPlaceholder());
                    expectedSeatNumber++;
                }

                expectedSeatNumber = seatNumber + 1;
            }

            cells.Add(seat);
        }

        return cells;
    }

    private static ScreeningRoomSeatViewModel CreateScreeningSeatPlaceholder()
    {
        return new ScreeningRoomSeatViewModel
        {
            ColumnSpan = 1,
            IsPlaceholder = true
        };
    }

    private sealed class RoomSummaryRow
    {
        public int RoomID { get; set; }

        public string RoomName { get; set; } = string.Empty;

        public int SeatCount { get; set; }
    }

    private sealed class SeatRow
    {
        public int SeatID { get; set; }

        public int RoomID { get; set; }

        public string SeatCode { get; set; } = string.Empty;

        public string SeatType { get; set; } = string.Empty;

        public string SeatStatus { get; set; } = string.Empty;
    }

    public sealed class ScreeningSeatRow
    {
        public int SeatID { get; set; }

        public string SeatCode { get; set; } = string.Empty;

        public string SeatType { get; set; } = string.Empty;

        public string ReservationState { get; set; } = "available";
    }

    public sealed class ScreeningReservationTicketRow
    {
        public int BookingID { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerEmail { get; set; } = string.Empty;

        public DateTime BookingDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public string SeatCode { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }
    }

    private sealed class ReservationRow
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
}
