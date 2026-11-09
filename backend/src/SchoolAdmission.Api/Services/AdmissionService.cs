using Microsoft.EntityFrameworkCore;
using Npgsql;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.Services;

public enum Failure
{
    None,
    NotFound,
    Forbidden,
    NoSeats,
    AlreadyApplied,
    Expired,
    InvalidState,
    DuplicateName
}

public sealed record Result<T>(T? Value, Failure Failure)
{
    public bool Ok => Failure == Failure.None;
    public static Result<T> Success(T value) => new(value, Failure.None);
    public static Result<T> Fail(Failure f) => new(default, f);
}



public class AdmissionService
{
    private readonly AppDbContext _db;
    private readonly AdmissionOptions _options;

    public AdmissionService(AppDbContext db, AdmissionOptions options)
    {
        _db = db;
        _options = options;
    }




    // ---------- Classes ----------

    public async Task<Result<ClassDto>> CreateClassAsync(string name, int totalSeats, CancellationToken ct)
    {
        var entity = new AdmissionClass
        {
            Id = Guid.NewGuid(),
            Name = name,
            TotalSeats = totalSeats,
            CreatedAt = DateTime.UtcNow
        };
        _db.Classes.Add(entity);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return Result<ClassDto>.Fail(Failure.DuplicateName);
        }
        return Result<ClassDto>.Success(new ClassDto(entity.Id, entity.Name, entity.TotalSeats, 0, 0, entity.TotalSeats));
    }




    public async Task<List<ClassDto>> ListClassesAsync(CancellationToken ct)
    {
        
    }



    public async Task<ClassDto?> GetClassAsync(Guid id, CancellationToken ct)
    {
       
    }




    private static ClassDto ToDto(Guid id, string name, int total, int held, int confirmed) =>);

    // ---------- Holds ----------

    /// <summary>
    /// Claims a seat. Correctness under concurrency comes from taking a row lock on the class
    /// (SELECT ... FOR UPDATE) inside a transaction: every claim for one class is serialized,
    /// so the occupancy count we read cannot change before we insert.
    /// </summary>
    public async Task<Result<HoldDto>> CreateHoldAsync(Guid classId, string studentId, CancellationToken ct)
    {
       
    }

    public async Task<Result<HoldDto>> GetHoldAsync(Guid holdId, string? studentId, CancellationToken ct)
    {
        
    }

    /// <summary>
    /// Simulated payment. The HELD -> CONFIRMED transition is one conditional UPDATE, so it is
    /// atomic against the expiry sweeper, cancellation, and duplicate confirm calls.
    /// Confirmed seats were already counted as held, so totals never change here.
    /// </summary>
    public async Task<Result<HoldDto>> ConfirmAsync(Guid holdId, string? studentId, CancellationToken ct)
    {
       
    }

    public async Task<Result<HoldDto>> CancelAsync(Guid holdId, string? studentId, CancellationToken ct)
    {
       
    }

    public async Task<List<HoldDto>> ListForStudentAsync(string studentId, CancellationToken ct)
    {
        
    }

    /// <summary>Marks overdue holds as EXPIRED. Called by the background sweeper.</summary>
    public Task<int> ExpireOverdueAsync(CancellationToken ct)
    {
        
    }

    // ---------- helpers ----------

    private async Task<Result<bool>> LoadOwned(Guid holdId, string? studentId, CancellationToken ct)
    {
        
    }



    private Task<Hold> ReloadAsync(Guid holdId, CancellationToken ct) =>
        _db.Holds.AsNoTracking().Include(h => h.Class).FirstAsync(h => h.Id == holdId, ct);

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}