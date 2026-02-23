namespace WCTV.Api.Models;

public class Session
{
    public int Id { get; set; }
    public int ToiletId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public string? ExitEvent { get; set; } // kortscan/timeout/doersensor
    public string Status { get; set; } = "active"; // active/completed

    public Toilet Toilet { get; set; } = null!;
    public ICollection<Snapshot> Snapshots { get; set; } = new List<Snapshot>();
    public Assessment? Assessment { get; set; }
}
