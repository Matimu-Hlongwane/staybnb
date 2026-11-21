using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Staybnb.Models;

public class HostApplication
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Range(1, 1000000)]
    public decimal PricePerNight { get; set; }

    [Required, StringLength(250)]
    public string Address { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(60)]
    public string PropertyType { get; set; } = string.Empty;

    [Required]
    public string UserId { get; set; } = string.Empty;

    public IdentityUser? User { get; set; }

    public string Status { get; set; } = "Pending";

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PropertyImage> Images { get; set; }
        = new List<PropertyImage>();
}
