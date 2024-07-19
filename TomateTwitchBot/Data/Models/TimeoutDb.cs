using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TomateTwitchBot.Data.Models;

public class TimeoutDb
{
    [Key] public int Id { get; set; }

    [Required] public string Text { get; set; }

    [Required] public double Roll { get; set; }

    [ForeignKey(nameof(Killer))] public int KillerId { get; set; }
    public UserDb Killer { get; set; }

    [ForeignKey(nameof(Victim))] public int VictimId { get; set; }
    public UserDb Victim { get; set; }

    [Required] public DateTimeOffset Date { get; set; }

    public TimeoutDb()
    {
    }

    public TimeoutDb(string text, double roll, int killerId, int victimId, DateTimeOffset date)
    {
        Text = text;
        Roll = roll;
        KillerId = killerId;
        VictimId = victimId;
        Date = date;
    }
}