using System.Text.Json.Serialization;

namespace Bubble.Shared.Ankama;

public class AnkamaAccountInfo
{
    [JsonPropertyName("id")] public required int Id { get; set; }

    [JsonPropertyName("uuid")] public required string Uuid { get; set; }

    [JsonPropertyName("type")] public required string Type { get; set; }

    [JsonPropertyName("login")] public required string Login { get; set; }
}

public class AnkamaAccountInfoWithKey
{
    [JsonPropertyName("key")] public required string Key { get; set; }

    [JsonPropertyName("refresh_token")] public required string RefreshToken { get; set; }

    [JsonPropertyName("account_id")] public required int AccountId { get; set; }

}