using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TaxApi.Data;
using TaxApi.DTOs;
using TaxApi.Models;
using TaxApi.Services;
using TaxApi.Validation;

namespace TaxApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TaxSubmissionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITaxCalculationService _calculator;
    private readonly IAuditService _audit;

    public TaxSubmissionsController(AppDbContext db, ITaxCalculationService calculator, IAuditService audit)
    {
        _db         = db;
        _calculator = calculator;
        _audit      = audit;
    }

    /// <summary>Returns all submissions, optionally filtered by clientId or status.</summary>
    [HttpGet]
    [ProducesResponseType<IEnumerable<TaxSubmissionResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int? clientId, [FromQuery] SubmissionStatus? status)
    {
        var query = _db.TaxSubmissions.Include(s => s.Client).AsQueryable();

        if (clientId.HasValue) query = query.Where(s => s.ClientId == clientId.Value);
        if (status.HasValue)   query = query.Where(s => s.Status == status.Value);

        var results = await query.Select(s => ToResponse(s)).ToListAsync();
        return Ok(results);
    }

    /// <summary>Returns a single submission by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<TaxSubmissionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var s = await _db.TaxSubmissions.Include(s => s.Client).FirstOrDefaultAsync(s => s.Id == id);
        return s is null ? NotFound() : Ok(ToResponse(s));
    }

    /// <summary>Submits tax data for a client. Validates, calculates liability, and stores the record.</summary>
    [HttpPost]
    [ProducesResponseType<TaxSubmissionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateTaxSubmissionRequest req)
    {
        // Validate request
        var validation = SubmissionValidator.Validate(req);
        if (!validation.IsValid)
        {
            await _audit.LogAsync(AuditEventType.ValidationFailure, "SubmissionValidationFailed",
                performedBy: req.ClientId.ToString(),
                details: JsonSerializer.Serialize(validation.Errors));
            return BadRequest(new { errors = validation.Errors });
        }

        // Verify client exists
        var client = await _db.Clients.FindAsync(req.ClientId);
        if (client is null)
            return NotFound(new { error = $"Client {req.ClientId} not found." });

        // Build submission entity
        var submission = new TaxSubmission
        {
            ClientId     = req.ClientId,
            TaxType      = req.TaxType,
            TaxYear      = req.TaxYear,
            GrossIncome  = req.GrossIncome,
            Deductions   = req.Deductions,
            VatableSales = req.VatableSales,
            VatRate      = req.VatRate,
            Status       = SubmissionStatus.Submitted,
            SubmittedAt  = DateTime.UtcNow
        };

        // Calculate tax liability
        var (liability, effectiveRate) = _calculator.Calculate(submission);
        submission.TaxLiability  = liability;
        submission.EffectiveRate = effectiveRate;

        _db.TaxSubmissions.Add(submission);
        await _db.SaveChangesAsync();

        // Audit: submission created + calculation logged
        await _audit.LogAsync(AuditEventType.Submission, "TaxSubmissionCreated",
            performedBy: client.Id.ToString(),
            submissionId: submission.Id,
            details: $"Type={req.TaxType}, Year={req.TaxYear}, Liability={liability:F2}");

        await _audit.LogAsync(AuditEventType.Calculation, "TaxLiabilityCalculated",
            performedBy: "System",
            submissionId: submission.Id,
            details: $"GrossIncome={req.GrossIncome}, Deductions={req.Deductions}, " +
                     $"TaxableIncome={submission.TaxableIncome}, Liability={liability}, Rate={effectiveRate:P2}");

        // Reload with client nav prop
        await _db.Entry(submission).Reference(s => s.Client).LoadAsync();
        return CreatedAtAction(nameof(GetById), new { id = submission.Id }, ToResponse(submission));
    }

    /// <summary>
    /// Transitions a submission through its workflow.
    /// Allowed: Submitted→UnderReview, UnderReview→Approved/Rejected, Approved→Filed
    /// </summary>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType<TaxSubmissionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateSubmissionStatusRequest req)
    {
        var submission = await _db.TaxSubmissions.Include(s => s.Client).FirstOrDefaultAsync(s => s.Id == id);
        if (submission is null)
            return NotFound();

        if (!SubmissionWorkflow.CanTransition(submission.Status, req.NewStatus))
        {
            var allowed = SubmissionWorkflow.AllowedNext(submission.Status);
            await _audit.LogAsync(AuditEventType.ValidationFailure, "InvalidStatusTransition",
                performedBy: req.PerformedBy,
                submissionId: id,
                details: $"Attempted {submission.Status} → {req.NewStatus}. Allowed: [{string.Join(", ", allowed)}]");

            return BadRequest(new
            {
                error   = $"Cannot transition from {submission.Status} to {req.NewStatus}.",
                allowed = allowed
            });
        }

        var oldStatus     = submission.Status;
        submission.Status = req.NewStatus;

        if (req.NewStatus is SubmissionStatus.Approved or SubmissionStatus.Rejected)
        {
            submission.ReviewedAt    = DateTime.UtcNow;
            submission.ReviewerNotes = req.ReviewerNotes;
        }

        if (req.NewStatus == SubmissionStatus.Filed)
            submission.ResolvedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _audit.LogAsync(AuditEventType.StatusChange, "StatusTransitioned",
            performedBy: req.PerformedBy,
            submissionId: id,
            details: $"{oldStatus} → {req.NewStatus}. Notes: {req.ReviewerNotes ?? "none"}");

        return Ok(ToResponse(submission));
    }

    /// <summary>Returns all audit log entries for a specific submission.</summary>
    [HttpGet("{id:int}/audit")]
    [ProducesResponseType<IEnumerable<AuditLogResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAuditLogs(int id)
    {
        if (!await _db.TaxSubmissions.AnyAsync(s => s.Id == id))
            return NotFound();

        var logs = await _db.AuditLogs
            .Where(a => a.TaxSubmissionId == id)
            .OrderByDescending(a => a.Timestamp)
            .Select(a => new AuditLogResponse(a.Id, a.TaxSubmissionId, a.Action,
                                              a.PerformedBy, a.Details, a.EventType, a.Timestamp))
            .ToListAsync();

        return Ok(logs);
    }

    private static TaxSubmissionResponse ToResponse(TaxSubmission s) =>
        new(s.Id, s.ClientId, s.Client?.FullName ?? string.Empty,
            s.TaxType, s.TaxYear, s.GrossIncome, s.Deductions,
            s.TaxableIncome, s.TaxLiability, s.EffectiveRate,
            s.VatableSales, s.VatRate, s.Status,
            s.SubmittedAt, s.ReviewedAt, s.ResolvedAt, s.ReviewerNotes);
}
