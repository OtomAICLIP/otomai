using System.Text.Json.Serialization;

namespace BubbleBot.Cli.Models;

public class GameRelease
{
    [JsonPropertyName("location")] public required string Location { get; set; }
}