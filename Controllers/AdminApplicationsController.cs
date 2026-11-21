using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;

namespace Staybnb.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminApplicationsController(
    ApplicationDbContext db,
    UserManager<IdentityUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var applications = await db.HostApplications
            .Include(a => a.Images)
            .Include(a => a.User)
            .OrderBy(a => a.Status == "Pending" ? 0 : 1)
            .ThenByDescending(a => a.SubmittedAt)
            .ToListAsync();

        return View(applications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var application = await db.HostApplications
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application == null)
            return NotFound();

        if (application.Status != "Pending")
        {
            TempData["ErrorMessage"] =
                "This application has already been reviewed.";
            return RedirectToAction(nameof(Index));
        }

        var user = await userManager.FindByIdAsync(application.UserId);

        if (user == null)
        {
            TempData["ErrorMessage"] =
                "The applicant's account could not be found.";
            return RedirectToAction(nameof(Index));
        }

        if (!await userManager.IsInRoleAsync(user, "Guest"))
        {
            TempData["ErrorMessage"] =
                "The applicant is not currently assigned the Guest role.";
            return RedirectToAction(nameof(Index));
        }

        var addHost = await userManager.AddToRoleAsync(user, "Host");

        if (!addHost.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join(
                "; ", addHost.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        var removeGuest = await userManager.RemoveFromRoleAsync(
            user, "Guest");

        if (!removeGuest.Succeeded)
        {
            await userManager.RemoveFromRoleAsync(user, "Host");

            TempData["ErrorMessage"] =
                "Role transition failed: " +
                string.Join("; ",
                    removeGuest.Errors.Select(e => e.Description));

            return RedirectToAction(nameof(Index));
        }

        application.Status = "Approved";

        try
        {
            await db.SaveChangesAsync();
        }
        catch
        {
            await userManager.AddToRoleAsync(user, "Guest");
            await userManager.RemoveFromRoleAsync(user, "Host");
            throw;
        }

        TempData["SuccessMessage"] =
            $"Application approved. {user.Email} is now a Host.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var application = await db.HostApplications
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application == null)
            return NotFound();

        if (application.Status != "Pending")
        {
            TempData["ErrorMessage"] =
                "This application has already been reviewed.";
            return RedirectToAction(nameof(Index));
        }

        application.Status = "Rejected";
        await db.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "The host application was rejected.";

        return RedirectToAction(nameof(Index));
    }
}
