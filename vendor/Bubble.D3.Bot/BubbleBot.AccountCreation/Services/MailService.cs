using System.Text.Json;
using System.Text.Json.Serialization;
using Bubble.Core.Services;
using Serilog;

namespace BubbleBot.AccountCreation.Services;

public class MailService : Singleton<MailService>
{
    public async Task<double> GetSolde()
    {
        var apiKey = GetApiKey();
        using var httpClient = new HttpClient();
        var response = await httpClient.GetStringAsync($"https://api.kopeechka.store/user-balance?token={apiKey}&cost=USD&type=json&api=2.0");
        
        var json = JsonSerializer.Deserialize<MailSolde>(response);
        
        return json?.Balance ?? 0;
    }

    public async Task<(string, string)> GetAddress()
    {
        var apiKey = GetApiKey();
        using var httpClient = new HttpClient();
        var response = await httpClient.GetStringAsync($"https://api.kopeechka.store/mailbox-get-email?site=ankama.com&mail_type=OUTLOOK&token={apiKey}&api=2.0");

        var json = JsonSerializer.Deserialize<MailResponse>(response);
        
        return (json?.Id ?? "", json?.Mail ?? "");
    }

    public async Task<(string Title, string Message)> GetMessage(string mailId)
    {
        var apiKey = GetApiKey();
        using var httpClient = new HttpClient();
        var response = await httpClient.GetStringAsync($"https://api.kopeechka.store/mailbox-get-message?id={mailId}&token={apiKey}&full=1&api=2.0");
        
        
        Log.Information("Mail content: {response}", response);
        var json = JsonSerializer.Deserialize<MailMessage>(response);
        
        return (json?.Value ?? "", json?.FullMessage ?? "");
    }   
    
    public async Task<(string, string)> ReorderMail(string mail)
    {
        var apiKey = GetApiKey();
        using var httpClient = new HttpClient();
        var response = await httpClient.GetStringAsync($"https://api.kopeechka.store/mailbox-reorder?site=ankama.com&email={mail}&token={apiKey}&api=2.0");
        
        var json = JsonSerializer.Deserialize<MailResponse>(response);
        
        return (json?.Id ?? "", json?.Mail ?? "");
    }

    private readonly List<string> _lastReceivedMails = new List<string>();
    public async Task<string> ReceiveMail(string mailId, string mailAddress)
    {
        var tries = 0;
        var title = "WAIT_LINK";
        while (title == "WAIT_LINK")
        {
            var (t, mailContent) = await Instance.GetMessage(mailId);
            title = t;

            if (title == "WAIT_LINK")
            {
                await Task.Delay(10000);
                
                tries++;
                if(tries > 20) 
                    return "";
                
                continue;
            }

            //Console.WriteLine(mailContent);
            if (_lastReceivedMails.Contains(mailContent))
            {
                Log.Warning("Mail already received, reordering mail");
                await ReorderMail(mailAddress);
                return await ReceiveMail(mailId, mailAddress);
            }
            
            _lastReceivedMails.Add(mailContent);

            return mailContent;
        }

        return "";
    }

    private static string GetApiKey()
    {
        var apiKey = Environment.GetEnvironmentVariable("BUBBLE_KOPEECHKA_API_KEY");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            return apiKey;
        }

        throw new InvalidOperationException(
            "Missing Kopeechka API key. Set the BUBBLE_KOPEECHKA_API_KEY environment variable.");
    }
}

public class MailSolde
{
    [JsonPropertyName("balance")]
    public double Balance { get; set; }
}

public class MailResponse
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";

    [JsonPropertyName("mail")] public string Mail { get; set; } = "";
}

public class MailMessage
{
    [JsonPropertyName("value")] public string Value { get; set; } = "";

    [JsonPropertyName("fullmessage")] public string FullMessage { get; set; } = "";
}
