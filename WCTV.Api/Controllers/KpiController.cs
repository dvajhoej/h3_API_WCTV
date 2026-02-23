using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WCTV.Api.Data;

namespace WCTV.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KpiController : ControllerBase
{
    private readonly WctvDbContext _db;
    public KpiController(WctvDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetKpi()
    {
        var now = DateTime.UtcNow;
        var weekAgo = now.AddDays(-7);

        var totalSessions = await _db.Sessions.CountAsync(s => s.StartedAt >= weekAgo && s.Status == "completed");
        var deteriorations = await _db.Assessments
            .CountAsync(a => a.AssessedAt >= weekAgo &&
                (a.Result == "forvaerring" || a.Result == "let_forvaerring"));

        float deteriorationRate = totalSessions > 0
            ? (float)deteriorations / totalSessions * 100
            : 0;

        var completedTriggers = await _db.CleaningTriggers
            .Include(t => t.Receipt)
            .Where(t => t.Status == "completed" && t.Receipt != null && t.CreatedAt >= weekAgo)
            .ToListAsync();

        double avgResponseMinutes = completedTriggers.Any()
            ? completedTriggers.Average(t => (t.Receipt!.CompletedAt - t.CreatedAt).TotalMinutes)
            : 0;

        var activeTriggers = await _db.CleaningTriggers
            .CountAsync(t => t.Status == "active" || t.Status == "acknowledged");

        var okSessions = await _db.Assessments
            .CountAsync(a => a.AssessedAt >= weekAgo && a.Result == "ok");
        float okRate = totalSessions > 0 ? (float)okSessions / totalSessions * 100 : 0;

        return Ok(new
        {
            deteriorationRate = Math.Round(deteriorationRate, 1),
            avgResponseMinutes = Math.Round(avgResponseMinutes, 1),
            activeTriggers,
            okRate = Math.Round(okRate, 1),
            totalSessionsThisWeek = totalSessions,
            period = "uge"
        });
    }

    [HttpGet("daily")]
    public async Task<IActionResult> GetDaily()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var assessments = await _db.Assessments
            .Where(a => a.AssessedAt >= today && a.AssessedAt < tomorrow)
            .ToListAsync();

        var hourly = assessments
            .GroupBy(a => a.AssessedAt.Hour)
            .Select(g => new
            {
                hour = g.Key,
                avgScore = Math.Round(g.Average(a => (double)a.AfterScore) * 100, 1),
                visits = g.Count(),
                needsCleaning = g.Count(a => a.Result == "forvaerring" || a.Result == "let_forvaerring")
            })
            .OrderBy(h => h.hour)
            .ToList();

        return Ok(hourly);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] string period = "uge")
    {
        var now = DateTime.UtcNow;
        var from = period == "maaned" ? now.AddDays(-30) : now.AddDays(-7);

        var sessions = await _db.Sessions
            .Include(s => s.Assessment)
            .Where(s => s.StartedAt >= from && s.Status == "completed")
            .ToListAsync();

        var triggers = await _db.CleaningTriggers
            .Include(t => t.Receipt)
            .Where(t => t.CreatedAt >= from)
            .ToListAsync();

        return Ok(new
        {
            exportedAt = now,
            period,
            from,
            to = now,
            totalSessions = sessions.Count,
            results = sessions
                .Where(s => s.Assessment != null)
                .GroupBy(s => s.Assessment!.Result)
                .Select(g => new { result = g.Key, count = g.Count() }),
            triggers = new
            {
                total = triggers.Count,
                completed = triggers.Count(t => t.Status == "completed"),
                falsePositive = triggers.Count(t => t.Status == "false_positive"),
                active = triggers.Count(t => t.Status is "active" or "acknowledged")
            }
        });
    }
}
