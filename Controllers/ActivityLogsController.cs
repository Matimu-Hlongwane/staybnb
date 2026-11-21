using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;

namespace Staybnb.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class ActivityLogsController(ApplicationDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var logs = await db.ActivityLogs.AsNoTracking()
            .OrderByDescending(log => log.CreatedAt)
            .Take(500)
            .ToListAsync();

        return View(logs);
    }
}
