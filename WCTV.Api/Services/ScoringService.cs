namespace WCTV.Api.Services;

public class ScoringResult
{
    public float BeforeScore { get; set; }
    public float AfterScore { get; set; }
    public float Confidence { get; set; }
    public float Delta { get; set; }
    public string Result { get; set; } = "ok";
    public string? TriggerSeverity { get; set; }
}

public class ScoringService
{
    private readonly Random _rng = new();

    public ScoringResult GenerateOutcome(float currentScore)
    {
        float confidence = 0.60f + (float)_rng.NextDouble() * 0.40f;
        float beforeScore = currentScore;

        // Weighted random outcome
        double roll = _rng.NextDouble();
        float delta;

        if (roll < 0.03) // 3% ugyldig
        {
            confidence = 0.20f + (float)_rng.NextDouble() * 0.39f; // < 0.60
            delta = (float)(_rng.NextDouble() * 0.1 - 0.05);
        }
        else if (roll < 0.10) // 7% forvaerring
        {
            delta = -(0.26f + (float)_rng.NextDouble() * 0.24f);
        }
        else if (roll < 0.22) // 12% let_forvaerring
        {
            delta = -(0.10f + (float)_rng.NextDouble() * 0.15f);
        }
        else // 78% ok
        {
            delta = (float)(_rng.NextDouble() * 0.10 - 0.02);
        }

        float afterScore = Math.Clamp(beforeScore + delta, 0.0f, 1.0f);
        float actualDelta = afterScore - beforeScore;

        string result;
        string? triggerSeverity = null;

        if (confidence < 0.60f)
        {
            result = "kraever_gennemgang";
        }
        else if (actualDelta > -0.10f)
        {
            result = "ok";
        }
        else if (actualDelta >= -0.25f)
        {
            result = "let_forvaerring";
            triggerSeverity = "let";
        }
        else
        {
            result = "forvaerring";
            triggerSeverity = "forvaerring";
        }

        return new ScoringResult
        {
            BeforeScore = beforeScore,
            AfterScore = afterScore,
            Confidence = confidence,
            Delta = actualDelta,
            Result = result,
            TriggerSeverity = triggerSeverity
        };
    }

    public string ScoreToStatus(float score, string assessmentResult)
    {
        if (assessmentResult == "kraever_gennemgang") return "ugyldig";
        if (assessmentResult == "forvaerring") return "forvaerring";
        if (assessmentResult == "let_forvaerring") return "let_forvaerring";
        return "ok";
    }
}
