using Microsoft.EntityFrameworkCore;
using TaxApi.Models;

namespace TaxApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<TaxSubmission> TaxSubmissions => Set<TaxSubmission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Client
        modelBuilder.Entity<Client>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.FullName).IsRequired().HasMaxLength(200);
            e.Property(c => c.Email).IsRequired().HasMaxLength(200);
            e.Property(c => c.TaxIdentificationNumber).IsRequired().HasMaxLength(50);
            e.HasIndex(c => c.TaxIdentificationNumber).IsUnique();
            e.HasIndex(c => c.Email).IsUnique();
        });

        // TaxSubmission
        modelBuilder.Entity<TaxSubmission>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.GrossIncome).HasPrecision(18, 2);
            e.Property(s => s.Deductions).HasPrecision(18, 2);
            e.Property(s => s.TaxLiability).HasPrecision(18, 2);
            e.Property(s => s.EffectiveRate).HasPrecision(8, 4);
            e.Property(s => s.VatableSales).HasPrecision(18, 2);
            e.Property(s => s.VatRate).HasPrecision(5, 2);
            e.Ignore(s => s.TaxableIncome); // computed property
            e.HasOne(s => s.Client)
             .WithMany(c => c.TaxSubmissions)
             .HasForeignKey(s => s.ClientId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Action).IsRequired().HasMaxLength(100);
            e.Property(a => a.PerformedBy).HasMaxLength(100);
            e.HasOne(a => a.TaxSubmission)
             .WithMany(s => s.AuditLogs)
             .HasForeignKey(a => a.TaxSubmissionId)
             .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
