
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
    }
}
