// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Bubble.Core;
using Bubble.Shared;
using BubbleBot.Connect;
using Serilog;

var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
var accountsPath = Path.Combine(appDataPath, "zaap", "Settings");

var errorsLogin = new List<string>();

if(File.Exists("errorsLogin.txt"))
{
    errorsLogin = File.ReadAllLines("errorsLogin.txt").ToList();
}

var force = File.Exists("force.txt") || File.Exists("force");

if (!File.Exists(accountsPath))
{
    File.WriteAllText(accountsPath, JsonSerializer.Serialize(new SettingsFile()));
}

var json = File.ReadAllText(accountsPath);
var settings = JsonSerializer.Deserialize<SettingsFile>(json)!;

var proxies = File.ReadAllLines("proxies.txt");

var account = GetRequiredValue(args, 0, "BUBBLE_CONNECT_ACCOUNT", "account");

Console.WriteLine($"Tentative de connexion du compte \"{account}\"");

var proxy = Random.Shared.GetItems(proxies, 1)[0];
var ankamaService = new AnkamaService();

var userPw = account;

if (account.Contains(' '))
{
    proxy = account.Split(' ')[0];
    userPw = account.Split(' ')[1];
}

if (!ParseProxy(proxy, ankamaService))
    return;

// si le compte existe déjà dans le dossier settings on update le proxy et l'ignore
var oldAccountInfo = settings.UserAccounts.FirstOrDefault(x => x.Login == userPw.Split(':')[0]);
if (oldAccountInfo != null && !force)
{
    oldAccountInfo.Proxy = proxy;
    File.WriteAllText(accountsPath, JsonSerializer.Serialize(settings));
    Console.WriteLine($"Compte {oldAccountInfo.Login} mis à jour avec succès");
    return;
}


var keyDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                               "zaap",
                               "keydata");

// user:password
if (!userPw.Contains(':'))
{
    Console.WriteLine("Erreur: user:password invalide");
    return;
}

var user = userPw.Split(':')[0];
var password = userPw.Split(':')[1];

try
{
    var successAws = false;
    var retryCount = 0;
    while (!successAws)
    {
        ankamaService = new AnkamaService();

        proxy = Random.Shared.GetItems(proxies, 1)[0];
        if (!ParseProxy(proxy, ankamaService))
            return;

        ankamaService.Initialize();
        var awsBypass = new AwsBypassService();
        await awsBypass.Initialize(ankamaService.Proxy);

        var (codeVerifier, codeChallenge, state) = await ankamaService.LoginStep1();

        var awsToken = await awsBypass.Bypass(state);
        var (accessToken, refreshToken, retry) =
            await ankamaService.LoginStep2(user, password, codeVerifier, codeChallenge, state, awsToken);

        if (retry)
        {
            retryCount++;
            if (retryCount > 2)
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "BubbleBot.Connect.exe",
                    Arguments = userPw,
                    CreateNoWindow = true,
                    UseShellExecute = true,
                });

                return;
            }

            continue;
        }

        successAws = !retry;

        var accountInfo = await ankamaService.GetAccountInfo(accessToken);

        if (!Directory.Exists(keyDataPath))
        {
            Directory.CreateDirectory(keyDataPath);
        }

        var keyDataFile = Path.Combine(keyDataPath, $".keydata{accountInfo.Id}");

        await CryptoHelper.EncryptToFileWithUuid(keyDataFile,
                                                 new KeyData
                                                 {
                                                     Key = accessToken,
                                                     Provider = "ankama",
                                                     RefreshToken = refreshToken,
                                                     IsStayLoggedIn = true,
                                                     AccountId = accountInfo.Id,
                                                     Login = accountInfo.Login,
                                                     Certificate = new CertificateKeyData(),
                                                     RefreshDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                                                 });

        var oldAccount = settings.UserAccounts.FirstOrDefault(x => x.Id == accountInfo.Id);
        if (oldAccount != null)
        {
            settings.UserAccounts.Remove(oldAccount);
        }

        settings.UserAccounts.Add(new AnkamaAccount
        {
            Id = accountInfo.Id,
            Type = "ankama",
            Login = accountInfo.Login,
            IsGuest = false,
            NicknameWithTag = "",
            Security = [],
            Avatar = "",
            NeedRefresh = false,
            AddedDate = DateTime.Now,
            Proxy = proxy,
        });

        // we resave the settings file
        File.WriteAllText(accountsPath, JsonSerializer.Serialize(settings));

        Console.WriteLine($"Compte {accountInfo.Login} connecté avec succès");

        errorsLogin.Remove(accountInfo.Login);
    }
}
catch (Exception e)
{
    Console.WriteLine(e.Message);
    errorsLogin.Add(user);
    File.WriteAllLines("errorsLogin.txt", errorsLogin);
}

File.WriteAllLines("errorsLogin.txt", errorsLogin);

return;

static string GetRequiredValue(string[] args, int index, string environmentVariable, string label)
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

bool ParseProxy(string s, AnkamaService ankamaService1)
{
    if (string.IsNullOrEmpty(s)) 
        return false;
    
    var proxyIpPort = s.Split("://")[1].Split("@")[0];
    var proxyUserPw = s.Split("://")[1].Split("@")[1];

    if (!proxyIpPort.Contains(':'))
    {
        Console.WriteLine("Erreur: ip:port invalide");
        return true;
    }

    var proxyIp = proxyIpPort.Split(":")[0];
    var proxyPort = int.Parse(proxyIpPort.Split(":")[1]);

    if (!proxyUserPw.Contains(':'))
    {
        Console.WriteLine("Erreur: user:password invalide");
        return false;
    }

    var proxyUser = proxyUserPw.Split(":")[0];
    var proxyPassword = proxyUserPw.Split(":")[1];

    ankamaService1.Proxy = new WebProxy()
    {
        Address = new Uri($"socks5://{proxyIp}:{proxyPort}"),
        Credentials = new NetworkCredential(proxyUser, proxyPassword),
    };

    return true;
}
