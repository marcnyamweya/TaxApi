using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaxApi.Data;
using TaxApi.DTOs;
using TaxApi.Models;
using TaxApi.Services;

namespace TaxApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ClientsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public ClientsController(AppDbContext db, IAuditService audit)
    {
        _db    = db;
        _audit = audit;
    }

    /// <summary>Returns all registered clients.</summary>
    [HttpGet]
    [ProducesResponseType<IEnumerable<ClientResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var clients = await _db.Clients
            .Select(c => ToResponse(c))
            .ToListAsync();
        return Ok(clients);
    }

    /// <summary>Returns a single client by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ClientResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var client = await _db.Clients.FindAsync(id);
        return client is null ? NotFound() : Ok(ToResponse(client));
    }

    /// <summary>Registers a new client.</summary>
    [HttpPost]
    [ProducesResponseType<ClientResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateClientRequest req)
    {
        if (await _db.Clients.AnyAsync(c => c.TaxIdentificationNumber == req.TaxIdentificationNumber))
            return Conflict(new { error = "A client with this Tax Identification Number already exists." });

        if (await _db.Clients.AnyAsync(c => c.Email == req.Email))
            return Conflict(new { error = "A client with this email already exists." });

        var client = new Client
        {
            FullName                = req.FullName,
            Email                   = req.Email,
            TaxIdentificationNumber = req.TaxIdentificationNumber,
            ClientType              = req.ClientType
        };

        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(AuditEventType.Submission, "ClientCreated",
            performedBy: client.Id.ToString(),
            details: $"New {client.ClientType} client registered: {client.Email}");

        return CreatedAtAction(nameof(GetById), new { id = client.Id }, ToResponse(client));
    }

    private static ClientResponse ToResponse(Client c) =>
        new(c.Id, c.FullName, c.Email, c.TaxIdentificationNumber, c.ClientType, c.CreatedAt);
}
