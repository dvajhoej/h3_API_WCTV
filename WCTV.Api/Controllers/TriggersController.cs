using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WCTV.Api.Data;
using WCTV.Api.Hubs;
using WCTV.Api.Models;

namespace WCTV.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TriggersController : ControllerBase
{
    private readonly WctvDbContext _db;
    private readonly IHubContext<DashboardHub> _hub;

    public TriggersController(WctvDbContext db, IHubContext<DashboardHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    [HttpGet]
    public async Task<IActionResult> GetActive()
    {
        var triggers = await _db.CleaningTriggers
            .Include(t => t.Toilet)
            .Where(t => t.Status == "active" || t.Status == "acknowledged")
            .ToListAsync();

        var deduped = triggers
            .GroupBy(t => t.ToiletId)
            .Select(g => g
                .OrderByDescending(t => t.Status == "acknowledged")
                .ThenByDescending(t => t.Severity == "forvaerring")
                .ThenByDescending(t => t.CreatedAt)
                .First())
            .OrderByDescending(t => t.Severity == "forvaerring")
            .ThenBy(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id,
                t.ToiletId,
                toiletName = t.Toilet.Name,
                t.Severity,
                t.Status,
                t.Confidence,
                t.CreatedAt,
                t.AcknowledgedAt
            })
            .ToList();

        return Ok(deduped);
    }

    [HttpPatch("{id}/acknowledge")]
    public async Task<IActionResult> Acknowledge(int id)
    {
        var trigger = await _db.CleaningTriggers.FindAsync(id);
        if (trigger == null) return NotFound();
        if (trigger.Status != "active") return BadRequest(new { error = "Trigger er ikke aktiv" });

        trigger.Status = "acknowledged";
        trigger.AcknowledgedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _hub.Clients.All.SendAsync("ReceiveTriggerUpdated",
            new { triggerId = id, status = "acknowledged" });

        return Ok(new { trigger.Id, trigger.Status });
    }

    [HttpPatch("{id}/complete")]
    public async Task<IActionResult> Complete(int id)
    {
        var trigger = await _db.CleaningTriggers
            .Include(t => t.Toilet).ThenInclude(t => t!.Status)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trigger == null) return NotFound();
        if (trigger.Status == "completed") return BadRequest(new { error = "Allerede fuldfort" });

        trigger.Status = "completed";

        var receipt = new CleaningReceipt
        {
            TriggerId = id,
            CompletedAt = DateTime.UtcNow
        };
        _db.CleaningReceipts.Add(receipt);

        // Reset toilet score
        if (trigger.Toilet?.Status != null)
        {
            trigger.Toilet.Status.CurrentScore = 0.92f;
            trigger.Toilet.Status.Status = "ok";
            trigger.Toilet.Status.LastUpdated = DateTime.UtcNow;

            await _hub.Clients.All.SendAsync("ReceiveStatusUpdate",
                new { toiletId = trigger.ToiletId, status = "ok", score = 0.92f });
        }

        await _db.SaveChangesAsync();

        await _hub.Clients.All.SendAsync("ReceiveTriggerUpdated",
            new { triggerId = id, status = "completed" });

        return Ok(new { id, status = "completed" });
    }

    [HttpPatch("{id}/false-positive")]
    public async Task<IActionResult> FalsePositive(int id)
    {
        var trigger = await _db.CleaningTriggers.FindAsync(id);
        if (trigger == null) return NotFound();

        trigger.Status = "false_positive";
        await _db.SaveChangesAsync();

        await _hub.Clients.All.SendAsync("ReceiveTriggerUpdated",
            new { triggerId = id, status = "false_positive" });

        return Ok(new { id, status = "false_positive" });
    }
}
