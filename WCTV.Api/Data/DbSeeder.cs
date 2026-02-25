using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WCTV.Api.Models;

namespace WCTV.Api.Data;

public static class DbSeeder
{
    public static void Seed(WctvDbContext context)
    {
        // ── Toilets ───────────────────────────────────────────────────────
        List<Toilet> toilets;

        if (!context.Toilets.Any())
        {
            toilets = Enumerable.Range(1, 10).Select(i => new Toilet
            {
                StallNumber = i,
                Name        = $"Bås {i}",
                Location    = i <= 5 ? "Bygning A, 1. sal" : "Bygning A, 2. sal",
                CreatedAt   = DateTime.UtcNow.AddDays(-7)
            }).ToList();

            context.Toilets.AddRange(toilets);
            context.SaveChanges();

            context.ToiletStatuses.AddRange(toilets.Select(t => new ToiletStatus
            {
                ToiletId     = t.Id,
                CurrentScore = 0.92f,
                Status       = "ok",
                LastUpdated  = DateTime.UtcNow.AddDays(-7)
            }));
            context.SaveChanges();
        }
        else
        {
            toilets = context.Toilets.Include(t => t.Status).ToList();
        }

        if (context.Sessions.Any()) return;

        SeedHistory(context, toilets);
    }

    // ─────────────────────────────────────────────────────────────────────
    private static void SeedHistory(WctvDbContext context, List<Toilet> toilets)
    {
        var rng = new Random(1337);
        var now = DateTime.UtcNow;

        // Sessions are generated per stall so the same stall never overlaps.
        // Each stall's score evolves in isolation through its own session chain.
        var sessions  = new List<Session>();
        var scores    = new Dictionary<int, float>();   // current score per toilet

        // Give each stall a realistic starting score (not all 100 %)
        foreach (var t in toilets)
            scores[t.Id] = 0.88f + (float)(rng.NextDouble() * 0.10);   // 88–98 %

        foreach (var toilet in toilets)
        {
            // Ground-floor stalls (1–5) are busier than upper floor (6–10)
            bool busy   = toilet.StallNumber <= 5;
            int minGap  = busy ? 12 : 22;   // minutes between visits
            int maxGap  = busy ? 28 : 48;

            for (int dayOffset = -6; dayOffset <= 0; dayOffset++)
            {
                var day    = now.Date.AddDays(dayOffset);
                var cursor = day.AddHours(7).AddMinutes(rng.Next(0, minGap));
                var dayEnd = dayOffset == 0 ? now.AddMinutes(-5) : day.AddHours(17);

                while (cursor < dayEnd)
                {
                    int dur = rng.Next(3, 9);           // visit: 3–8 min
                    var end = cursor.AddMinutes(dur);
                    if (end > dayEnd) break;

                    sessions.Add(new Session
                    {
                        ToiletId  = toilet.Id,
                        StartedAt = cursor,
                        EndedAt   = end,
                        Status    = "completed",
                        ExitEvent = new[] { "kortscan", "timeout", "doersensor" }[rng.Next(3)]
                    });

                    cursor = end.AddMinutes(rng.Next(minGap, maxGap + 1));
                }
            }
        }

        context.Sessions.AddRange(sessions);
        context.SaveChanges();   // sessions now have IDs

        // ── Pass 2: assessments, snapshots, triggers ──────────────────────
        // Process sessions per-stall in chronological order so each stall's
        // score chain is self-consistent (no cross-stall contamination).
        var triggers = new List<CleaningTrigger>();

        var byStall = sessions
            .GroupBy(s => s.ToiletId)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.StartedAt).ToList());

        foreach (var toilet in toilets)
        {
            if (!byStall.ContainsKey(toilet.Id)) continue;

            foreach (var session in byStall[toilet.Id])
            {
                float before  = scores[toilet.Id];
                var   outcome = GenerateOutcome(rng, before);
                var   ended   = session.EndedAt!.Value;

                context.Snapshots.Add(new Snapshot
                {
                    SessionId  = session.Id,
                    Type       = "before",
                    Score      = before,
                    Confidence = 0.90f + (float)rng.NextDouble() * 0.10f,
                    TakenAt    = session.StartedAt
                });
                context.Snapshots.Add(new Snapshot
                {
                    SessionId  = session.Id,
                    Type       = "after",
                    Score      = outcome.After,
                    Confidence = outcome.Confidence,
                    TakenAt    = ended
                });

                context.Assessments.Add(new Assessment
                {
                    SessionId      = session.Id,
                    BeforeScore    = before,
                    AfterScore     = outcome.After,
                    Confidence     = outcome.Confidence,
                    Result         = outcome.Result,
                    ChangeMetadata = JsonSerializer.Serialize(new { delta = outcome.Delta }),
                    AssessedAt     = ended
                });

                context.EventLogs.Add(new EventLog
                {
                    EventType = "score",
                    ToiletId  = toilet.Id,
                    SessionId = session.Id,
                    Payload   = JsonSerializer.Serialize(new { outcome.Result, outcome.After, outcome.Confidence }),
                    LoggedAt  = ended
                });

                scores[toilet.Id] = outcome.After;

                if (outcome.TriggerSeverity is null) continue;

                // Decide trigger resolution based on timing
                int  responseMin  = 7 + rng.Next(-3, 8);    // 4–14 min
                int  cleanMin     = rng.Next(1, 5);
                var  ackAt        = ended.AddMinutes(responseMin);
                var  resolvedAt   = ackAt.AddMinutes(cleanMin);
                bool isToday      = ended.Date == now.Date;

                string triggerStatus;
                DateTime? ackStamp;

                if (!isToday || resolvedAt < now.AddMinutes(-1))
                {
                    // Past or fully elapsed today → resolved
                    bool fp       = rng.NextDouble() < 0.15;
                    triggerStatus = fp ? "false_positive" : "completed";
                    ackStamp      = ackAt;
                    if (triggerStatus == "completed")
                        scores[toilet.Id] = 0.92f;
                }
                else if (ackAt < now)
                {
                    // Acknowledge window has passed but cleaning not done yet
                    triggerStatus = "acknowledged";
                    ackStamp      = ackAt;
                }
                else
                {
                    // Still waiting for acknowledgement
                    triggerStatus = "active";
                    ackStamp      = null;
                }

                triggers.Add(new CleaningTrigger
                {
                    ToiletId       = toilet.Id,
                    SessionId      = session.Id,
                    Severity       = outcome.TriggerSeverity,
                    Status         = triggerStatus,
                    Confidence     = outcome.Confidence,
                    ChangeMetadata = JsonSerializer.Serialize(new { delta = outcome.Delta, outcome.Result }),
                    CreatedAt      = ended,
                    AcknowledgedAt = ackStamp
                });
            }
        }

        context.SaveChanges();   // snapshots + assessments + event logs

        context.CleaningTriggers.AddRange(triggers);
        context.SaveChanges();   // triggers now have IDs

        // Receipts for completed triggers
        context.CleaningReceipts.AddRange(
            triggers
                .Where(t => t.Status == "completed")
                .Select(t => new CleaningReceipt
                {
                    TriggerId   = t.Id,
                    CompletedAt = t.AcknowledgedAt!.Value.AddMinutes(rng.Next(1, 5))
                })
        );
        context.SaveChanges();

        // ── Pass 3: update toilet statuses to final seeded scores ─────────
        foreach (var toilet in toilets)
        {
            var status = context.ToiletStatuses.Find(toilet.Id);
            if (status is null) continue;

            float sc          = scores[toilet.Id];
            status.CurrentScore = sc;
            status.Status       = sc >= 0.80f ? "ok"
                                : sc >= 0.65f ? "let_forvaerring"
                                :               "forvaerring";
            status.LastUpdated  = now.AddMinutes(-rng.Next(1, 30));
        }
        context.SaveChanges();
    }

    // ── Outcome generator — mirrors ScoringService probabilities ─────────
    private record Outcome(float After, float Confidence, string Result, string? TriggerSeverity, float Delta);

    private static Outcome GenerateOutcome(Random rng, float current)
    {
        float confidence = 0.60f + (float)rng.NextDouble() * 0.40f;
        float delta;
        string result;
        string? triggerSeverity = null;

        double roll = rng.NextDouble();

        if (roll < 0.03)
        {
            confidence     = 0.20f + (float)rng.NextDouble() * 0.39f;
            delta          = (float)(rng.NextDouble() * 0.10 - 0.05);
            result         = "kraever_gennemgang";
        }
        else if (roll < 0.10)
        {
            delta          = -(0.26f + (float)rng.NextDouble() * 0.24f);
            result         = "forvaerring";
            triggerSeverity = "forvaerring";
        }
        else if (roll < 0.22)
        {
            delta          = -(0.10f + (float)rng.NextDouble() * 0.15f);
            result         = "let_forvaerring";
            triggerSeverity = "let";
        }
        else
        {
            delta  = (float)(rng.NextDouble() * 0.10 - 0.02);
            result = "ok";
        }

        float after      = Math.Clamp(current + delta, 0.0f, 1.0f);
        float actualDelta = after - current;

        return new Outcome(after, confidence, result, triggerSeverity, actualDelta);
    }
}
