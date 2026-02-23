using System.ComponentModel.DataAnnotations;

namespace WCTV.Api.Models;

public class Toilet
{
    public int Id { get; set; }
    [Required] public string Name { get; set; } = "";
    [Required] public string Location { get; set; } = "";
    public int StallNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ToiletStatus? Status { get; set; }
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
    public ICollection<CleaningTrigger> CleaningTriggers { get; set; } = new List<CleaningTrigger>();
}
