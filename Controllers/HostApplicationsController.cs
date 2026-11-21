
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;

namespace Staybnb.Controllers;

[Authorize]
public class HostApplicationsController(
    ApplicationDbContext db,
    UserManager<IdentityUser> userManager,
    IWebHostEnvironment environment) : Controller
{
    // GET: HostApplications/Create
    [HttpGet]
    [Authorize(Roles = "Guest")]
    public IActionResult Create()
    {
        return View(new HostApplication());
    }

    // POST: HostApplications/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Guest")]
    public async Task<IActionResult> Create(
        [Bind("Title,Description,PricePerNight,Address,City,PropertyType")]
        HostApplication application,
        IFormFile? propertyImage)
    {
        // A property image is mandatory.
        if (propertyImage == null || propertyImage.Length == 0)
        {
            ModelState.AddModelError(
                "propertyImage",
                "Please upload at least one property image.");
        }
        else if (propertyImage.Length > 5 * 1024 * 1024)
        {
            ModelState.AddModelError(
                "propertyImage",
                "The image must be 5 MB or smaller.");
        }
        else
        {
            var extension = Path.GetExtension(propertyImage.FileName)
                .ToLowerInvariant();

            var allowedExtensions = new[]
            {
                ".jpg", ".jpeg", ".png", ".webp"
            };

            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(
                    "propertyImage",
                    "Upload a JPG, JPEG, PNG, or WEBP image.");
            }
        }

        var user = await userManager.GetUserAsync(User);

        if (user == null)
        {
            return Challenge();
        }

        // The server assigns the applicant's ID; it is never entered in the form.
        application.UserId = user.Id;
        ModelState.Remove(nameof(application.UserId));

        if (!ModelState.IsValid)
        {
            return View(application);
        }

        // Store uploaded images under wwwroot/uploads/property-images.
        var webRoot = environment.WebRootPath
            ?? Path.Combine(environment.ContentRootPath, "wwwroot");

        var uploadFolder = Path.Combine(
            webRoot,
            "uploads",
            "property-images");

        Directory.CreateDirectory(uploadFolder);

        var extensionForFile = Path.GetExtension(propertyImage!.FileName)
            .ToLowerInvariant();

        var uniqueFileName = $"{Guid.NewGuid():N}{extensionForFile}";
        var fullPath = Path.Combine(uploadFolder, uniqueFileName);
        var relativePath = $"/uploads/property-images/{uniqueFileName}";

        await using (var stream = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write))
        {
            await propertyImage.CopyToAsync(stream);
        }

        // Set server-controlled values instead of trusting submitted data.
        application.UserId = user.Id;
        application.Status = "Pending";
        application.SubmittedAt = DateTime.UtcNow;

        application.Images.Add(new PropertyImage
        {
            ImageUrl = relativePath
        });

        try
        {
            db.HostApplications.Add(application);
            await db.SaveChangesAsync();
        }
        catch
        {
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }

            throw;
        }

        TempData["SuccessMessage"] =
            "Your Host application has been submitted for review.";

        return RedirectToAction(nameof(MyApplications));
    }

    // GET: HostApplications/MyApplications
    [HttpGet]
    [Authorize(Roles = "Guest,Host")]
    public async Task<IActionResult> MyApplications()
    {
        var user = await userManager.GetUserAsync(User);

        if (user == null)
        {
            return Challenge();
        }

        // Users can only view their own applications.
        var applications = await db.HostApplications
            .Where(application => application.UserId == user.Id)
            .OrderByDescending(application => application.SubmittedAt)
            .ToListAsync();

        return View(applications);
    }
}
