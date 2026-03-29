namespace OtomAI.Core;

/// <summary>
/// Global bot configuration. Mirrors Bubble.Core's Kernel.Configuration.
/// Loaded from config file or environment at startup.
/// </summary>
public sealed class Configuration
{
    public string DofusInstallPath { get; set; } = "";
    public string DataPath { get; set; } = "Data";
    public string AccountsPath { get; set; } = "accounts.json";
    public string WebhooksPath { get; set; } = "webhooks";
    public string LogsPath { get; set; } = "logs";
    public int MaxConcurrentBots { get; set; } = 4;
    public bool EnableDiscordWebhook { get; set; }
    public string DiscordWebhookUrl { get; set; } = "";
    public int KeepAliveIntervalSeconds { get; set; } = 30;
    public int ReconnectDelaySeconds { get; set; } = 10;
    public int MaxReconnectAttempts { get; set; } = 5;
}
