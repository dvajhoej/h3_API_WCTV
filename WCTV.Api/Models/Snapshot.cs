namespace WCTV.Api.Models;

public class Snapshot
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string Type { get; set; } = "before"; // before/after
    public float Score { get; set; }
    public float Confidence { get; set; }
    public DateTime TakenAt { get; set; } = DateTime.UtcNow;

    public Session Session { get; set; } = null!;
}
