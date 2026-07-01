
using Cinema_Management.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Cinema_Management.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<MovieViewModel> Movies { get; set; }
    public DbSet<Genre> Genres { get; set; }
    public DbSet<MovieGenre> MovieGenres { get; set; }
    public DbSet<Language> Languages { get; set; }
    public DbSet<Country> Countries { get; set; }
    public DbSet<MovieCasts> MovieCasts { get; set; }
    public DbSet<MovieDirectors> MovieDirectors { get; set; }
    public DbSet<Person> Persons { get; set; }
    public DbSet<Showtimes> Showtimes { get; set; }
    public DbSet<Combo> Combos { get; set; }
    public DbSet<Payment> Payments { get; set; } = null!;

    public DbSet<Booking> Bookings { set; get; }

    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<Seat> Seats { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<PaymentMethod> PaymentMethods { get; set; }
    public DbSet<SeatTypePricing> SeatTypePricings { get; set; }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Khai báo khóa chính kép cho bảng trung gian
        modelBuilder.Entity<MovieGenre>()
            .HasKey(mg => new { mg.MovieID, mg.GenreID });

        modelBuilder.Entity<MovieCasts>()
            .HasKey(mc => new { mc.MovieID, mc.PersonId });

        modelBuilder.Entity<MovieDirectors>()
            .HasKey(md => new { md.MovieID, md.PersonId });

        //Khai báo quan hệ bảng Booking
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingID);

            entity.Property(e => e.TotalAmount)
                 .HasColumnType("decimal(10,2)");

            entity.Property(e => e.Status)
                 .HasMaxLength(10)
                 .HasDefaultValue("Pending");

            entity.HasCheckConstraint(
               "CK_Bookings_Status",
               "[Status] = 'Pending' OR [Status] = 'Confirmed' OR [Status] = 'Cancelled'"
            );

            entity.HasOne(e => e.User)
                 .WithMany(u => u.Bookings)
                 .HasForeignKey(e => e.UserID)
                 .HasConstraintName("FK_Bookings_Users");
        });

        // =========================
        // Showtime
        // =========================
        modelBuilder.Entity<Showtimes>(entity =>
        {
            entity.Property(e => e.Date).HasColumnType("date");
            entity.Property(e => e.BasePrice).HasColumnType("decimal(10,2)");

            entity.HasOne(e => e.Movie)
                .WithMany(e => e.Showtimes)
                .HasForeignKey(e => e.MovieID)
                .HasConstraintName("FK_Showtimes_Movies");

            entity.HasOne(e => e.Room)
                .WithMany(e => e.Showtimes)
                .HasForeignKey(e => e.RoomID)
                .HasConstraintName("FK_Showtimes_Rooms");
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasIndex(e => e.TicketCode).IsUnique();

            entity.HasIndex(e => new { e.ShowtimeID, e.SeatID })
                .IsUnique()
                .HasDatabaseName("UQ_Tickets_Showtime_Seat");

            entity.HasOne(e => e.Booking)
                .WithMany(e => e.Tickets)
                .HasForeignKey(e => e.BookingID)
                .HasConstraintName("FK_Tickets_Bookings");

            entity.HasOne(e => e.Showtime)
                .WithMany(e => e.Tickets)
                .HasForeignKey(e => e.ShowtimeID)
                .HasConstraintName("FK_Tickets_Showtimes");

            entity.HasOne(e => e.Seat)
                .WithMany(e => e.Tickets)
                .HasForeignKey(e => e.SeatID)
                .HasConstraintName("FK_Tickets_Seats");
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.HasCheckConstraint(
                "CK_Seats_SeatType",
                "[SeatType] IN ('Regular', 'VIP', 'Couple')"
            );

            entity.HasOne(e => e.Room)
                .WithMany(e => e.Seats)
                .HasForeignKey(e => e.RoomID)
                .HasConstraintName("FK_Seats_Rooms");

            entity.HasOne(e => e.SeatTypePricing)
                .WithMany(e => e.Seats)
                .HasForeignKey(e => e.SeatType)
                .HasPrincipalKey(e => e.SeatType)
                .HasConstraintName("FK_Seats_SeatTypePricing");
        });

        // =========================
        // Room - Seat - Pricing
        // =========================
        modelBuilder.Entity<SeatTypePricing>(entity =>
        {
            entity.Property(e => e.Multiplier)
                .HasColumnType("decimal(4,2)")
                .HasDefaultValue(1.00m);

            entity.HasCheckConstraint(
                "CK_SeatTypePricing_SeatType",
                "[SeatType] IN ('Regular', 'VIP', 'Couple')"
            );
        });

        // =========================
        // Payment
        // =========================
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(e => e.Amount).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Status).HasDefaultValue("Pending");
            entity.Property(p => p.PaymentDate)
                .IsRequired();

            entity.HasCheckConstraint(
                "CK_Payments_Status",
                "[Status] IN ('Pending', 'Success', 'Failed')"
            );

            entity.HasOne(e => e.Booking)
                .WithMany(e => e.Payments)
                .HasForeignKey(e => e.BookingID)
                .HasConstraintName("FK_Payments_Bookings");

            entity.HasOne(e => e.PaymentMethod)
                .WithMany(e => e.Payments)
                .HasForeignKey(e => e.MethodID)
                .HasConstraintName("FK_Payments_PaymentMethods");
        });


        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email)
                .IsUnique();

            entity.HasIndex(u => new { u.ExternalProvider, u.ExternalProviderKey })
                .IsUnique()
                .HasFilter("[ExternalProvider] IS NOT NULL AND [ExternalProviderKey] IS NOT NULL");

            entity.Property(u => u.Email)
                .HasMaxLength(200);

            entity.Property(u => u.PasswordHash)
                .HasMaxLength(512)
                .IsRequired(false);

            entity.Property(u => u.Role)
                .HasMaxLength(20)
                .HasDefaultValue("KhachHang");

            entity.Property(u => u.ExternalProvider)
                .HasMaxLength(50);

            entity.Property(u => u.ExternalProviderKey)
                .HasMaxLength(200);

            entity.Property(u => u.EmailVerificationTokenHash)
                .HasMaxLength(64);
        });
    }
}
