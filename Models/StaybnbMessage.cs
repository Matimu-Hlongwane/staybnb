using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models;

public class StaybnbMessage
{
    public int Id { get; set; }

    [Required]
    public string SenderUserId { get; set; } = "";

    [Required]
    public string RecipientUserId { get; set; } = "";

    [Required, StringLength(2000, MinimumLength = 1)]
    public string Body { get; set; } = "";

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
}
