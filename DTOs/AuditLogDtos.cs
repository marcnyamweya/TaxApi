using TaxApi.Models;

namespace TaxApi.DTOs;

public record AuditLogResponse(
    int Id,
    int? TaxSubmissionId,
    string Action,
    string? PerformedBy,
    string? Details,
    AuditEventType EventType,
    DateTime Timestamp);
