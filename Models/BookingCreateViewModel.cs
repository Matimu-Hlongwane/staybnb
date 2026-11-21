using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models;

public class BookingCreateViewModel
{
    public int HostApplicationId { get; set; }

    public string PropertyTitle { get; set; } = "";
    public string PropertyCity { get; set; } = "";
    public decimal PricePerNight { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime CheckIn { get; set; } = DateTime.Today.AddDays(1);

    [Required]
    [DataType(DataType.Date)]
    public DateTime CheckOut { get; set; } = DateTime.Today.AddDays(2);

    [Range(1, 20)]
    public int TravellerCount { get; set; } = 1;
}
