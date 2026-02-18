namespace TaxApi.Models;

public class AuditLog
{
    public int Id { get; set; }
    public int? ClientId { get; set; }
    public int? TaxSubmissionId { get; set; }
    public AuditEventType EventType { get; set; }
    public string? Description { get; set; }
    public string? Details { get; set; } // JSON string for complex data
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? ActorIdentity { get; set; } // User or system identifier

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
