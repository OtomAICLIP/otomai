namespace OtomAI.Bot.Models;

/// <summary>
/// Per-account bot settings. Mirrors Bubble.D3.Bot's SaharachAccount.
/// </summary>
public sealed class AccountSettings
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public int ServerId { get; set; }
    public string? Hwid { get; set; }
    public string? ProxyHost { get; set; }
    public int ProxyPort { get; set; }
    public string? ProxyUser { get; set; }
    public string? ProxyPassword { get; set; }
    public string? Trajet { get; set; }
    public bool IsBank { get; set; }
    public bool IsKoli { get; set; }
    public bool AutoPass { get; set; }
    public string LoginHost { get; set; } = "dofus2-co-production.ankama-games.com";
    public int LoginPort { get; set; } = 443;
}
