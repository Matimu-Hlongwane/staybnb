using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;

namespace Staybnb.Controllers;

[Authorize]
public class DashboardController(
    ApplicationDbContext db,
    UserManager<IdentityUser> userManager) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        if (User.IsInRole("SuperAdmin") || User.IsInRole("Admin"))
            return RedirectToAction(nameof(Admin));

        if (User.IsInRole("Host"))
            return RedirectToAction(nameof(Host));

        return RedirectToAction(nameof(Guest));
    }

    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Admin()
    {
        ViewBag.PendingCount = await db.HostApplications
            .CountAsync(a => a.Status == "Pending");

        ViewBag.ApprovedCount = await db.HostApplications
            .CountAsync(a => a.Status == "Approved");

        ViewBag.RejectedCount = await db.HostApplications
            .CountAsync(a => a.Status == "Rejected");

        ViewBag.TotalCount = await db.HostApplications.CountAsync();

        return View();
    }

    [HttpGet]
    [Authorize(Roles = "Host,Guest")]
    public async Task<IActionResult> Host()
    {
        var user = await userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        var applications = await db.HostApplications
            .AsNoTracking()
            .Where(a => a.UserId == user.Id)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync();

        return View(applications);
    }

    [HttpGet]
    public IActionResult Guest()
    {
        return View();
    }
}
