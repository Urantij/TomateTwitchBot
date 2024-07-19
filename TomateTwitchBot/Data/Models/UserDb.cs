using System.ComponentModel.DataAnnotations;

namespace TomateTwitchBot.Data.Models;

public class UserDb
{
    [Key] public int Id { get; set; }

    [Required] public string TwitchId { get; set; }

    public string? LastSeenUsername { get; set; }

    public ICollection<TimeoutDb> Killed { get; set; }
    public ICollection<TimeoutDb> Died { get; set; }
}