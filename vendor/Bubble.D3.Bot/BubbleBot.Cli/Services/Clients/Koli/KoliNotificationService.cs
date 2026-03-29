using System.Diagnostics;
using BubbleBot.Cli.Services.Clients.Contracts;
using Serilog;
using Serilog.Core;

namespace BubbleBot.Cli.Services.Clients.Koli;

internal sealed class KoliNotificationService : IClientLogger
{
    private readonly BotKoliClientContext _context;
    private readonly Action _planifyDisconnect;

    public KoliNotificationService(BotKoliClientContext context, Action planifyDisconnect)
    {
        _context = context;
        _planifyDisconnect = planifyDisconnect;
    }

    public void LogKoli()
    {
        _ = Stopwatch.GetElapsedTime(_context.State.ConnectedAt);
        _planifyDisconnect();
    }

    private static bool ShouldSkipLogging()
    {
        return BotManager.NoLog;
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogWarning(string messageTemplate)
    {
        if (ShouldSkipLogging())
        {
            return;
        }

        Log.Warning("[" + _context.BotId + " - KOLI] : " + messageTemplate);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogWarning<T>(string messageTemplate, T propertyValue)
    {
        if (ShouldSkipLogging())
        {
            return;
        }

        Log.Warning("[" + _context.BotId + " - KOLI] : " + messageTemplate, propertyValue);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogWarning<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1)
    {
        Log.Warning("[" + _context.BotId + " - KOLI] : " + messageTemplate, propertyValue0, propertyValue1);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogWarning<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
    {
        if (ShouldSkipLogging())
        {
            return;
        }

        Log.Warning("[" + _context.BotId + " - KOLI] : " + messageTemplate,
                    propertyValue0,
                    propertyValue1,
                    propertyValue2);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogInfo(string messageTemplate)
    {
        if (ShouldSkipLogging())
        {
            return;
        }

        Log.Information("[" + _context.BotId + " - KOLI] : " + messageTemplate);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogInfo<T>(string messageTemplate, T propertyValue)
    {
        if (ShouldSkipLogging())
        {
            return;
        }

        Log.Information("[" + _context.BotId + " - KOLI] : " + messageTemplate, propertyValue);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogInfo<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1)
    {
        Log.Information("[" + _context.BotId + " - KOLI] : " + messageTemplate, propertyValue0, propertyValue1);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogInfo<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
    {
        if (ShouldSkipLogging())
        {
            return;
        }

        Log.Information("[" + _context.BotId + " - KOLI] : " + messageTemplate,
                        propertyValue0,
                        propertyValue1,
                        propertyValue2);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogError(Exception? exception, string messageTemplate)
    {
        if (ShouldSkipLogging())
        {
            return;
        }

        Log.Error(exception, "[" + _context.BotId + " - KOLI] : " + messageTemplate);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogError(string messageTemplate)
    {
        if (ShouldSkipLogging())
        {
            return;
        }

        Log.Error("[" + _context.BotId + " - KOLI] : " + messageTemplate);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogError<T>(string messageTemplate, T propertyValue)
    {
        if (ShouldSkipLogging())
        {
            return;
        }

        Log.Error("[" + _context.BotId + " - KOLI] : " + messageTemplate, propertyValue);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogError<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1)
    {
        Log.Error("[" + _context.BotId + " - KOLI] : " + messageTemplate, propertyValue0, propertyValue1);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogError<T0>(Exception? exception, string messageTemplate, T0 propertyValue0)
    {
        Log.Error(exception, "[" + _context.BotId + " - KOLI] : " + messageTemplate, propertyValue0);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogError<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
    {
        if (ShouldSkipLogging())
        {
            return;
        }

        Log.Error("[" + _context.BotId + " - KOLI] : " + messageTemplate,
                  propertyValue0,
                  propertyValue1,
                  propertyValue2);
    }
}
