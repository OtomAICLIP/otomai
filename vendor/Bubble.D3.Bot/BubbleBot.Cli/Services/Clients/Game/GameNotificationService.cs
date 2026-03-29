using System.Diagnostics;
using System.Text;
using BubbleBot.Cli.Logging;
using BubbleBot.Cli.Services.Clients.Contracts;
using BubbleBot.Cli.Services.TreasureHunts;
using Discord;
using Discord.Net.Rest;
using Discord.Rest;
using Discord.Webhook;
using Serilog;
using Serilog.Core;

namespace BubbleBot.Cli.Services.Clients.Game;

internal sealed class GameNotificationService : IClientLogger
{
    private readonly BotGameClientContext _context;

    public GameNotificationService(BotGameClientContext context)
    {
        _context = context;
        InitializeFinalLogChannels();
    }

    public static string FormatKamas(long kamas, bool usePrefix)
    {
        try
        {
            var prefix = kamas > 0 && usePrefix
                ? "+"
                : string.Empty;

            return prefix + kamas.ToString("N0").Replace(",", " ");
        }
        catch
        {
            return kamas.ToString();
        }
    }

    public string[] GetChannel()
    {
        return GetConfiguredChannels(_context.Client.IsSpecial ? "private" : "public");
    }

    public string GetDiscordRole()
    {
        return _context.Client.ServerId switch
        {
            309 => "<@&1335019091882147851>",
            320 => "<@&1335019119463891097>",
            321 => "<@&1335019328940146699>",
            322 => "<@&1335019343712620606>",
            323 => "<@&1335019359063506964>",
            324 => "<@&1335019371155685416>",
            325 => "<@&1331849134193246229>",
            326 => "<@&1335019385802457148>",
            327 => "<@&1335019398167134250>",
            328 => "<@&1335019410200592467>",
            329 => "<@&1335019422276124812>",
            330 => "<@&1335019432963215462>",
            310 => "<@&1335019508607225916>",
            311 => "<@&1335019556212838400>",
            312 => "<@&1335019531474833408>",
            313 => "<@&1335019647539613858>",
            314 => "<@&1335020897505116200>",
            315 => "<@&1335019520670040165>",
            316 => "<@&1335019634423759050>",
            317 => "<@&1335019541234843739>",
            318 => "<@&1335019739290009691>",
            319 => "<@&1335020919176822825>",
            _ => string.Empty
        };
    }

    public void LogDiscordVente(string message, Color? color = null)
    {
        LogInfo(message);

        try
        {
            color ??= Color.Gold;
            _ = LoggerWebhook.LoggingVenteWebhook?.SendMessageAsync(embeds:
                                                                    [
                                                                        new EmbedBuilder()
                                                                            .WithAuthor(_context.Client.Info.Name + " - " +
                                                                                        _context.Client.ServerName)
                                                                            .WithDescription(message)
                                                                            .WithColor(color.Value)
                                                                            .WithFooter(
                                                                                $"Version: {BotClient.Version}")
                                                                            .WithCurrentTimestamp()
                                                                            .Build()
                                                                    ]);
        }
        catch (Exception exception)
        {
            LogError(exception, "Error while sending discord message");
        }
    }

    public void LogMpDiscord(string message)
    {
        if (_context.Client.ServerId == 329)
        {
            return;
        }

        LoggerWebhook.LogChat($"[{DateTime.Now:HH:mm:ss}] [{_context.Client.ServerName}] {_context.Client.Info.Name} - {message}");
    }

    public void LogDiscord(string message, bool force = false, Color? color = null)
    {
        LogInfo(message);
    }

    public void LogArchiDiscord(string message)
    {
        _ = LoggerWebhook.LoggingArchiWebhook?.SendMessageAsync(GetDiscordRole(),
                                                                embeds:
                                                                [
                                                                    new EmbedBuilder()
                                                                        .WithAuthor(_context.Client.ServerName)
                                                                        .WithDescription(message)
                                                                        .WithColor(Color.Green)
                                                                        .WithCurrentTimestamp()
                                                                        .WithFooter($"Version: {BotClient.Version}")
                                                                        .Build()
                                                                ]);
    }

    public void LogMaxDaily()
    {
        var elapsed = Stopwatch.GetElapsedTime(_context.Client.ConnectedAt);
        var elapsedStr = $"{elapsed.Hours} heures {elapsed.Minutes} minutes et {elapsed.Seconds} secondes";

        foreach (var channel in _context.FinalLogChannels)
        {
            try
            {
                channel?.SendMessageAsync(embeds:
                                          [
                                              new EmbedBuilder()
                                                  .WithAuthor(
                                                      $"{_context.Client.Info.Name} - {_context.Client.ServerName} - Lv{_context.Client.Info.Information?.Level}")
                                                  .WithTitle("\ufffd  Nombre de chasses limite atteinte")
                                                  .WithColor(Color.Parse("#56c2d1"))
                                                  .WithFields(
                                                      new EmbedFieldBuilder().WithName("Chasses réussis")
                                                                             .WithValue(_context.Client.TreasureHuntData.ChassesSuccess
                                                                                 .ToString())
                                                                             .WithIsInline(true),
                                                      new EmbedFieldBuilder().WithName("Chasses")
                                                                             .WithValue(_context.Client.TreasureHuntData.ChassesDones
                                                                                 .ToString())
                                                                             .WithIsInline(true),
                                                      new EmbedFieldBuilder().WithName("** **")
                                                                             .WithValue("** **")
                                                                             .WithIsInline(false),
                                                      new EmbedFieldBuilder().WithName("Kamas")
                                                                             .WithValue(CalculateKamas())
                                                                             .WithIsInline(true),
                                                      new EmbedFieldBuilder().WithName("Inventaire")
                                                                             .WithValue(CalculateObjectModified())
                                                                             .WithIsInline(true),
                                                      new EmbedFieldBuilder().WithName("Temps passé")
                                                                             .WithValue(elapsedStr)
                                                                             .WithIsInline(false))
                                                  .WithCurrentTimestamp()
                                                  .WithFooter($"Version: {BotClient.Version}")
                                                  .Build()
                                          ]);
            }
            catch (Exception exception)
            {
                LogError(exception, "Error while sending discord message");
            }
        }

        _context.Client.IsAtDailyLimit = true;
        _context.Client.NeedEmptyToBank = true;
        _context.Client.DoWork();
    }

    public void LogChassesFinished(long startedAt, bool giveUp, GiveUpReason? giveUpReason)
    {
        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var elapsedStr = $"{elapsed.Minutes} minutes et {elapsed.Seconds} secondes";

        var totalElapsed = Stopwatch.GetElapsedTime(_context.Client.ConnectedAt);
        var totalElapsedStr =
            $"{totalElapsed.Hours} heures {totalElapsed.Minutes} minutes et {totalElapsed.Seconds} secondes";

        var title = "\ud83d\uddfa\ufe0f  Chasse terminée";
        if (giveUp)
        {
            title += $" (Abandon {giveUpReason})";
            if (giveUpReason != GiveUpReason.HintNotFound)
            {
                title +=
                    $" [{_context.Client.Map?.Data.PosX},{_context.Client.Map?.Data.PosY}] -> [{_context.Client.TreasureHuntData.NextClue?.MapX},{_context.Client.TreasureHuntData.NextClue?.MapY}]";
            }
        }

        foreach (var channel in _context.FinalLogChannels)
        {
            try
            {
                _ = channel?.SendMessageAsync(embeds:
                                              [
                                                  new EmbedBuilder()
                                                      .WithAuthor(
                                                          $"{_context.Client.Info.Name} - {_context.Client.ServerName} - Lv{_context.Client.Info.Information?.Level}")
                                                      .WithTitle(title)
                                                      .WithColor(Color.Parse("#56c2d1"))
                                                      .WithFields(
                                                          new EmbedFieldBuilder().WithName("Chasses réussis")
                                                                                 .WithValue(_context.Client.TreasureHuntData
                                                                                     .ChassesSuccess.ToString())
                                                                                 .WithIsInline(true),
                                                          new EmbedFieldBuilder().WithName("Chasses")
                                                                                 .WithValue(_context.Client.TreasureHuntData
                                                                                     .ChassesDones.ToString())
                                                                                 .WithIsInline(true),
                                                          new EmbedFieldBuilder().WithName("** **")
                                                                                 .WithValue("** **")
                                                                                 .WithIsInline(false),
                                                          new EmbedFieldBuilder().WithName("Kamas")
                                                                                 .WithValue(CalculateKamas())
                                                                                 .WithIsInline(true),
                                                          new EmbedFieldBuilder().WithName("Inventaire")
                                                                                 .WithValue(CalculateObjectModified())
                                                                                 .WithIsInline(true),
                                                          new EmbedFieldBuilder().WithName("** **")
                                                                                 .WithValue("** **")
                                                                                 .WithIsInline(false),
                                                          new EmbedFieldBuilder().WithName("Temps passé sur cette chasse")
                                                                                 .WithValue(elapsedStr)
                                                                                 .WithIsInline(true),
                                                          new EmbedFieldBuilder().WithName("Temps passé")
                                                                                 .WithValue(totalElapsedStr)
                                                                                 .WithIsInline(true))
                                                      .WithCurrentTimestamp()
                                                      .WithFooter($"Version: {BotClient.Version}")
                                                      .Build()
                                              ]);
            }
            catch (Exception exception)
            {
                LogError(exception, "Error while sending discord message");
            }
        }

        BotManager.Instance.UpdateConsoleTitle();
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogWarning(string messageTemplate)
    {
        if (BotManager.NoLog)
        {
            return;
        }

        Log.Warning("[" + _context.Client.BotId + "] : " + messageTemplate);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogWarning<T>(string messageTemplate, T propertyValue)
    {
        if (BotManager.NoLog)
        {
            return;
        }

        Log.Warning("[" + _context.Client.BotId + "] : " + messageTemplate, propertyValue);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogWarning<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1)
    {
        Log.Warning("[" + _context.Client.BotId + "] : " + messageTemplate, propertyValue0, propertyValue1);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogWarning<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
    {
        if (BotManager.NoLog)
        {
            return;
        }

        Log.Warning("[" + _context.Client.BotId + "] : " + messageTemplate,
                    propertyValue0,
                    propertyValue1,
                    propertyValue2);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogInfo(string messageTemplate)
    {
        if (BotManager.NoLog)
        {
            return;
        }

        Log.Information("[" + _context.Client.BotId + "] : " + messageTemplate);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogInfo<T>(string messageTemplate, T propertyValue)
    {
        if (BotManager.NoLog)
        {
            return;
        }

        Log.Information("[" + _context.Client.BotId + "] : " + messageTemplate, propertyValue);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogInfo<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1)
    {
        Log.Information("[" + _context.Client.BotId + "] : " + messageTemplate, propertyValue0, propertyValue1);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogInfo<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
    {
        if (BotManager.NoLog)
        {
            return;
        }

        Log.Information("[" + _context.Client.BotId + "] : " + messageTemplate,
                        propertyValue0,
                        propertyValue1,
                        propertyValue2);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogError(Exception? exception, string messageTemplate)
    {
        if (BotManager.NoLog)
        {
            return;
        }

        Log.Error(exception, "[" + _context.Client.BotId + "] : " + messageTemplate);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogError(string messageTemplate)
    {
        if (BotManager.NoLog)
        {
            return;
        }

        Log.Error("[" + _context.Client.BotId + "] : " + messageTemplate);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogError<T>(string messageTemplate, T propertyValue)
    {
        if (BotManager.NoLog)
        {
            return;
        }

        Log.Error("[" + _context.Client.BotId + "] : " + messageTemplate, propertyValue);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogError<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1)
    {
        Log.Error("[" + _context.Client.BotId + "] : " + messageTemplate, propertyValue0, propertyValue1);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogError<T0>(Exception? exception, string messageTemplate, T0 propertyValue0)
    {
        Log.Error(exception, "[" + _context.Client.BotId + "] : " + messageTemplate, propertyValue0);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogError<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
    {
        if (BotManager.NoLog)
        {
            return;
        }

        Log.Error("[" + _context.Client.BotId + "] : " + messageTemplate,
                  propertyValue0,
                  propertyValue1,
                  propertyValue2);
    }

    private void InitializeFinalLogChannels()
    {
        try
        {
            if (LoggerWebhook.IsCustomWebhook)
            {
                _context.FinalLogChannels.Add(LoggerWebhook.LoggingArchiWebhook);
                return;
            }

            foreach (var channel in GetChannel())
            {
                if (!string.IsNullOrEmpty(channel))
                {
                    _context.FinalLogChannels.Add(new DiscordWebhookClient(channel,
                                                                           new DiscordRestConfig
                                                                           {
                                                                               RestClientProvider =
                                                                                   DefaultRestClientProvider.Create(
                                                                                       _context.Settings.WebProxy != null,
                                                                                       _context.Settings.WebProxy),
                                                                           }));
                }
                else
                {
                    _context.FinalLogChannels.Clear();
                    _context.FinalLogChannels.Add(LoggerWebhook.LoggingArchiWebhook);
                }
            }
        }
        catch (Exception exception)
        {
            LogError(exception, "Error while creating the webhook client");
        }
    }

    private string[] GetConfiguredChannels(string scope)
    {
        var path = Path.Combine("webhooks", scope, $"{_context.Client.ServerId}.txt");
        if (!File.Exists(path))
        {
            return [];
        }

        return File.ReadAllLines(path)
                   .Select(line => line.Trim())
                   .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'))
                   .ToArray();
    }

    private string CalculateKamas()
    {
        var builder = new StringBuilder();
        if (_context.Client.Inventory.Kamas != _context.Client.Inventory.KamasBase)
        {
            builder.AppendLine(
                $"{FormatKamas(_context.Client.Inventory.Kamas, false)} ({FormatKamas(_context.Client.Inventory.Kamas - _context.Client.Inventory.KamasBase, true)})");
        }
        else
        {
            builder.AppendLine($"{FormatKamas(_context.Client.Inventory.Kamas, false)}");
        }

        return builder.ToString();
    }

    private string CalculateObjectModified()
    {
        var builder = new StringBuilder();
        builder.Append("** **");
        foreach (var item in _context.Client.Inventory.Items)
        {
            if (item.Value.BaseQuantity != item.Value.Item.Item.Quantity || item.Value.Item.Item.Gid == 15263)
            {
                if (item.Value.Template?.Name.Contains("Rabmablague") == true)
                {
                    continue;
                }

                if (item.Value.Item.Item.Gid == 15263)
                {
                    builder.AppendLine(
                        $"{item.Value.Template?.Name} : {FormatKamas((long)(item.Value.Item.Item.Quantity * 0.7d), false)}");
                }
                else
                {
                    builder.AppendLine(
                        $"{item.Value.Template?.Name} : {FormatKamas(item.Value.Item.Item.Quantity, false)} ({FormatKamas(item.Value.Item.Item.Quantity - item.Value.BaseQuantity, true)})");
                }
            }
        }

        return builder.ToString();
    }
}
