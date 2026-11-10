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
        var now = DateTime.UtcNow;
        var rows = await _db.Classes.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.TotalSeats,
                Held = c.Holds.Count(h => h.Status == HoldStatus.Held && h.ExpiresAt > now),
                Confirmed = c.Holds.Count(h => h.Status == HoldStatus.Confirmed)
            })
            .ToListAsync(ct);

        return rows.Select(r => ToDto(r.Id, r.Name, r.TotalSeats, r.Held, r.Confirmed)).ToList();
    }


    public async Task<ClassDto?> GetClassAsync(Guid id, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var r = await _db.Classes.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.TotalSeats,
                Held = c.Holds.Count(h => h.Status == HoldStatus.Held && h.ExpiresAt > now),
                Confirmed = c.Holds.Count(h => h.Status == HoldStatus.Confirmed)
            })
            .FirstOrDefaultAsync(ct);

        return r is null ? null : ToDto(r.Id, r.Name, r.TotalSeats, r.Held, r.Confirmed);
    }


    private static ClassDto ToDto(Guid id, string name, int total, int held, int confirmed) =>
        new(id, name, total, held, confirmed, Math.Max(0, total - held - confirmed));




    // ---------- Holds ----------

    /// <summary>
    /// Claims a seat. Correctness under concurrency comes from taking a row lock on the class
    /// (SELECT ... FOR UPDATE) inside a transaction: every claim for one class is serialized,
    /// so the occupancy count we read cannot change before we insert.
    /// </summary>



  public async Task<Result<HoldDto>> CreateHoldAsync(Guid classId, string studentId, CancellationToken ct)
    {
        var cls = await _db.Classes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == classId, ct);
        if (cls is null) return Result<HoldDto>.Fail(Failure.NotFound);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        await _db.Database.ExecuteSqlAsync(
            $"SELECT 1 FROM classes WHERE \"Id\" = {classId} FOR UPDATE", ct);

        var now = DateTime.UtcNow;

        // Release seats whose deadline passed so they are reusable right now.
        await _db.Holds
            .Where(h => h.ClassId == classId && h.Status == HoldStatus.Held && h.ExpiresAt <= now)
            .ExecuteUpdateAsync(s => s.SetProperty(h => h.Status, HoldStatus.Expired), ct);

        var alreadyApplied = await _db.Holds.AnyAsync(h =>
            h.ClassId == classId && h.StudentId == studentId &&
            (h.Status == HoldStatus.Held || h.Status == HoldStatus.Confirmed), ct);
        if (alreadyApplied)
        {
            await tx.RollbackAsync(ct);
            return Result<HoldDto>.Fail(Failure.AlreadyApplied);
        }

        var occupied = await _db.Holds.CountAsync(h =>
            h.ClassId == classId &&
            (h.Status == HoldStatus.Held || h.Status == HoldStatus.Confirmed), ct);
        if (occupied >= cls.TotalSeats)
        {
            await tx.RollbackAsync(ct);
            return Result<HoldDto>.Fail(Failure.NoSeats);
        }

        var hold = new Hold
        {
            Id = Guid.NewGuid(),
            ClassId = classId,
            StudentId = studentId,
            Status = HoldStatus.Held,
            CreatedAt = now,
            ExpiresAt = now.AddSeconds(_options.HoldDurationSeconds)
        };
        _db.Holds.Add(hold);

        try
        {
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return Result<HoldDto>.Fail(Failure.AlreadyApplied);
        }

        return Result<HoldDto>.Success(HoldDto.From(hold, cls.Name, now));
    }



    public async Task<Result<HoldDto>> GetHoldAsync(Guid holdId, string? studentId, CancellationToken ct)
    {
        var hold = await _db.Holds.AsNoTracking().Include(h => h.Class)
            .FirstOrDefaultAsync(h => h.Id == holdId, ct);
        if (hold is null) return Result<HoldDto>.Fail(Failure.NotFound);
        if (studentId is not null && hold.StudentId != studentId) return Result<HoldDto>.Fail(Failure.Forbidden);

        return Result<HoldDto>.Success(HoldDto.From(hold, hold.Class.Name, DateTime.UtcNow));
    }






    /// <summary>
    /// Simulated payment. The HELD -> CONFIRMED transition is one conditional UPDATE, so it is
    /// atomic against the expiry sweeper, cancellation, and duplicate confirm calls.
    /// Confirmed seats were already counted as held, so totals never change here.
    /// </summary>
 



 public async Task<Result<HoldDto>> ConfirmAsync(Guid holdId, string? studentId, CancellationToken ct)
    {
        var check = await LoadOwned(holdId, studentId, ct);
        if (!check.Ok) return Result<HoldDto>.Fail(check.Failure);

        var now = DateTime.UtcNow;
        var rows = await _db.Holds
            .Where(h => h.Id == holdId && h.Status == HoldStatus.Held && h.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(h => h.Status, HoldStatus.Confirmed)
                .SetProperty(h => h.ConfirmedAt, (DateTime?)now), ct);

        var current = await ReloadAsync(holdId, ct);
        if (rows == 1 || current.Status == HoldStatus.Confirmed)   // second case: idempotent retry
            return Result<HoldDto>.Success(HoldDto.From(current, current.Class.Name, now));

        if (current.Status == HoldStatus.Expired || (current.Status == HoldStatus.Held && current.ExpiresAt <= now))
            return Result<HoldDto>.Fail(Failure.Expired);

        return Result<HoldDto>.Fail(Failure.InvalidState);
    }





    public async Task<Result<HoldDto>> CancelAsync(Guid holdId, string? studentId, CancellationToken ct)
    {
        var check = await LoadOwned(holdId, studentId, ct);
        if (!check.Ok) return Result<HoldDto>.Fail(check.Failure);

        var now = DateTime.UtcNow;
        var rows = await _db.Holds
            .Where(h => h.Id == holdId && h.Status == HoldStatus.Held && h.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(h => h.Status, HoldStatus.Cancelled)
                .SetProperty(h => h.CancelledAt, (DateTime?)now), ct);

        var current = await ReloadAsync(holdId, ct);
        if (rows == 1 || current.Status == HoldStatus.Cancelled)   // second case: idempotent retry
            return Result<HoldDto>.Success(HoldDto.From(current, current.Class.Name, now));

        if (current.Status == HoldStatus.Expired || (current.Status == HoldStatus.Held && current.ExpiresAt <= now))
            return Result<HoldDto>.Fail(Failure.Expired);

        return Result<HoldDto>.Fail(Failure.InvalidState); // CONFIRMED applications cannot be cancelled
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
        var owner = await _db.Holds.AsNoTracking()
            .Where(h => h.Id == holdId).Select(h => h.StudentId).FirstOrDefaultAsync(ct);
        
        
        if (owner is null) return Result<bool>.Fail(Failure.NotFound);
        
        if (studentId is not null && owner != studentId) return Result<bool>.Fail(Failure.Forbidden);
        
        return Result<bool>.Success(true);

    }


    private Task<Hold> ReloadAsync(Guid holdId, CancellationToken ct) =>
        _db.Holds.AsNoTracking().Include(h => h.Class).FirstAsync(h => h.Id == holdId, ct);

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}