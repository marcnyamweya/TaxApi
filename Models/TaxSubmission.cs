namespace TaxApi.Models;

public class TaxSubmission
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public TaxType TaxType { get; set; }
    public int TaxYear { get; set; }

    // Income / Revenue fields
    public decimal GrossIncome { get; set; }
    public decimal Deductions { get; set; }
    public decimal TaxableIncome => GrossIncome - Deductions;

    // VAT-specific
    public decimal? VatableSales { get; set; }
    public decimal? VatRate { get; set; }

    // Calculated results (populated by TaxCalculationService)
    public decimal TaxLiability { get; set; }
    public decimal EffectiveRate { get; set; }

    public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ReviewerNotes { get; set; }

    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

public enum TaxType
{
    PersonalIncome = 1,
    Corporate = 2,
    VAT = 3
}

public enum SubmissionStatus
{
    Submitted = 1,
    UnderReview = 2,
    Approved = 3,
    Rejected = 4,
    Filed = 5
}
