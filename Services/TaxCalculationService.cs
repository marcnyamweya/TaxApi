using TaxApi.Models;

namespace TaxApi.Services;

public interface ITaxCalculationService
{
    (decimal liability, decimal effectiveRate) Calculate(TaxSubmission submission);
}

public class TaxCalculationService : ITaxCalculationService
{
    // ── 2024 US Federal Income Tax Brackets (MFJ single filer) ──────────────
    private static readonly (decimal UpTo, decimal Rate)[] PersonalBrackets2024 =
    {
        (11_600m,  0.10m),
        (47_150m,  0.12m),
        (100_525m, 0.22m),
        (191_950m, 0.24m),
        (243_725m, 0.32m),
        (609_350m, 0.35m),
        (decimal.MaxValue, 0.37m)
    };

    // ── US Federal Corporate Tax: flat 21% (TCJA 2017) ──────────────────────
    private const decimal CorporateRate = 0.21m;

    // ── Standard EU VAT (20%) — overridden by submission's VatRate if set ───
    private const decimal DefaultVatRate = 0.20m;

    public (decimal liability, decimal effectiveRate) Calculate(TaxSubmission submission)
    {
        return submission.TaxType switch
        {
            TaxType.PersonalIncome => CalculatePersonalIncome(submission),
            TaxType.Corporate      => CalculateCorporate(submission),
            TaxType.VAT            => CalculateVat(submission),
            _ => throw new ArgumentOutOfRangeException(nameof(submission.TaxType), "Unsupported tax type.")
        };
    }

    // Progressive bracket calculation (IRS 2024 single-filer brackets)
    private static (decimal, decimal) CalculatePersonalIncome(TaxSubmission sub)
    {
        var income = sub.TaxableIncome;
        if (income <= 0) return (0m, 0m);

        decimal tax = 0m;
        decimal previous = 0m;

        foreach (var (upTo, rate) in PersonalBrackets2024)
        {
            if (income <= previous) break;

            var bracketTop   = Math.Min(income, upTo);
            var taxableSlice = bracketTop - previous;
            tax     += taxableSlice * rate;
            previous = upTo;
        }

        var effectiveRate = income > 0 ? Math.Round(tax / income, 4) : 0m;
        return (Math.Round(tax, 2), effectiveRate);
    }

    // Flat 21% corporate rate on taxable income
    private static (decimal, decimal) CalculateCorporate(TaxSubmission sub)
    {
        var income = sub.TaxableIncome;
        if (income <= 0) return (0m, 0m);

        var tax = Math.Round(income * CorporateRate, 2);
        return (tax, CorporateRate);
    }

    // Output VAT on vatable sales
    private static (decimal, decimal) CalculateVat(TaxSubmission sub)
    {
        var sales   = sub.VatableSales ?? sub.GrossIncome;
        var rate    = sub.VatRate.HasValue ? sub.VatRate.Value / 100m : DefaultVatRate;
        var tax     = Math.Round(sales * rate, 2);
        var effRate = sales > 0 ? Math.Round(tax / sales, 4) : 0m;
        return (tax, effRate);
    }
}
