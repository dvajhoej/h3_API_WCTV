using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WCTV.Api.Data;
using WCTV.Api.Hubs;
using WCTV.Api.Models;

namespace WCTV.Api.Services;

public class DataMotorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<DashboardHub> _hub;
    private readonly ILogger<DataMotorService> _logger;
    private readonly ScoringService _scoring;
    private readonly Random _rng = new();

    public DataMotorService(
        IServiceScopeFactory scopeFactory,
        IHubContext<DashboardHub> hub,
        ILogger<DataMotorService> logger,
        ScoringService scoring)
    {
        _scopeFactory = scopeFactory;
        _hub = hub;
        _logger = logger;
        _scoring = scoring;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DataMotor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            int delayMs = _rng.Next(8000, 18001);
            await Task.Delay(delayMs, stoppingToken);

            try
            {
                await SimulateVisitAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DataMotor cycle");
            }
        }
    }

    private async Task SimulateVisitAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WctvDbContext>();

        // Pick a random toilet with no active session
        var activeSessions = await db.Sessions
            .Where(s => s.Status == "active")
            .Select(s => s.ToiletId)
            .ToListAsync(ct);

        var available = await db.Toilets
            .Include(t => t.Status)
            .Where(t => !activeSessions.Contains(t.Id))
            .ToListAsync(ct);

        if (!available.Any()) return;

        var toilet = available[_rng.Next(available.Count)];
        float currentScore = toilet.Status?.CurrentScore ?? 1.0f;

        // Start session
        var session = new Session
        {
            ToiletId = toilet.Id,
            StartedAt = DateTime.UtcNow,
            Status = "active"
        };
        db.Sessions.Add(session);
        await db.SaveChangesAsync(ct);

        // Before snapshot
        var beforeSnap = new Snapshot
        {
            SessionId = session.Id,
            Type = "before",
            Score = currentScore,
            Confidence = 0.90f + (float)_rng.NextDouble() * 0.10f,
            TakenAt = DateTime.UtcNow
        };
        db.Snapshots.Add(beforeSnap);
        await db.SaveChangesAsync(ct);

        await _hub.Clients.All.SendAsync("ReceiveSessionStarted",
            new { toiletId = toilet.Id, sessionId = session.Id }, ct);

        _logger.LogInformation("Motor: Session {SessionId} started on Toilet {ToiletId}", session.Id, toilet.Id);

        // Simulate visit duration
        int visitMs = _rng.Next(4000, 10001);
        await Task.Delay(visitMs, ct);

        // After snapshot + scoring
        var outcome = _scoring.GenerateOutcome(currentScore);

        var afterSnap = new Snapshot
        {
            SessionId = session.Id,
            Type = "after",
            Score = outcome.AfterScore,
            Confidence = outcome.Confidence,
            TakenAt = DateTime.UtcNow
        };
        db.Snapshots.Add(afterSnap);

        // Complete session
        session.EndedAt = DateTime.UtcNow;
        session.Status = "completed";
        session.ExitEvent = new[] { "kortscan", "timeout", "doersensor" }[_rng.Next(3)];

        // Assessment
        var assessment = new Assessment
        {
            SessionId = session.Id,
            BeforeScore = outcome.BeforeScore,
            AfterScore = outcome.AfterScore,
            Confidence = outcome.Confidence,
            Result = outcome.Result,
            ChangeMetadata = JsonSerializer.Serialize(new { delta = outcome.Delta }),
            AssessedAt = DateTime.UtcNow
        };
        db.Assessments.Add(assessment);

        // Update toilet status
        string newStatus = _scoring.ScoreToStatus(outcome.AfterScore, outcome.Result);
        if (toilet.Status == null)
        {
            db.ToiletStatuses.Add(new ToiletStatus
            {
                ToiletId = toilet.Id,
                CurrentScore = outcome.AfterScore,
                Status = newStatus,
                LastUpdated = DateTime.UtcNow
            });
        }
        else
        {
            toilet.Status.CurrentScore = outcome.AfterScore;
            toilet.Status.Status = newStatus;
            toilet.Status.LastUpdated = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);

        await _hub.Clients.All.SendAsync("ReceiveSessionEnded",
            new { toiletId = toilet.Id, sessionId = session.Id, result = outcome.Result }, ct);

        await _hub.Clients.All.SendAsync("ReceiveStatusUpdate",
            new { toiletId = toilet.Id, status = newStatus, score = outcome.AfterScore }, ct);

        _logger.LogInformation("Motor: Session {SessionId} ended, result={Result}, score={Score:F2}",
            session.Id, outcome.Result, outcome.AfterScore);

        // Create trigger if needed
        if (outcome.TriggerSeverity != null)
        {
            var trigger = new CleaningTrigger
            {
                ToiletId = toilet.Id,
                SessionId = session.Id,
                Severity = outcome.TriggerSeverity,
                Status = "active",
                Confidence = outcome.Confidence,
                ChangeMetadata = JsonSerializer.Serialize(new { delta = outcome.Delta, result = outcome.Result }),
                CreatedAt = DateTime.UtcNow
            };
            db.CleaningTriggers.Add(trigger);
            await db.SaveChangesAsync(ct);

            await _hub.Clients.All.SendAsync("ReceiveTriggerCreated", new
            {
                trigger = new
                {
                    id = trigger.Id,
                    toiletId = trigger.ToiletId,
                    toiletName = toilet.Name,
                    severity = trigger.Severity,
                    status = trigger.Status,
                    confidence = trigger.Confidence,
                    createdAt = trigger.CreatedAt
                }
            }, ct);

            _logger.LogInformation("Motor: Trigger created for Toilet {ToiletId}, severity={Severity}",
                toilet.Id, trigger.Severity);

            // Schedule simulated cleaning team response (fire-and-forget)
            _ = ScheduleCleaningResponseAsync(trigger.Id, toilet.Id, ct);
        }

        // EventLog
        db.EventLogs.Add(new EventLog
        {
            EventType = "score",
            ToiletId = toilet.Id,
            SessionId = session.Id,
            Payload = JsonSerializer.Serialize(new { outcome.Result, outcome.AfterScore, outcome.Confidence }),
            LoggedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    // Simulates the cleaning team: acknowledge → complete or false-positive.
    // Response time: base 7 min ± 3–7 min (range 4–14 min).
    // If a human operator acts first, the motor steps aside gracefully.
    private async Task ScheduleCleaningResponseAsync(int triggerId, int toiletId, CancellationToken ct)
    {
        try
        {
            // ── Step 1: Acknowledge ─────────────────────────────────────────
            // 7 + [-3 .. +7] = 4–14 minutes
            int acknowledgeMinutes = 7 + _rng.Next(-3, 8);
            await Task.Delay(TimeSpan.FromMinutes(acknowledgeMinutes), ct);

            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<WctvDbContext>();
                var trigger = await db.CleaningTriggers.FindAsync(new object[] { triggerId }, ct);

                if (trigger == null || trigger.Status != "active")
                {
                    _logger.LogInformation(
                        "Motor: Trigger {TriggerId} already handled before acknowledge — skipping",
                        triggerId);
                    return;
                }

                trigger.Status = "acknowledged";
                trigger.AcknowledgedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }

            await _hub.Clients.All.SendAsync("ReceiveTriggerUpdated",
                new { triggerId, status = "acknowledged" }, ct);

            _logger.LogInformation("Motor: Auto-acknowledged trigger {TriggerId}", triggerId);

            // ── Step 2: Complete or false-positive ─────────────────────────
            // Cleaning takes 1–4 additional minutes
            int cleanMinutes = _rng.Next(1, 5);
            await Task.Delay(TimeSpan.FromMinutes(cleanMinutes), ct);

            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<WctvDbContext>();
                var trigger = await db.CleaningTriggers
                    .Include(t => t.Toilet).ThenInclude(t => t!.Status)
                    .FirstOrDefaultAsync(t => t.Id == triggerId, ct);

                if (trigger == null || trigger.Status != "acknowledged")
                {
                    _logger.LogInformation(
                        "Motor: Trigger {TriggerId} already handled before completion — skipping",
                        triggerId);
                    return;
                }

                // 15% false positive, 85% completed
                bool isFalsePositive = _rng.NextDouble() < 0.15;

                if (isFalsePositive)
                {
                    trigger.Status = "false_positive";
                    await db.SaveChangesAsync(ct);

                    await _hub.Clients.All.SendAsync("ReceiveTriggerUpdated",
                        new { triggerId, status = "false_positive" }, ct);

                    _logger.LogInformation("Motor: Auto-marked trigger {TriggerId} as false_positive", triggerId);
                }
                else
                {
                    trigger.Status = "completed";
                    db.CleaningReceipts.Add(new CleaningReceipt
                    {
                        TriggerId = triggerId,
                        CompletedAt = DateTime.UtcNow
                    });

                    if (trigger.Toilet?.Status != null)
                    {
                        trigger.Toilet.Status.CurrentScore = 0.92f;
                        trigger.Toilet.Status.Status = "ok";
                        trigger.Toilet.Status.LastUpdated = DateTime.UtcNow;

                        await _hub.Clients.All.SendAsync("ReceiveStatusUpdate",
                            new { toiletId, status = "ok", score = 0.92f }, ct);
                    }

                    await db.SaveChangesAsync(ct);

                    await _hub.Clients.All.SendAsync("ReceiveTriggerUpdated",
                        new { triggerId, status = "completed" }, ct);

                    _logger.LogInformation("Motor: Auto-completed trigger {TriggerId}", triggerId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Service is shutting down — exit quietly
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Motor: Error in cleaning response for trigger {TriggerId}", triggerId);
        }
    }
}
