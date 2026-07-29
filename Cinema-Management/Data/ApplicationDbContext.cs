
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
    public DbSet<PaymentIntent> PaymentIntents { get; set; } = null!;
    public DbSet<Review> Reviews { get; set; } = null!;

    public DbSet<Booking> Bookings { set; get; }

    public DbSet<BookingCombo> BookingCombos { get; set; }

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

        modelBuilder.Entity<MovieCasts>()
            .HasOne(mc => mc.Person)
            .WithMany()
            .HasForeignKey(mc => mc.PersonId);

        modelBuilder.Entity<MovieDirectors>()
            .HasOne(md => md.Person)
            .WithMany()
            .HasForeignKey(md => md.PersonId);

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
        modelBuilder.Entity<BookingCombo>(entity =>
        {
            entity.ToTable("BookingCombos");

            // Khóa chính gồm BookingID và ComboID.
            entity.HasKey(item => new
            {
                item.BookingID,
                item.ComboID
            });

            entity.Property(item => item.UnitPrice)
                .HasColumnType("decimal(10,2)");

            entity.HasOne(item => item.Booking)
                .WithMany(booking => booking.BookingCombos)
                .HasForeignKey(item => item.BookingID);

            entity.HasOne(item => item.Combo)
                .WithMany(combo => combo.BookingCombos)
                .HasForeignKey(item => item.ComboID);
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

        modelBuilder.Entity<PaymentIntent>(entity =>
        {
            entity.Property(e => e.ExpectedAmount).HasColumnType("decimal(10,2)");
            entity.Property(e => e.PaymentReference).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Pending");
            entity.Property(e => e.SelectedSeatCodes).HasMaxLength(500);
            entity.Property(e => e.SePayReferenceCode).HasMaxLength(255);

            entity.HasIndex(e => e.PaymentReference)
                .IsUnique()
                .HasDatabaseName("UQ_PaymentIntents_PaymentReference");

            entity.HasIndex(e => e.SePayTransactionID)
                .IsUnique()
                .HasFilter("[SePayTransactionID] IS NOT NULL")
                .HasDatabaseName("UQ_PaymentIntents_SePayTransactionID");

            entity.HasCheckConstraint(
                "CK_PaymentIntents_Status",
                "[Status] IN ('Pending', 'Processing', 'Success', 'Failed', 'Expired')"
            );

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserID)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PaymentIntents_Users");

            entity.HasOne(e => e.Movie)
                .WithMany()
                .HasForeignKey(e => e.MovieID)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PaymentIntents_Movies");

            entity.HasOne(e => e.Showtime)
                .WithMany()
                .HasForeignKey(e => e.ShowtimeID)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PaymentIntents_Showtimes");

            entity.HasOne(e => e.Booking)
                .WithMany()
                .HasForeignKey(e => e.BookingID)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_PaymentIntents_Bookings");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(review => review.ReviewID);

            entity.Property(review => review.Content)
                .HasColumnName("Content")
                .HasMaxLength(2000);

            entity.Property(review => review.Rating)
                .HasColumnType("decimal(3,2)");

            entity.Property(review => review.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Visible");

            entity.Property(review => review.CreatedAt)
                .HasDefaultValueSql("sysutcdatetime()");

            entity.HasOne(review => review.User)
                .WithMany()
                .HasForeignKey(review => review.UserID)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Reviews_Users");

            entity.HasOne(review => review.Movie)
                .WithMany()
                .HasForeignKey(review => review.MovieID)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Reviews_Movies");

            entity.HasOne<Review>()
                .WithMany()
                .HasForeignKey(review => review.ParentReviewID)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Reviews_Parent");
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

            entity.Property(u => u.PasswordResetTokenHash)
                .HasMaxLength(64);
        });
    }
}
