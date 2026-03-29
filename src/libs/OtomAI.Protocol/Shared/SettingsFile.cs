using System.Text.Json.Serialization;

namespace OtomAI.Protocol.Shared;

public class SettingsFile
{
    [JsonPropertyName("USER_ACCOUNTS")]
    public List<AnkamaAccount> UserAccounts { get; set; } = [];
}

public class AnkamaAccount
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;

    [JsonPropertyName("isGuest")]
    public bool IsGuest { get; set; }

    [JsonPropertyName("nicknameWithTag")]
    public string NicknameWithTag { get; set; } = string.Empty;

    [JsonPropertyName("security")]
    public List<string> Security { get; set; } = new();

    [JsonPropertyName("avatar")]
    public string Avatar { get; set; } = string.Empty;

    [JsonPropertyName("needRefresh")]
    public bool NeedRefresh { get; set; }

    [JsonPropertyName("addedDate")]
    public DateTime AddedDate { get; set; }

    [JsonPropertyName("proxy")]
    public string? Proxy { get; set; } = string.Empty;
}
