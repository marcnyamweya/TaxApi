# Tax Workflow Processing API

## Overview

A backend system simulating an enterprise-grade tax processing workflow. Supports client management, automated tax calculations using real-world brackets (IRS 2024), a state-machine driven workflow, and full audit logging — all exposed via documented REST endpoints.

---

## Technology Stack

| Layer | Technology |
|---|---|
| Language | C# 13 |
| Framework | .NET 10 Web API |
| ORM | Entity Framework Core 9 |
| Database | SQL Server (Docker) |
| Documentation | Swagger / OpenAPI |
| Testing | Postman |

---

## Architecture

```
TaxApi/
├── Controllers/          # REST endpoints
│   ├── ClientsController.cs
│   ├── TaxSubmissionsController.cs
│   └── AuditLogsController.cs
├── Data/
│   └── AppDbContext.cs   # EF Core DbContext + Fluent API config
├── DTOs/                 # Request/Response contracts (no model leakage)
├── Middleware/
│   └── GlobalExceptionMiddleware.cs  # Catches & audit-logs all unhandled errors
├── Models/               # Domain entities
│   ├── Client.cs
│   ├── TaxSubmission.cs
│   └── AuditLog.cs
├── Services/
│   ├── TaxCalculationService.cs  # IRS bracket calculations
│   ├── AuditService.cs           # Writes structured audit events
│   └── SubmissionWorkflow.cs     # Status transition state machine
├── Validation/
│   └── SubmissionValidator.cs    # Business rule validation
└── Program.cs            # DI wiring, middleware pipeline, Swagger
```

---

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

### 1. Start SQL Server via Docker

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong@Password" \
   -p 1433:1433 --name sql_server_container \
   -d mcr.microsoft.com/mssql/server:2022-latest
```

> **Apple Silicon (M1/M2/M3):** Docker Desktop handles the x86_64 image via Rosetta 2 automatically.

### 2. Configure Connection String

Create `appsettings.Development.json` (excluded from git):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=TaxDb;User Id=sa;Password=YourStrong@Password;TrustServerCertificate=True"
  }
}
```

### 3. Apply Migrations

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

> Migrations are also auto-applied on startup in development mode.

### 4. Run the API

```bash
dotnet run
```

Swagger UI is available at `http://localhost:5166` (root).

---

## Domain Models

### Client
| Field | Type | Notes |
|---|---|---|
| Id | int | PK |
| FullName | string | Required |
| Email | string | Unique |
| TaxIdentificationNumber | string | Unique |
| ClientType | enum | `Individual`, `Corporate` |

### TaxSubmission
| Field | Type | Notes |
|---|---|---|
| TaxType | enum | `PersonalIncome`, `Corporate`, `VAT` |
| TaxYear | int | 2000–current year |
| GrossIncome | decimal | |
| Deductions | decimal | Must not exceed gross |
| TaxableIncome | decimal | Computed: Gross − Deductions |
| TaxLiability | decimal | Calculated on submission |
| EffectiveRate | decimal | Liability ÷ Taxable Income |
| Status | enum | Workflow state (see below) |

### AuditLog
Captures every significant event with a timestamp, actor, and structured details.

| EventType | When triggered |
|---|---|
| `Submission` | Client registered, submission created |
| `StatusChange` | Workflow transition applied |
| `ValidationFailure` | Invalid request data or illegal transition |
| `SystemError` | Unhandled exception caught by middleware |
| `Calculation` | Tax liability computed |

---

## Tax Calculation Logic

### Personal Income Tax (IRS 2024 Federal Brackets — Single Filer)

| Taxable Income | Rate |
|---|---|
| Up to $11,600 | 10% |
| $11,601 – $47,150 | 12% |
| $47,151 – $100,525 | 22% |
| $100,526 – $191,950 | 24% |
| $191,951 – $243,725 | 32% |
| $243,726 – $609,350 | 35% |
| Over $609,350 | 37% |

Uses **progressive marginal rate** calculation — each bracket only applies to income within that range.

### Corporate Tax
Flat **21%** on taxable income (post-TCJA 2017 US federal rate). Corporate deductions capped at 90% of gross income by business rule.

### VAT
Calculated as `VatableSales × (VatRate / 100)`. Defaults to 20% if no rate provided.

---

## Workflow State Machine

```
Submitted ──► Under Review ──► Approved ──► Filed
                   │
                   └──► Rejected
```

Invalid transitions return `400 Bad Request` with the list of valid next states and are recorded in the audit log.

---

## API Endpoints

### Clients — `POST/GET /api/clients`

| Method | Path | Description |
|---|---|---|
| GET | `/api/clients` | List all clients |
| GET | `/api/clients/{id}` | Get client by ID |
| POST | `/api/clients` | Register a new client |

### Tax Submissions — `/api/taxsubmissions`

| Method | Path | Description |
|---|---|---|
| GET | `/api/taxsubmissions` | List all (filter: `?clientId=&status=`) |
| GET | `/api/taxsubmissions/{id}` | Get submission detail |
| POST | `/api/taxsubmissions` | Submit tax data (validates + calculates) |
| PATCH | `/api/taxsubmissions/{id}/status` | Advance workflow status |
| GET | `/api/taxsubmissions/{id}/audit` | Full audit trail for a submission |

### Audit Logs — `/api/auditlogs`

| Method | Path | Description |
|---|---|---|
| GET | `/api/auditlogs` | Paginated logs (filter: `?eventType=`) |

---

## Validation Rules

- `GrossIncome` must be non-negative
- `Deductions` must be non-negative and ≤ `GrossIncome`
- `TaxYear` must be between 2000 and the current year
- Corporate deductions cannot exceed 90% of gross income
- VAT rate (if provided) must be between 0 and 100
- TIN and email must be unique per client

---

## Testing with Postman

Import `TaxApi.postman_collection.json` into Postman. The collection includes pre-built requests for the full workflow:

1. Create Individual and Corporate clients
2. Submit Personal Income, Corporate, and VAT returns
3. Walk a submission through every status transition
4. Inspect the audit trail at each step
5. Query audit logs by event type

---

## Environment Notes

- `appsettings.Development.json` and `Properties/launchSettings.json` are excluded from git via `.gitignore`
- Swagger is only enabled in the Development environment
- EF migrations are applied automatically on startup in Development
