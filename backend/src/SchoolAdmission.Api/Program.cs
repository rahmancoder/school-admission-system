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



using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    const int attempts = 30;
    for (var i = 1; i <= attempts; i++)
    {
        try
        {
            await db.Database.EnsureCreatedAsync();
            break;
        }
        catch (Exception ex) when (i < attempts)
        {
            app.Logger.LogWarning("Database not ready (attempt {Attempt}/{Max}): {Message}", i, attempts, ex.Message);
            await Task.Delay(1000);
        }
    }
}





app.UseCors();

// ---------- helpers ----------


static IResult Error(int status, string code, string message) =>
    Results.Json(new ApiError(code, message), statusCode: status);

static IResult FromFailure(Failure f) => f switch
{
    Failure.NotFound => Error(404, "NOT_FOUND", "Resource not found."),
    Failure.Forbidden => Error(403, "FORBIDDEN", "This application belongs to a different student."),
    Failure.NoSeats => Error(409, "NO_SEATS_AVAILABLE", "No seats are available in this class."),
    Failure.AlreadyApplied => Error(409, "ALREADY_APPLIED", "You already have an active application for this class."),
    Failure.Expired => Error(409, "HOLD_EXPIRED", "This application has expired."),
    Failure.DuplicateName => Error(409, "DUPLICATE_CLASS", "A class with this name already exists."),
    _ => Error(409, "INVALID_STATE", "This application cannot be changed in its current state.")
};

// Returns null when the header is valid; otherwise the error response to send.
static IResult? RequireStudent(HttpRequest req, out string studentId)
{
    studentId = req.Headers["X-Student-Id"].ToString().Trim();
    if (studentId.Length == 0)
        return Error(400, "MISSING_STUDENT_ID", "The X-Student-Id header is required.");
    return StudentIdRules.IsValid(studentId)
        ? null
        : Error(400, "INVALID_STUDENT_ID", "X-Student-Id must be a 17-digit birth registration number.");
}

// For endpoints where the header is optional: when present it must be valid and match the owner.
static IResult? OptionalStudent(HttpRequest req, out string? studentId)
{
    var raw = req.Headers["X-Student-Id"].ToString().Trim();
    if (raw.Length == 0) { studentId = null; return null; }
    studentId = raw;
    return StudentIdRules.IsValid(raw)
        ? null
        : Error(400, "INVALID_STUDENT_ID", "X-Student-Id must be a 17-digit birth registration number.");
}








// ---------- classes ----------

app.MapPost("/classes", async (CreateClassRequest body, AdmissionService svc, CancellationToken ct) =>
{

    var name = body.Name?.Trim();
    if (string.IsNullOrEmpty(name) || name.Length > 200)
        return Error(400, "INVALID_NAME", "name is required (max 200 characters).");
    if (body.TotalSeats is null or <= 0 or > 1_000_000)
        return Error(400, "INVALID_SEATS", "totalSeats must be a positive integer.");

    var result = await svc.CreateClassAsync(name, body.TotalSeats.Value, ct);
    return result.Ok
        ? Results.Created($"/classes/{result.Value!.Id}", result.Value)
        : FromFailure(result.Failure);
    
   
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
