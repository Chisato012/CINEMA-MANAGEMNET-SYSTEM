using System.ComponentModel.DataAnnotations;

namespace Cinema_Management.Models.Validation;

public class BirthYearNotBeforeAttribute : ValidationAttribute
{
    private readonly int _minYear;

    public BirthYearNotBeforeAttribute(int minYear)
    {
        _minYear = minYear;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success;
        }

        if (value is not DateTime dob)
        {
            return new ValidationResult("Ngay sinh khong hop le");
        }

        if (dob.Year < _minYear)
        {
            return new ValidationResult(ErrorMessage ?? $"Nam sinh khong duoc nho hon nam {_minYear}");
        }

        return ValidationResult.Success;
    }
}
