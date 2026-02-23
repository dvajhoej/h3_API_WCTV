using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WCTV.Api.Data;

namespace WCTV.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ToiletsController : ControllerBase
{
    private readonly WctvDbContext _db;
    public ToiletsController(WctvDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var toilets = await _db.Toilets
            .Include(t => t.Status)
            .OrderBy(t => t.StallNumber)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Location,
                t.StallNumber,
                status = t.Status == null ? null : new
                {
                    t.Status.CurrentScore,
                    t.Status.Status,
                    t.Status.LastUpdated
                },
                activeSession = _db.Sessions
                    .Where(s => s.ToiletId == t.Id && s.Status == "active")
                    .Select(s => new { s.Id, s.StartedAt })
                    .FirstOrDefault()
            })
            .ToListAsync();
        return Ok(toilets);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var toilet = await _db.Toilets
            .Include(t => t.Status)
            .Where(t => t.Id == id)
            .FirstOrDefaultAsync();

        if (toilet == null) return NotFound();

        var recentSessions = await _db.Sessions
            .Where(s => s.ToiletId == id)
            .Include(s => s.Assessment)
            .OrderByDescending(s => s.StartedAt)
            .Take(10)
            .ToListAsync();

        return Ok(new { toilet, recentSessions });
    }

    [HttpPost("feed")]
    public IActionResult Feed() => StatusCode(403, new { error = "Live feed disabled (FR-12 -- privathed)" });
}
