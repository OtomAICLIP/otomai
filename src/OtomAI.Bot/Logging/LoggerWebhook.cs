using System.Text;
using System.Text.Json;
using Serilog;

namespace OtomAI.Bot.Logging;

/// <summary>
/// Discord webhook logger. Mirrors Bubble.D3.Bot's LoggerWebhook.
/// Sends formatted messages to Discord for remote monitoring.
/// </summary>
public sealed class LoggerWebhook
{
    private readonly HttpClient _http = new();
    private readonly string _webhookUrl;

    public LoggerWebhook(string webhookUrl)
    {
        _webhookUrl = webhookUrl;
    }

    public async Task SendAsync(string content, string? username = null)
    {
        try
        {
            var payload = new { content, username = username ?? "OtomAI" };
            var json = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            await _http.PostAsync(_webhookUrl, httpContent);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to send Discord webhook");
        }
    }

    public async Task SendEmbedAsync(string title, string description, int color = 0x2ECC71)
    {
        try
        {
            var payload = new
            {
                embeds = new[]
                {
                    new
                    {
                        title,
                        description,
                        color,
                        timestamp = DateTime.UtcNow.ToString("o"),
                    }
                }
            };
            var json = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            await _http.PostAsync(_webhookUrl, httpContent);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to send Discord webhook embed");
        }
    }
}
