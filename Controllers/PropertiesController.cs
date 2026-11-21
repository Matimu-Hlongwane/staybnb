using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;

namespace Staybnb.Controllers;

public class PropertiesController(ApplicationDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var properties = await db.HostApplications
            .AsNoTracking()
            .Include(p => p.Images)
            .Where(p => p.Status == "Approved")
            .OrderByDescending(p => p.SubmittedAt)
            .ToListAsync();

        return View(properties);
    }
}
