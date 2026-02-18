using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaxApi.Data;
using TaxApi.DTOs;
using TaxApi.Models;

namespace TaxApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuditLogsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuditLogsController(AppDbContext db) => _db = db;

    /// <summary>Returns audit logs, optionally filtered by eventType.</summary>
    [HttpGet]
    [ProducesResponseType<IEnumerable<AuditLogResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] AuditEventType? eventType, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var query = _db.AuditLogs.AsQueryable();
        if (eventType.HasValue) query = query.Where(a => a.EventType == eventType.Value);

        var total = await query.CountAsync();
        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogResponse(a.Id, a.TaxSubmissionId, a.Action,
                                              a.PerformedBy, a.Details, a.EventType, a.Timestamp))
            .ToListAsync();

        Response.Headers["X-Total-Count"] = total.ToString();
        return Ok(logs);
    }
}
