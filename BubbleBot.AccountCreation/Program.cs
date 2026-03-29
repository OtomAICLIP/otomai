using System.Net;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using BubbleBot.AccountCreation.Services;
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

        var resetUrl = GetRequiredValue(args, 0, "BUBBLE_ACCOUNT_CREATION_RESET_URL", "reset URL");
        var proxy = GetRequiredValue(args, 1, "BUBBLE_ACCOUNT_CREATION_PROXY", "proxy");
        
        using var client = new HttpClient();
        var response = await client.GetAsync(resetUrl);
        
        Thread.Sleep(10000);

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

        var (codeVerifier, challenge, state) = await ankamaService.LoginStep1();
        var awsBypass = new AwsBypassService();
        await awsBypass.Initialize(ankamaService.Proxy);
        var awsToken = await awsBypass.Bypass(state);

        ankamaService.SetCookie(awsToken);
        var (input, url) = await ankamaService.RegisterStep1(state);


        // Regex that matches the entire object and extracts key, iv, and context.
        var pattern =
            @"window\.gokuProps\s*=\s*\{\s*""key""\s*:\s*""(?<key>[^""]+)"",\s*""iv""\s*:\s*""(?<iv>[^""]+)"",\s*""context""\s*:\s*""(?<context>[^""]+)""\s*\}";
        var regex = new Regex(pattern);

        var match = regex.Match(input);
        if (match.Success)
        {
            var urlPattern = @"<script\s+src\s*=\s*""(?<url>[^""]+)""";
            var urlRegex = new Regex(urlPattern);

            var urlMatch = urlRegex.Match(input);
            if (urlMatch.Success)
            {
                var urlCaptcha = urlMatch.Groups["url"].Value;
                //Console.WriteLine("URL du captcha: {0}", urlCaptcha);

                var key = match.Groups["key"].Value;
                var iv = match.Groups["iv"].Value;
                var context = match.Groups["context"].Value;

                var cookie = await CaptchaService.Instance.GetAmazonWaf(
                    url,
                    key,
                    iv,
                    context,
                    urlCaptcha);

                ankamaService.SetCookie(cookie);
            }
            else
            {
                Console.WriteLine("Aucune correspondance pour l'URL.");
            }
        }
        else
        {
            Console.WriteLine("No match found.");
        }

        var solde = await MailService.Instance.GetSolde();

        if (solde < 1)
        {
            Log.Error("Solde insuffisant {Solde}", solde);
            return;
        }

        Log.Information("Solde des mails: {Solde}", solde);

        var (addressId, address) = await MailService.Instance.GetAddress();

        if (string.IsNullOrEmpty(addressId))
        {
            Log.Error("Aucune adresse mail disponible");
            return;
        }

        // random password from 8 to 12 characters
        var password = Guid.NewGuid().ToString().Replace("-", "")[..8];
        var firstNames = new[]
        {
            "Alphonse", "Benoit", "Cyril", "David", "Emmanuel", "Franck", "Gilles", "Hugo", "Igor", "Jules", "Karl", "Lucas",
            "Maurice", "Nicolas", "Olivier", "Pierre", "Quentin", "Romain", "Sylvain", "Thibault", "Ulysse", "Victor",
            "William", "Xavier", "Yann", "Zacharie"
        };
        var lastNames = new[]
        {
            "Teston", "Dupont", "Durand", "Martin", "Bernard", "Dubois", "Thomas", "Robert", "Richard", "Petit", "Durand",
            "Leroy", "Moreau", "Simon", "Laurent", "Lefevre", "Michel", "Garcia", "David", "Bertrand", "Roux", "Vincent",
            "Fournier", "Morel", "Girard", "Andre"
        };

        var random = new Random();
        var firstName = firstNames[random.Next(firstNames.Length)];
        var lastName = lastNames[random.Next(lastNames.Length)];

        var birthDay = random.Next(1, 28).ToString("00");
        var birthMonth = random.Next(1, 12).ToString("00");
        var birthYear = random.Next(1950, 2000).ToString();

        Log.Information("Adresse mail disponible: {Address}", address);

        var (content, _) = await ankamaService.RegisterStep1(state);
        state = content.Split("state\" value=\"")[1].Split("\"")[0];

        state = await ankamaService.RegisterStep2(state,
                                                  address,
                                                  password,
                                                  firstName,
                                                  lastName,
                                                  birthDay,
                                                  birthMonth,
                                                  birthYear);

        if (!string.IsNullOrEmpty(state))
        {
            Log.Information("Compte créé: {Address}", address);
        }
        else
        {
            Log.Error("Erreur lors de la création du compte: {Address}", address);
            return;
        }

        var receiveMail = await MailService.Instance.ReceiveMail(addressId, address);


        var pattern2 = @"<div\s+[^>]*>(\d+)<\/div>";
        var matches = Regex.Matches(receiveMail, pattern2);

        //Console.WriteLine(receiveMail);

        var codes = new List<string>();

        foreach (Match x in matches)
        {
            //Console.WriteLine(match.Groups[1].Value);
            codes.Add(x.Groups[1].Value);
        }

        await ankamaService.ConfirmRegister(state, codes[0], codes[1], codes[2], codes[3], codes[4], codes[5]);

        Log.Information("Email confirmed.");
        //Console.WriteLine("Email confirmed.");
        //Console.WriteLine($"{address}:{password}");

        Thread.Sleep(10000);
        Log.Information("Re-ordering mail for connect ankama");
        (addressId, address) = await MailService.Instance.ReorderMail(address);
        Thread.Sleep(10000);
        
        Log.Information("Connecting to Ankama");
        await ankamaService.ConnectAnkama(address, password);
        receiveMail = await MailService.Instance.ReceiveMail(addressId, address);
        if (receiveMail.Contains("pour valider la"))
        {
            Log.Error("Erreur lors de la connexion");
            return;
        }
        var code = receiveMail.Split("padding:10px\">")[1].Split("<")[0];
        Log.Information("Received code: {Code}", code);
        Thread.Sleep(10000);
        Log.Information("Re-ordering mail for shield disable");
        (addressId, address) = await MailService.Instance.ReorderMail(address);    
        Thread.Sleep(10000);
        Log.Information("Connecting to Ankama Shield");
        await ankamaService.ConnectAnkamaShield(code);


        receiveMail = await MailService.Instance.ReceiveMail(addressId, address);

        code = receiveMail.Split("padding:10px\">")[1].Split("<")[0];
        Log.Information("Received code: {Code} for shield disable", code);
        if (!await ankamaService.DisableShield(code))
        {
            Log.Error("Erreur lors de la désactivation du shield");
            return;
        }
        
        Log.Information($"{address}:{password}");

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

class CapmonsterRequest
{
    [JsonPropertyName("clientKey")] public string ClientKey { get; set; }

    [JsonPropertyName("task")] public AwsCaptchaRequest Task { get; set; }
}

public class AwsCaptchaRequest
{
    [JsonPropertyName("type")] public string Type { get; set; }

    [JsonPropertyName("websiteURL")] public string WebsiteURL { get; set; }

    [JsonPropertyName("awsKey")] public string AwsKey { get; set; }

    [JsonPropertyName("awsIv")] public string AwsIv { get; set; }

    [JsonPropertyName("awsContext")] public string AwsContext { get; set; }

    [JsonPropertyName("awsChallengeJS")] public string AwsChallengeJS { get; set; }
}

public class CapMonsterReqResult
{
    [JsonPropertyName("taskId")] public string TaskId { get; set; }

    [JsonPropertyName("errorId")] public int ErrorId { get; set; }

    [JsonPropertyName("errorCode")] public string ErrorCode { get; set; }

    [JsonPropertyName("errorDescription")] public string ErrorDescription { get; set; }
}

public class CapMonsterGetTaskRequest
{
    [JsonPropertyName("taskId")] public string TaskId { get; set; }

    [JsonPropertyName("clientKey")] public string ClientKey { get; set; }
}

public class CapMonsterGetTaskResult
{
    [JsonPropertyName("status")] public string Status { get; set; }

    [JsonPropertyName("solution")] public CapMonsterGetTaskSolution Solution { get; set; }
}

public class CapMonsterGetTaskSolution
{
    [JsonPropertyName("cookie")] public string Cookie { get; set; }
}
