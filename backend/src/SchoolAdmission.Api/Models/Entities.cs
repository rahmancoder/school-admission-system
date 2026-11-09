namespace SchoolAdmission.Api.Models;

public enum HoldStatus
{
    Held,
    Confirmed,
    Expired,
    Cancelled
}

public class AdmissionClass
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<Hold> Holds { get; set; } = new();
}

public class Hold
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public AdmissionClass Class { get; set; } = null!;
    public string StudentId { get; set; } = string.Empty;
    public HoldStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}