using TaxApi.Data;
using TaxApi.Models;

namespace TaxApi.Services;

public interface IAuditService
{
    Task LogAsync(
        AuditEventType eventType,
        string action,
        string? performedBy = null,
        int? submissionId = null,
        string? details = null);
}

public class AuditService : IAuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db) => _db = db;

    public async Task LogAsync(
        AuditEventType eventType,
        string action,
        string? performedBy = null,
        int? submissionId = null,
        string? details = null)
    {
        var log = new AuditLog
        {
            EventType        = eventType,
            Action           = action,
            PerformedBy      = performedBy,
            TaxSubmissionId  = submissionId,
            Details          = details,
            Timestamp        = DateTime.UtcNow
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }
}
