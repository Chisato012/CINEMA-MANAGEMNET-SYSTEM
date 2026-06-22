namespace Cinema_Management.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;

public class MovieViewModel
{
    [Key]
    public int MovieId { get; set; }
    
    // Basic information
    public string Title { get; set; }
    public short Duration { get; set; }
    public string PosterURL { get; set; }
    public string Synopsis { get; set; }
    public DateTime ReleaseDate { get; set; }
    public string AgeRating { get; set; }
    public string Trailer { get; set; }

    // Các thuộc tính hiển thị — không có trong DB, dữ liệu lấy từ bảng quan hệ
    [NotMapped]
    public string Genre { get; set; }
    [NotMapped]
    public string MovieCast { get; set; }
    [NotMapped]
    public string MovieDirector { get; set; }

    //Thuộc tính lịch chiếu
    public ICollection<Showtimes> Showtimes { get; set; }

    //Thêm thuộc tính ngôn ngữ
    public int? LanguageId { get; set; }
    public Language Language { get; set; }

    public int? CountryId { get; set; }
    public Country Country { get; set; }

    // Các bảng quan hệ
    public ICollection<MovieGenre> MovieGenres { get; set; }
    public ICollection<MovieCasts> MovieCasts { get; set; }
    public ICollection<MovieDirectors> MovieDirectors { get; set; }
}
