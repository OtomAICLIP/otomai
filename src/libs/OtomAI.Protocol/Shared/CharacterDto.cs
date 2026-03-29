using System.Text.Json.Serialization;

namespace OtomAI.Protocol.Shared;

public class CharacterDto
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("breed")]
    public required sbyte Breed { get; init; }

    [JsonPropertyName("level")]
    public required int Level { get; init; }

    [JsonPropertyName("prestige")]
    public required int Prestige { get; init; }

    [JsonPropertyName("look")]
    public required string Look { get; init; }
}
