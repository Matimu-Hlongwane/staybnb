using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models;

public class CheckInRecord
{
    public int Id { get; set; }

    [Required]
    public int BookingId { get; set; }

    public Booking? Booking { get; set; }

    [Required]
    public string GuestUserId { get; set; } = "";

    [Required, StringLength(30)]
    public string DocumentType { get; set; } = "ID";

    [Required, StringLength(20)]
    public string VerificationStatus { get; set; } = "Pending";

    [Required, StringLength(20)]
    public string CheckInStatus { get; set; } = "Not Checked In";

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public DateTime? VerifiedAt { get; set; }

    public string? VerifiedByUserId { get; set; }

    public DateTime? CheckedInAt { get; set; }

    [StringLength(500)]
    public string? ReviewNotes { get; set; }
}
