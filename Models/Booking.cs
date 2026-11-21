using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Staybnb.Models;

public class Booking
{
    public int Id { get; set; }

    [Required]
    public int HostApplicationId { get; set; }

    public HostApplication? Property { get; set; }

    [Required]
    public string GuestUserId { get; set; } = string.Empty;

    public IdentityUser? GuestUser { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime CheckIn { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime CheckOut { get; set; }

    [Range(1, 20)]
    public int TravellerCount { get; set; } = 1;

    [Column(TypeName = "decimal(18,2)")]
    [Range(typeof(decimal), "0.01", "999999999")]
    public decimal TotalPrice { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
