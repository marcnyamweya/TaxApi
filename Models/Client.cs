namespace TaxApi.Models;

public class Client
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string TaxIdentificationNumber { get; set; } = null!;
    public ClientType ClientType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ICollection<TaxSubmission> TaxSubmissions { get; set; } = new List<TaxSubmission>();
}

public enum ClientType
{
    Individual = 1,
    Corporate = 2
}
