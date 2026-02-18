using TaxApi.DTOs;
using TaxApi.Models;

namespace TaxApi.Validation;

public static class SubmissionValidator
{
    public record ValidationResult(bool IsValid, List<string> Errors);

    public static ValidationResult Validate(CreateTaxSubmissionRequest req)
    {
        var errors = new List<string>();

        if (req.GrossIncome < 0)
            errors.Add("GrossIncome must be non-negative.");

        if (req.Deductions < 0)
            errors.Add("Deductions must be non-negative.");

        if (req.Deductions > req.GrossIncome)
            errors.Add("Deductions cannot exceed GrossIncome.");

        if (req.TaxYear < 2000 || req.TaxYear > DateTime.UtcNow.Year)
            errors.Add($"TaxYear must be between 2000 and {DateTime.UtcNow.Year}.");

        if (!Enum.IsDefined(typeof(TaxType), req.TaxType))
            errors.Add("Invalid TaxType specified.");

        // VAT-specific rules
        if (req.TaxType == TaxType.VAT)
        {
            if (req.VatRate.HasValue && (req.VatRate < 0 || req.VatRate > 100))
                errors.Add("VatRate must be between 0 and 100.");
        }

        // Corporate — deductions should not exceed 90% of gross (business rule)
        if (req.TaxType == TaxType.Corporate && req.GrossIncome > 0)
        {
            var deductionRatio = req.Deductions / req.GrossIncome;
            if (deductionRatio > 0.90m)
                errors.Add("Corporate deductions cannot exceed 90% of gross income.");
        }

        return new ValidationResult(errors.Count == 0, errors);
    }
}
