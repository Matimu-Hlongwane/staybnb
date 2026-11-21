using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;

namespace Staybnb.Controllers;

[Authorize]
public class CheckInsController(
    ApplicationDbContext db,
    UserManager<IdentityUser> userManager) : Controller
{
    private bool IsAdmin =>
        User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = userManager.GetUserId(User);

        var query = db.CheckInRecords
            .AsNoTracking()
            .Include(r => r.Booking)
                .ThenInclude(b => b!.Property)
            .AsQueryable();

        if (!IsAdmin)
        {
            query = query.Where(r =>
                r.GuestUserId == userId ||
                (r.Booking != null &&
                 r.Booking.Property != null &&
                 r.Booking.Property.UserId == userId));
        }

        var records = await query
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync();

        return View(records);
    }

    [HttpGet]
    public async Task<IActionResult> Submit(int bookingId)
    {
        var userId = userManager.GetUserId(User);

        var booking = await db.Bookings
            .AsNoTracking()
            .Include(b => b.Property)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
            return NotFound();

        if (booking.GuestUserId != userId)
            return Forbid();

        if (booking.Status != "Approved")
        {
            TempData["Error"] = "Identity verification is available after your booking is approved.";
            return RedirectToAction(nameof(Index));
        }

        var existing = await db.CheckInRecords
            .AsNoTracking()
            .AnyAsync(r => r.BookingId == bookingId);

        if (existing)
        {
            TempData["Error"] = "Verification has already been submitted for this booking.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.BookingId = bookingId;
        ViewBag.PropertyTitle = booking.Property?.Title ?? "Your booking";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int bookingId, string documentType)
    {
        var userId = userManager.GetUserId(User);

        if (documentType != "National ID" &&
            documentType != "Passport" &&
            documentType != "Driver's Licence")
        {
            ModelState.AddModelError(nameof(documentType), "Select a valid document type.");
        }

        var booking = await db.Bookings
            .Include(b => b.Property)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
            return NotFound();

        if (booking.GuestUserId != userId)
            return Forbid();

        if (booking.Status != "Approved")
            return Forbid();

        if (await db.CheckInRecords.AnyAsync(r => r.BookingId == bookingId))
        {
            TempData["Error"] = "Verification has already been submitted.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            ViewBag.BookingId = bookingId;
            ViewBag.PropertyTitle = booking.Property?.Title ?? "Your booking";
            return View();
        }

        db.CheckInRecords.Add(new CheckInRecord
        {
            BookingId = bookingId,
            GuestUserId = userId!,
            DocumentType = documentType,
            VerificationStatus = "Pending",
            CheckInStatus = "Not Checked In"
        });

        db.ActivityLogs.Add(new StaybnbActivityLog
        {
            UserId = userId,
            Action = "IdentityVerificationSubmitted",
            Details = $"Document type submitted for booking {bookingId}."
        });

        await db.SaveChangesAsync();

        TempData["Success"] = "Your verification request has been submitted.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Host,Admin,SuperAdmin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int id, string decision, string? notes)
    {
        if (decision != "Approve" && decision != "Reject")
            return BadRequest();

        var userId = userManager.GetUserId(User);

        var record = await db.CheckInRecords
            .Include(r => r.Booking)
                .ThenInclude(b => b!.Property)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (record?.Booking?.Property == null)
            return NotFound();

        if (!IsAdmin && record.Booking.Property.UserId != userId)
            return Forbid();

        if (record.VerificationStatus != "Pending")
        {
            TempData["Error"] = "This verification request has already been reviewed.";
            return RedirectToAction(nameof(Index));
        }

        record.VerificationStatus = decision == "Approve" ? "Verified" : "Rejected";
        record.VerifiedAt = DateTime.UtcNow;
        record.VerifiedByUserId = userId;
        record.ReviewNotes = string.IsNullOrWhiteSpace(notes)
            ? null
            : notes.Trim()[..Math.Min(notes.Trim().Length, 500)];

        db.ActivityLogs.Add(new StaybnbActivityLog
        {
            UserId = userId,
            Action = "IdentityVerificationReviewed",
            Details = $"Verification record {id} was {record.VerificationStatus}."
        });

        await db.SaveChangesAsync();

        TempData["Success"] = $"Verification {record.VerificationStatus.ToLowerInvariant()}.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Host,Admin,SuperAdmin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkCheckedIn(int id)
    {
        var userId = userManager.GetUserId(User);

        var record = await db.CheckInRecords
            .Include(r => r.Booking)
                .ThenInclude(b => b!.Property)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (record?.Booking?.Property == null)
            return NotFound();

        if (!IsAdmin && record.Booking.Property.UserId != userId)
            return Forbid();

        if (record.VerificationStatus != "Verified" ||
            record.Booking.Status != "Approved")
        {
            TempData["Error"] = "The booking and identity verification must both be approved before check-in.";
            return RedirectToAction(nameof(Index));
        }

        if (record.CheckInStatus == "Checked In")
        {
            TempData["Error"] = "This guest has already been checked in.";
            return RedirectToAction(nameof(Index));
        }

        record.CheckInStatus = "Checked In";
        record.CheckedInAt = DateTime.UtcNow;

        db.ActivityLogs.Add(new StaybnbActivityLog
        {
            UserId = userId,
            Action = "GuestCheckedIn",
            Details = $"Check-in record {id} was completed."
        });

        await db.SaveChangesAsync();

        TempData["Success"] = "Guest successfully checked in.";
        return RedirectToAction(nameof(Index));
    }
}
