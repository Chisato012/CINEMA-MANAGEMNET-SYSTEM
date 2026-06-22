namespace Cinema_Management.Models;
using System.ComponentModel.DataAnnotations;

public class Country
{
    [Key]
    public int CountryId { get; set; }
    public string CountryName { get; set; }
}
