using System.Text.RegularExpressions;

namespace SchoolAdmission.Api.Models;

public sealed record AdmissionOptions(int HoldDurationSeconds);

public sealed record CreateClassRequest(string? Name, int? TotalSeats);

public sealed record ApiError(string Code, string Error);



public sealed record ClassDto(
    Guid Id,
    string Name,
    int TotalSeats,
    int HeldSeats,
    int ConfirmedSeats,
    int AvailableSeats);



public sealed record HoldDto(
    Guid Id,
    Guid ClassId,
    string ClassName,
    string StudentId,
    string Status,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? ConfirmedAt,
    int RemainingSeconds,
    DateTime ServerTime)
{
    /// <summary>
    /// Builds the API representation. A HELD row whose deadline has passed is reported as
    /// EXPIRED immediately, even if the background sweeper has not persisted that yet.
    /// </summary>
    public static HoldDto From(Hold h, string className, DateTime now)
    {
        var status = h.Status;
        if (status == HoldStatus.Held && h.ExpiresAt <= now) status = HoldStatus.Expired;

        var remaining = status == HoldStatus.Held
            ? Math.Max(0, (int)Math.Ceiling((h.ExpiresAt - now).TotalSeconds))
            : 0;

        return new HoldDto(
            h.Id, h.ClassId, className, h.StudentId,
            status.ToString().ToUpperInvariant(),
            h.CreatedAt, h.ExpiresAt, h.ConfirmedAt, remaining, now);
    }
}


public static class StudentIdRules
{
    private static readonly Regex Pattern = new(@"^\d{17}$", RegexOptions.Compiled);
    public static bool IsValid(string? id) => id is not null && Pattern.IsMatch(id);
}