namespace WCTV.Api.Models;

public class CleaningTrigger
{
    public int Id { get; set; }
    public int ToiletId { get; set; }
    public int SessionId { get; set; }
    public string Severity { get; set; } = "let"; // let/forvaerring
    public string Status { get; set; } = "active"; // active/acknowledged/completed/false_positive
    public string? ChangeMetadata { get; set; } // JSON
    public float Confidence { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAt { get; set; }

    public Toilet Toilet { get; set; } = null!;
    public CleaningReceipt? Receipt { get; set; }
}
