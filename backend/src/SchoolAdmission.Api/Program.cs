using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Models;
using SchoolAdmission.Api.Services;


var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Host=localhost;Port=5432;Database=school_admission;Username=postgres;Password=postgres;Maximum Pool Size=50";


var holdSeconds = builder.Configuration.GetValue<int?>("HOLD_DURATION_SECONDS") ?? 600;

if (holdSeconds <= 0) throw new InvalidOperationException("HOLD_DURATION_SECONDS must be greater than zero.");


builder.Services.AddSingleton(new AdmissionOptions(holdSeconds));

builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));


builder.Services.AddScoped<AdmissionService>();
builder.Services.AddHostedService<HoldExpiryService>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));


var app = builder.Build();



app.UseCors();

// ---------- helpers ----------


// ---------- classes ----------

app.MapPost("/classes", async (CreateClassRequest body, AdmissionService svc, CancellationToken ct) =>
{

    return
   
});

app.MapGet("/classes", async (AdmissionService svc, CancellationToken ct) =>);

app.MapGet("/classes/{classId:guid}", async (Guid classId, AdmissionService svc, CancellationToken ct) =>
{
 
});

// ---------- holds ----------

app.MapPost("/classes/{classId:guid}/holds", async (Guid classId, HttpRequest req, AdmissionService svc, CancellationToken ct) =>
{

});

app.MapGet("/holds/{holdId:guid}", async (Guid holdId, HttpRequest req, AdmissionService svc, CancellationToken ct) =>
{

});

app.MapPost("/holds/{holdId:guid}/confirm", async (Guid holdId, HttpRequest req, AdmissionService svc, CancellationToken ct) =>
{

});

app.MapDelete("/holds/{holdId:guid}", async (Guid holdId, HttpRequest req, AdmissionService svc, CancellationToken ct) =>
{

});

app.MapGet("/me/holds", async (HttpRequest req, AdmissionService svc, CancellationToken ct) =>
{

});

app.Run();

// Exposed so the integration tests can use WebApplicationFactory<Program>.
public partial class Program { }
