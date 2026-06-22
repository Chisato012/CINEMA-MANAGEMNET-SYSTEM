namespace Cinema_Management.Models;
using System.ComponentModel.DataAnnotations;

public class Language
{
    [Key]
    public int LanguageId { get; set; }
    public string LanguageName { get; set; }
}
