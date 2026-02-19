using TaxApi.Models;

namespace TaxApi.DTOs;

public record CreateTaxSubmissionRequest(
    int ClientId,
    TaxType TaxType,
    int TaxYear,
    decimal GrossIncome,
    decimal Deductions,
    decimal? VatableSales,
    decimal? VatRate);

public record UpdateSubmissionStatusRequest(
    SubmissionStatus NewStatus,
    string PerformedBy,
    string? ReviewerNotes);

public record TaxSubmissionResponse(
    int Id,
    int ClientId,
    string ClientName,
    TaxType TaxType,
    int TaxYear,
    decimal GrossIncome,
    decimal Deductions,
    decimal TaxableIncome,
    decimal TaxLiability,
    decimal EffectiveRate,
    decimal? VatableSales,
    decimal? VatRate,
    SubmissionStatus Status,
    DateTime SubmittedAt,
    DateTime? ReviewedAt,
    DateTime? ResolvedAt,
    string? ReviewerNotes);
