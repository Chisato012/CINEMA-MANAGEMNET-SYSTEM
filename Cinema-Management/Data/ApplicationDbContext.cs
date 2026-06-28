
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

        modelBuilder.Entity<Showtimes>(entity =>
        {
            entity.Property(s => s.Date)
                .HasColumnType("date");

            entity.Property(s => s.BasePrice)
                .HasColumnType("decimal(10,2)");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments");

            entity.HasKey(p => p.PaymentID);

            entity.Property(p => p.Amount)
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            entity.Property(p => p.PaymentDate)
                .IsRequired();

            entity.Property(p => p.Status)
                .HasMaxLength(10)
                .IsRequired()
                .HasDefaultValue("Pending");

            entity.Property(p => p.BookingID)
                .IsRequired();

            entity.Property(p => p.MethodID)
                .IsRequired();
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
