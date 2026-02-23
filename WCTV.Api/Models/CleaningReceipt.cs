namespace WCTV.Api.Models;

public class CleaningReceipt
{
    public int Id { get; set; }
    public int TriggerId { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    public CleaningTrigger Trigger { get; set; } = null!;
}
