using System.Net;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using BubbleBot.ShieldDisable;
using Serilog;

public class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
                     .Enrich.FromLogContext()
                     .MinimumLevel.Debug()
                     .WriteTo.Console()
                     .CreateLogger();

        var resetUrl = GetRequiredValue(args, 0, "BUBBLE_SHIELD_DISABLE_RESET_URL", "reset URL");
        var proxy = GetRequiredValue(args, 1, "BUBBLE_SHIELD_DISABLE_PROXY", "proxy");
        
        using var client = new HttpClient();
        var response = await client.GetAsync(resetUrl);
        Thread.Sleep(5000);

        var email = GetRequiredValue(args, 2, "BUBBLE_SHIELD_DISABLE_EMAIL", "email");
        var password = GetRequiredValue(args, 3, "BUBBLE_SHIELD_DISABLE_PASSWORD", "password");
        
        var proxyUsername = proxy.Split(":")[2];
        var proxyPassword = proxy.Split(":")[3];
        var proxyHost = proxy.Split(":")[0];
        var proxyPort = int.Parse(proxy.Split(":")[1]);

        var proxyUri = new Uri($"socks5://{proxyHost}:{proxyPort}");

        var ankamaService = new AnkamaService
        {
            Proxy = new WebProxy(proxyUri)
            {
                Credentials = new NetworkCredential(proxyUsername, proxyPassword)
            }
        };
        ankamaService.Initialize();


        var awsBypass = new AwsBypassService();
        await awsBypass.Initialize(ankamaService.Proxy);

        var solde = await MailService.Instance.GetSolde();

        if (solde < 1)
        {
            Log.Error("Solde insuffisant {Solde}", solde);
            return;
        }

        Log.Information("Solde des mails: {Solde}", solde);

        var (addressId, address) = await MailService.Instance.ReorderMail(email);
        var retry = true;
        while (retry)
        {
            var success = await ankamaService.ConnectAnkama(address, password, awsBypass);

            retry = !success;
            
            Log.Information("Retrying connection");
            
            if(retry)
                Thread.Sleep(5000);
        }
        var receiveMail = await MailService.Instance.ReceiveMail(addressId);
        var code = receiveMail.Split("padding:10px\">")[1].Split("<")[0];

        (addressId, address) = await MailService.Instance.ReorderMail(address);
        await ankamaService.ConnectAnkamaShield(code);

        receiveMail = await MailService.Instance.ReceiveMail(addressId);
        code = receiveMail.Split("padding:10px\">")[1].Split("<")[0];
        if (!await ankamaService.DisableShield(code))
        {
            Console.WriteLine("Erreur lors de la désactivation du shield");
            return;
        }
        
        Console.WriteLine($"{address}:{password}");

        if (!File.Exists("accounts.txt"))
        {
            File.Create("accounts.txt").Close();
        }
        
        await File.AppendAllTextAsync("accounts.txt", $"{address}:{password}\n");
    }

    private static string GetRequiredValue(string[] args, int index, string environmentVariable, string label)
    {
        if (args.Length > index && !string.IsNullOrWhiteSpace(args[index]))
        {
            return args[index];
        }

        var value = Environment.GetEnvironmentVariable(environmentVariable);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new InvalidOperationException(
            $"Missing {label}. Provide argument {index + 1} or set the {environmentVariable} environment variable.");
    }
}
