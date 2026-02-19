using TaxApi.Models;

namespace TaxApi.DTOs;

public record CreateClientRequest(
    string FullName,
    string Email,
    string TaxIdentificationNumber,
    ClientType ClientType);

public record ClientResponse(
    int Id,
    string FullName,
    string Email,
    string TaxIdentificationNumber,
    ClientType ClientType,
    DateTime CreatedAt);
