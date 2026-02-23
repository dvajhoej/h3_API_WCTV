namespace WCTV.Api.Models;

public class EventLog
{
    public int Id { get; set; }
    public string EventType { get; set; } = ""; // scan/snapshot/score/trigger/receipt
    public int? ToiletId { get; set; }
    public int? SessionId { get; set; }
    public string? Payload { get; set; } // JSON
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
}
