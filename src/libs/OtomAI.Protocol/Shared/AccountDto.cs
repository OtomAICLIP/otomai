using System.Text.Json.Serialization;

namespace OtomAI.Protocol.Shared;

public sealed class AccountDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public int Role { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("token_expiration")]
    public DateTime? TokenExpiration { get; set; }

    [JsonPropertyName("haapi_token")]
    public string HaapiToken { get; set; } = string.Empty;

    [JsonPropertyName("ticket")]
    public string Ticket { get; set; } = string.Empty;

    [JsonPropertyName("nickname")]
    public string? Nickname { get; set; } = string.Empty;

    [JsonPropertyName("tag")]
    public string Tag { get; set; } = "0000";

    [JsonPropertyName("secret_code")]
    public string SecretCode { get; set; } = string.Empty;

    [JsonPropertyName("ban_expire_at")]
    public DateTime? BanExpireAt { get; set; } = DateTime.UnixEpoch;

    [JsonPropertyName("ban_reason")]
    public string? BanReason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UnixEpoch;

    [JsonPropertyName("last_hwid")]
    public string LastHwid { get; set; } = string.Empty;

    [JsonPropertyName("last_ip")]
    public string LastIp { get; set; } = string.Empty;

    [JsonPropertyName("last_activity")]
    public DateTime? LastActivity { get; set; } = DateTime.UnixEpoch;

    [JsonPropertyName("last_updated_at")]
    public DateTime? LastUpdatedAt { get; set; } = DateTime.UnixEpoch;

    [JsonPropertyName("web_account_id")]
    public int WebAccountId { get; set; }

    [JsonIgnore]
    public bool IsAdmin => Role > 1;
}
