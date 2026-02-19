namespace TaxApi.Models;

public class AuditLog
{
    public int Id { get; set; }
    public int? ClientId { get; set; }
    public int? TaxSubmissionId { get; set; }
    public AuditEventType EventType { get; set; }
    public string Action { get; set; } = null!;
    public string? PerformedBy { get; set; }
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Client? Client { get; set; }
    public TaxSubmission? TaxSubmission { get; set; }
}

public enum AuditEventType
{
    Submission = 1,
    StatusChange = 2,
    ValidationFailure = 3,
    SystemError = 4,
    Calculation = 5
}
