namespace WCTV.Api.Models;

public class ToiletStatus
{
    public int ToiletId { get; set; }
    public float CurrentScore { get; set; } = 1.0f;
    public string Status { get; set; } = "ok"; // ok/let_forvaerring/forvaerring/ugyldig/inactive
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public Toilet Toilet { get; set; } = null!;
}
