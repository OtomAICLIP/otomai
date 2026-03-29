using System.Text.Json.Serialization;

namespace OtomAI.Protocol.Shared.Api.Characters;

public class CharacterMetaApiResult
{
    [JsonPropertyName("character_id")]
    public long CharacterId { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("error")]
    public int? Error { get; set; }
}
