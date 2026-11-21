using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models;

public class PropertyImage
{
    public int Id { get; set; }

    [Required, StringLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    public int HostApplicationId { get; set; }

    public HostApplication? HostApplication { get; set; }
}
