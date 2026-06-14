using System.ComponentModel.DataAnnotations;

namespace Cinema_Management.Models.Validation;

public class MinimumAgeAttribute : ValidationAttribute
{
    private readonly int _minimumAge;

    public MinimumAgeAttribute(int minimumAge)
    {
        _minimumAge = minimumAge;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success;
        }

        if (value is not DateTime birthDate)
        {
            return new ValidationResult("Ngay sinh khong hop le");
        }

        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;

        if (birthDate.Date > today.AddYears(-age))
        {
            age--;
        }

        if (age < _minimumAge)
        {
            return new ValidationResult(ErrorMessage ?? $"Ban phai du {_minimumAge} tuoi de dang ky");
        }

        return ValidationResult.Success;
    }
}
