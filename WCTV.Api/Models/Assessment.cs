namespace WCTV.Api.Models;

public class Assessment
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public float BeforeScore { get; set; }
    public float AfterScore { get; set; }
    public float Confidence { get; set; }
    public string Result { get; set; } = "ok"; // ok/let_forvaerring/forvaerring/ugyldig/kraever_gennemgang
    public string? ChangeMetadata { get; set; } // JSON
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;

    public Session Session { get; set; } = null!;
}
