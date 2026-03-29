using System.Text.Json.Serialization;

namespace OtomAI.Protocol.Shared.Api.Characters;

public class CharacterStateDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("account_id")]
    public required int AccountId { get; set; }

    [JsonPropertyName("web_account_id")]
    public required int WebAccountId { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; } = string.Empty;

    [JsonPropertyName("experience")]
    public required ulong Experience { get; set; }

    [JsonPropertyName("level")]
    public required int Level { get; set; }

    [JsonPropertyName("breed")]
    public required short Breed { get; set; }

    [JsonPropertyName("head")]
    public required int Head { get; set; }

    [JsonPropertyName("sex")]
    public required bool Gender { get; set; }

    [JsonPropertyName("look")]
    public required string Look { get; set; } = string.Empty;

    [JsonPropertyName("server_id")]
    public required short ServerId { get; set; }
}
