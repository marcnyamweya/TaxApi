using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using System.Reflection;
using TaxApi.Data;
using TaxApi.Middleware;
using TaxApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database ────────────────────────────────────────────────────────────────
// In Production (Azure), the connection string is injected via App Service
// Configuration > Connection strings (set by deploy.sh).
// In Development, it is read from appsettings.Development.json.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)
    ));

// ── Application Services ────────────────────────────────────────────────────
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddSingleton<ITaxCalculationService, TaxCalculationService>();

// ── Controllers ─────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// ── Swagger / OpenAPI ───────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Tax Workflow Processing API",
        Version     = "v1",
        Description = "Enterprise-grade tax processing backend: client management, " +
                      "tax calculation (personal income, corporate, VAT), " +
                      "workflow state machine, and full audit logging.",
        Contact     = new OpenApiContact { Name = "Tax API Team" }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});

// ── CORS ────────────────────────────────────────────────────────────────────
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// ── Middleware Pipeline ──────────────────────────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>();

// Swagger available in all environments for this project.
// Wrap in if (app.Environment.IsDevelopment()) to restrict in future.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tax API v1");
    c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();

// ── Auto-apply EF Migrations on startup ─────────────────────────────────────
// EF tracks applied migrations — safe to run on every startup.
using (var scope = app.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        db.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to apply database migrations on startup.");
        throw;
    }
}

app.Run();
