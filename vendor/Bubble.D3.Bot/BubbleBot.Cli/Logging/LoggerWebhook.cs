using Discord;
using Discord.Net.Rest;
using Discord.Rest;
using Discord.Webhook;

namespace BubbleBot.Cli.Logging;

public static class LoggerWebhook
{
    public static readonly DiscordWebhookClient? LoggingWebhook;
    public static readonly DiscordWebhookClient? LoggingArchiWebhook;
    public static readonly DiscordWebhookClient? LoggingChasseWebhook;
    public static readonly DiscordWebhookClient? LoggingVenteWebhook;
    public static readonly DiscordWebhookClient[] LoggingChatWebhooks;

    public static bool IsCustomWebhook { get; private set; }

    static LoggerWebhook()
    {
        var webhookLines = ReadConfiguredLines("webhook.txt");
        LoggingWebhook = CreateClient(webhookLines.ElementAtOrDefault(0));
        LoggingArchiWebhook = CreateClient(webhookLines.ElementAtOrDefault(1));
        LoggingChasseWebhook = CreateClient(webhookLines.ElementAtOrDefault(2));

        var chatWebhooksFile = File.Exists("special") || File.Exists("special.txt")
            ? "special-chat-webhooks.txt"
            : "chat-webhooks.txt";

        LoggingChatWebhooks = CreateClients(ReadConfiguredLines(chatWebhooksFile), useRetryMode: true);
        LoggingVenteWebhook = CreateClient(ReadConfiguredLines("vente-webhook.txt").FirstOrDefault(), useRetryMode: true);

        IsCustomWebhook = webhookLines.Length > 0;
    }

    public static void LogChat(string message)
    {
        foreach (var webhook in LoggingChatWebhooks)
        {
            _ = webhook.SendMessageAsync(message);
        }
    }

    public static async Task LogAsync(string message)
    {
        if (LoggingWebhook is null)
        {
            return;
        }

        await LoggingWebhook.SendMessageAsync(message);
    }
    
    public static async Task LogArchiAsync(string message)
    {
        if (LoggingArchiWebhook is null)
        {
            return;
        }

        await LoggingArchiWebhook.SendMessageAsync(message);
    }
    
    public static async Task LogChasseAsync(string message)
    {
        if (LoggingChasseWebhook is null)
        {
            return;
        }

        await LoggingChasseWebhook.SendMessageAsync(message);
    }

    private static string[] ReadConfiguredLines(string path)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        return File.ReadAllLines(path)
                   .Select(line => line.Trim())
                   .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'))
                   .ToArray();
    }

    private static DiscordWebhookClient? CreateClient(string? webhookUrl, bool useRetryMode = false)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            return null;
        }

        var config = new DiscordRestConfig();
        if (useRetryMode)
        {
            config.DefaultRetryMode = RetryMode.RetryRatelimit;
        }

        return new DiscordWebhookClient(webhookUrl, config);
    }

    private static DiscordWebhookClient[] CreateClients(IEnumerable<string> webhookUrls, bool useRetryMode = false)
    {
        return webhookUrls.Select(url => CreateClient(url, useRetryMode))
                          .OfType<DiscordWebhookClient>()
                          .ToArray();
    }
}
