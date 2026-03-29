using Serilog.Core;

namespace BubbleBot.Cli.Services.Clients.Contracts;

public interface IClientLogger
{
    [MessageTemplateFormatMethod("messageTemplate")]
    void LogWarning(string messageTemplate);

    [MessageTemplateFormatMethod("messageTemplate")]
    void LogWarning<T>(string messageTemplate, T propertyValue);

    [MessageTemplateFormatMethod("messageTemplate")]
    void LogWarning<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1);

    [MessageTemplateFormatMethod("messageTemplate")]
    void LogWarning<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2);

    [MessageTemplateFormatMethod("messageTemplate")]
    void LogInfo(string messageTemplate);

    [MessageTemplateFormatMethod("messageTemplate")]
    void LogInfo<T>(string messageTemplate, T propertyValue);

    [MessageTemplateFormatMethod("messageTemplate")]
    void LogInfo<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1);

    [MessageTemplateFormatMethod("messageTemplate")]
    void LogInfo<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2);

    [MessageTemplateFormatMethod("messageTemplate")]
    void LogError(Exception? exception, string messageTemplate);

    [MessageTemplateFormatMethod("messageTemplate")]
    void LogError(string messageTemplate);

    [MessageTemplateFormatMethod("messageTemplate")]
    void LogError<T>(string messageTemplate, T propertyValue);

    [MessageTemplateFormatMethod("messageTemplate")]
    void LogError<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1);

    [MessageTemplateFormatMethod("messageTemplate")]
    void LogError<T0>(Exception? exception, string messageTemplate, T0 propertyValue0);

    [MessageTemplateFormatMethod("messageTemplate")]
    void LogError<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2);
}
