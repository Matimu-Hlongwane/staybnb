using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Models;

namespace Staybnb.Controllers;

[Authorize(Roles = "SuperAdmin")]
public class UserManagementController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    private static readonly string[] AppRoles =
        { "Guest", "Host", "Admin", "SuperAdmin" };

    public UserManagementController(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users
            .OrderBy(u => u.Email)
            .ToListAsync();

        var result = new List<UserRoleViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            result.Add(new UserRoleViewModel
            {
                Id = user.Id,
                Email = user.Email ?? "(no email)",
                CurrentRole = AppRoles.FirstOrDefault(r => roles.Contains(r))
                    ?? "Unassigned"
            });
        }

        ViewBag.AvailableRoles = AppRoles;
        return View(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(string userId, string role)
    {
        if (string.IsNullOrWhiteSpace(userId) ||
            !AppRoles.Contains(role))
        {
            TempData["Error"] = "Choose a valid user and role.";
            return RedirectToAction(nameof(Index));
        }

        if (!await _roleManager.RoleExistsAsync(role))
        {
            TempData["Error"] = $"The {role} role does not exist.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction(nameof(Index));
        }

        var existingRoles = await _userManager.GetRolesAsync(user);
        var currentAppRole = AppRoles.FirstOrDefault(r => existingRoles.Contains(r));

        if (currentAppRole == "SuperAdmin" && role != "SuperAdmin")
        {
            var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");

            if (superAdmins.Count <= 1)
            {
                TempData["Error"] =
                    "The last SuperAdmin cannot be demoted.";
                return RedirectToAction(nameof(Index));
            }
        }

        var removal = await _userManager.RemoveFromRolesAsync(
            user, existingRoles.Where(r => AppRoles.Contains(r)));

        if (!removal.Succeeded)
        {
            TempData["Error"] = string.Join(" ",
                removal.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        var addition = await _userManager.AddToRoleAsync(user, role);

        if (!addition.Succeeded)
        {
            TempData["Error"] =
                "The old role was removed, but the new role could not be assigned: " +
                string.Join(" ", addition.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] =
            $"Updated {user.Email ?? "user"} to {role}.";
        return RedirectToAction(nameof(Index));
    }
}
