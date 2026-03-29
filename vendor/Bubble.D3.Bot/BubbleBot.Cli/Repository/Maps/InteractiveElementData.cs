using System.Text.Json.Serialization;

namespace BubbleBot.Cli.Repository.Maps;

public class InteractiveElementData
{
    [JsonPropertyName("gfxId")] public int GfxId { get; set; }

    [JsonPropertyName("cellId")] public int CellId { get; set; }

    [JsonPropertyName("interactionId")] public int InteractionId { get; set; }
}