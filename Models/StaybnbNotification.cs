using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models;

public class StaybnbNotification
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = "";

    [Required, StringLength(120)]
    public string Title { get; set; } = "";

    [Required, StringLength(1000)]
    public string Message { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
}
