using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;

namespace Staybnb.Controllers;

[Authorize]
public class CommunicationController(
    ApplicationDbContext db,
    UserManager<IdentityUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = userManager.GetUserId(User);
        var messages = await db.Messages.AsNoTracking()
            .Where(m => m.RecipientUserId == userId || m.SenderUserId == userId)
            .OrderByDescending(m => m.SentAt)
            .ToListAsync();

        return View(messages);
    }

    [HttpGet]
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string recipientEmail, string body)
    {
        recipientEmail = recipientEmail?.Trim() ?? "";
        body = body?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(recipientEmail) ||
            string.IsNullOrWhiteSpace(body) || body.Length > 2000)
        {
            ModelState.AddModelError("", "Enter a recipient email and a message of up to 2000 characters.");
            return View();
        }

        var senderId = userManager.GetUserId(User);
        var recipient = await userManager.FindByEmailAsync(recipientEmail);

        if (recipient == null)
        {
            ModelState.AddModelError(nameof(recipientEmail), "No account was found for that email.");
            return View();
        }

        if (recipient.Id == senderId)
        {
            ModelState.AddModelError(nameof(recipientEmail), "You cannot message yourself.");
            return View();
        }

        db.Messages.Add(new StaybnbMessage
        {
            SenderUserId = senderId!,
            RecipientUserId = recipient.Id,
            Body = body
        });

        db.Notifications.Add(new StaybnbNotification
        {
            UserId = recipient.Id,
            Title = "New message",
            Message = $"You received a new message from {User.Identity?.Name ?? "a Staybnb user"}."
        });

        db.ActivityLogs.Add(new StaybnbActivityLog
        {
            UserId = senderId,
            Action = "MessageSent",
            Details = $"A message was sent to account {recipient.Id}."
        });

        await db.SaveChangesAsync();
        TempData["Success"] = "Your message has been sent.";
        return RedirectToAction(nameof(Index));
    }
}
