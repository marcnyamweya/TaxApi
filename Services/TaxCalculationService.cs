using TaxApi.Models;

namespace TaxApi.Services;

public interface ITaxCalculationService
{
    (decimal liability, decimal effectiveRate) Calculate(TaxSubmission submission);
}

public class TaxCalculationService : ITaxCalculationService
{
    // ── Kenya 2024 Personal Income Tax (Monthly PAYE Bands) ──────────────────
    private static readonly (decimal UpTo, decimal Rate)[] KenyaPersonalBrackets2024 =
    {
        (24_000m,  0.10m),    // 0 – 24,000: 10%
        (32_333m,  0.25m),    // 24,001 – 32,333: 25%
        (decimal.MaxValue, 0.30m)  // Above 32,333: 30%
    };

    // ── Kenya Personal Relief (annual: KES 28,800 or monthly: KES 2,400) ──────
    private const decimal AnnualPersonalRelief = 28_800m;

    // ── Kenya Corporate Tax Rate: 30% ────────────────────────────────────────
    private const decimal CorporateRate = 0.30m;

    // ── Kenya Standard VAT Rate: 16% ─────────────────────────────────────────
    private const decimal DefaultVatRate = 0.16m;

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

    // Kenya progressive bracket calculation with personal relief
    private static (decimal, decimal) CalculatePersonalIncome(TaxSubmission sub)
    {
        var income = sub.TaxableIncome;
        if (income <= 0) return (0m, 0m);

        decimal tax = 0m;
        decimal previous = 0m;

        foreach (var (upTo, rate) in KenyaPersonalBrackets2024)
        {
            if (income <= previous) break;

            var bracketTop   = Math.Min(income, upTo);
            var taxableSlice = bracketTop - previous;
            tax     += taxableSlice * rate;
            previous = upTo;
        }

        // Apply annual personal relief (KES 28,800)
        tax = Math.Max(0, tax - AnnualPersonalRelief);

        var effectiveRate = income > 0 ? Math.Round(tax / income, 4) : 0m;
        return (Math.Round(tax, 2), effectiveRate);
    }

    // Kenya flat 30% corporate tax rate on taxable income
    private static (decimal, decimal) CalculateCorporate(TaxSubmission sub)
    {
        var income = sub.TaxableIncome;
        if (income <= 0) return (0m, 0m);

        var tax = Math.Round(income * CorporateRate, 2);
        return (tax, CorporateRate);
    }

    // Kenya VAT (16% standard, or submission override)
    private static (decimal, decimal) CalculateVat(TaxSubmission sub)
    {
        var sales   = sub.VatableSales ?? sub.GrossIncome;
        var rate    = sub.VatRate.HasValue ? sub.VatRate.Value / 100m : DefaultVatRate;
        var tax     = Math.Round(sales * rate, 2);
        var effRate = sales > 0 ? Math.Round(tax / sales, 4) : 0m;
        return (tax, effRate);
    }
}
