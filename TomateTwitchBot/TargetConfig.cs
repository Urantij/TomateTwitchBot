using System.ComponentModel.DataAnnotations;

namespace TomateTwitchBot;

public class TargetConfig
{
    [Required] public required string Name { get; init; }
    [Required] public required string Id { get; init; }
    [Required] public required string RewardId { get; init; }

    public int MinTimeoutTime { get; init; }
    public int MaxTimeoutTime { get; init; }
}