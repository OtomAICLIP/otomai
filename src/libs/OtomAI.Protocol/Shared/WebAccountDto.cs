using System.Text.Json.Serialization;

namespace OtomAI.Protocol.Shared;

public class WebAccountDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("ogrines")]
    public int Ogrines { get; set; }

    [JsonPropertyName("votes")]
    public short Votes { get; set; }
}
