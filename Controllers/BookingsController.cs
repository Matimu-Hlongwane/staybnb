using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;

namespace Staybnb.Controllers;

[Authorize]
public class BookingsController(
    ApplicationDbContext db,
    UserManager<IdentityUser> userManager) : Controller
{
    private async Task<BookingCreateViewModel?> GetBookingFormAsync(
        int propertyId, DateTime? checkIn = null,
        DateTime? checkOut = null, int travellerCount = 1)
    {
        var property = await db.HostApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.Id == propertyId && p.Status == "Approved");

        if (property == null)
            return null;

        return new BookingCreateViewModel
        {
            HostApplicationId = property.Id,
            PropertyTitle = property.Title,
            PropertyCity = property.City,
            PricePerNight = property.PricePerNight,
            CheckIn = checkIn ?? DateTime.Today.AddDays(1),
            CheckOut = checkOut ?? DateTime.Today.AddDays(2),
            TravellerCount = travellerCount
        };
    }

    [HttpGet]
    public async Task<IActionResult> Create(int propertyId)
    {
        var model = await GetBookingFormAsync(propertyId);

        if (model == null)
            return NotFound("This property is unavailable.");

        var userId = userManager.GetUserId(User);

        var property = await db.HostApplications
            .AsNoTracking()
            .FirstAsync(p => p.Id == propertyId);

        if (property.UserId == userId)
            return Forbid();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingCreateViewModel model)
    {
        var userId = userManager.GetUserId(User);

        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        var property = await db.HostApplications
            .FirstOrDefaultAsync(p =>
                p.Id == model.HostApplicationId &&
                p.Status == "Approved");

        if (property == null)
            return NotFound("This property is unavailable.");

        if (property.UserId == userId)
            return Forbid();

        var today = DateTime.Today;
        var checkIn = model.CheckIn.Date;
        var checkOut = model.CheckOut.Date;

        if (checkIn < today)
            ModelState.AddModelError(nameof(model.CheckIn),
                "Check-in cannot be in the past.");

        if (checkOut <= checkIn)
            ModelState.AddModelError(nameof(model.CheckOut),
                "Check-out must be after check-in.");

        if (model.TravellerCount < 1 || model.TravellerCount > 20)
            ModelState.AddModelError(nameof(model.TravellerCount),
                "Choose between 1 and 20 travellers.");

        if (ModelState.IsValid)
        {
            var overlap = await db.Bookings.AnyAsync(b =>
                b.HostApplicationId == property.Id &&
                b.Status == "Approved" &&
                b.CheckIn < checkOut &&
                b.CheckOut > checkIn);

            if (overlap)
                ModelState.AddModelError("",
                    "These dates are unavailable. Please choose different dates.");
        }

        if (!ModelState.IsValid)
        {
            model.PropertyTitle = property.Title;
            model.PropertyCity = property.City;
            model.PricePerNight = property.PricePerNight;
            return View(model);
        }

        var nights = (checkOut - checkIn).Days;
        var total = property.PricePerNight * nights;

        db.Bookings.Add(new Booking
        {
            HostApplicationId = property.Id,
            GuestUserId = userId,
            CheckIn = checkIn,
            CheckOut = checkOut,
            TravellerCount = model.TravellerCount,
            TotalPrice = total,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        TempData["Success"] = "Booking request submitted. The host must approve it.";
        return RedirectToAction(nameof(MyBookings));
    }

    [HttpGet]
    public async Task<IActionResult> MyBookings()
    {
        var userId = userManager.GetUserId(User);

        var bookings = await db.Bookings
            .AsNoTracking()
            .Include(b => b.Property)
            .Where(b => b.GuestUserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return View(bookings);
    }

    [Authorize(Roles = "Host,Admin,SuperAdmin")]
    [HttpGet]
    public async Task<IActionResult> HostRequests()
    {
        var userId = userManager.GetUserId(User);
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

        var query = db.Bookings
            .AsNoTracking()
            .Include(b => b.Property)
            .Include(b => b.GuestUser)
            .AsQueryable();

        if (!isAdmin)
            query = query.Where(b => b.Property != null &&
                                     b.Property.UserId == userId);

        var bookings = await query
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return View(bookings);
    }

    [Authorize(Roles = "Host,Admin,SuperAdmin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Respond(int id, string decision)
    {
        if (decision != "Approve" && decision != "Reject")
            return BadRequest();

        var userId = userManager.GetUserId(User);
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

        var booking = await db.Bookings
            .Include(b => b.Property)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking?.Property == null)
            return NotFound();

        if (!isAdmin && booking.Property.UserId != userId)
            return Forbid();

        if (booking.Status != "Pending")
        {
            TempData["Error"] = "This booking has already been processed.";
            return RedirectToAction(nameof(HostRequests));
        }

        if (decision == "Approve")
        {
            var overlap = await db.Bookings.AnyAsync(b =>
                b.Id != booking.Id &&
                b.HostApplicationId == booking.HostApplicationId &&
                b.Status == "Approved" &&
                b.CheckIn < booking.CheckOut &&
                b.CheckOut > booking.CheckIn);

            if (overlap)
            {
                TempData["Error"] =
                    "This booking overlaps another approved booking.";
                return RedirectToAction(nameof(HostRequests));
            }

            booking.Status = "Approved";
        }
        else
        {
            booking.Status = "Rejected";
        }

        await db.SaveChangesAsync();

        TempData["Success"] = $"Booking {booking.Status.ToLowerInvariant()}.";
        return RedirectToAction(nameof(HostRequests));
    }
}
