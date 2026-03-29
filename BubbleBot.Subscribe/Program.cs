// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Text.Json;
using BubbleBot.Subscribe;

var account = GetRequiredValue(args, 0, "BUBBLE_SUBSCRIBE_ACCOUNT", "account");

var type = args.Length > 1 ? args[1] : "paysafecard";

var subscribeCacheEntries = new List<SubscribeCacheEntry>();
var proxies = File.ReadAllLines("proxies.txt");


try
{
    Console.WriteLine(account);
    if (File.Exists("subscribeCache.json"))
    {
        subscribeCacheEntries =
            JsonSerializer.Deserialize<List<SubscribeCacheEntry>>(File.ReadAllText("subscribeCache.json")) ??
            new List<SubscribeCacheEntry>();
    }
}
catch (Exception e)
{
    Console.WriteLine(e.Message);
    Thread.Sleep(5000);
    return;
}


var retryCount = 0;
const int maxRetry = 5;

while (true)
{
    try
    {
        var ankamaService = new AnkamaService();
        var awsBypass = new AwsBypassService();

        var proxy = "";
        var userPw = account;

        if (account.Contains(' '))
        {
            proxy = account.Split(' ')[0];
            userPw = account.Split(' ')[1];
        }

        proxy = Random.Shared.GetItems(proxies, 1)[0];
        if (!ParseProxy(proxy, ankamaService))
            return;

        var login = userPw.Split(':')[0];

        var entry = subscribeCacheEntries.FirstOrDefault(x => x.Login == login);
        // On doit abonné ceux qui ne le sont pas a 2 jours de l'expiration
        if (entry != null && entry.Expiration > DateTime.Now.AddDays(2))
        {
            Console.WriteLine($"Compte {login} déjà abonné");
            return;
        }

        if (!string.IsNullOrEmpty(proxy))
        {
            var proxyIpPort = proxy.Split("://")[1].Split("@")[0];
            var proxyUserPw = proxy.Split("://")[1].Split("@")[1];

            if (!proxyIpPort.Contains(':'))
            {
                Console.WriteLine("Erreur: ip:port invalide");
                return;
            }

            var proxyIp = proxyIpPort.Split(":")[0];
            var proxyPort = int.Parse(proxyIpPort.Split(":")[1]);

            if (!proxyUserPw.Contains(':'))
            {
                Console.WriteLine("Erreur: user:password invalide");
                return;
            }

            var proxyUser = proxyUserPw.Split(":")[0];
            var proxyPassword = proxyUserPw.Split(":")[1];

            Console.WriteLine($"Proxy: {proxyIp}:{proxyPort}");

            ankamaService.Proxy = new WebProxy()
            {
                Address = new Uri($"socks5://{proxyIp}:{proxyPort}"),
                Credentials = new NetworkCredential(proxyUser, proxyPassword),
            };
        }

        await ankamaService.Initialize();
        await awsBypass.Initialize(ankamaService.Proxy);

        // user:password
        if (!userPw.Contains(':'))
        {
            Console.WriteLine("Erreur: user:password invalide");
            return;
        }


        var user = userPw.Split(':')[0];
        var password = userPw.Split(':')[1];

        await ankamaService.LoginStep0();
        var (state, codeChallenge) = await ankamaService.LoginStep1();

        var awsToken = await awsBypass.Bypass(state);
        var retry = await ankamaService.LoginStep2(user,
                                                   password,
                                                   codeChallenge,
                                                   state,
                                                   awsToken,
                                                   subscribeCacheEntries,
                                                   type);

        if (retry)
        {
            Console.WriteLine($"Compte {user} échec de connexion, réessai");
            Thread.Sleep(2000);

            if (retryCount > maxRetry)
            {
                break;
            }

            retryCount++;
            continue;
        }
        
        Console.WriteLine($"Compte {user} connecté avec succès");
        await ankamaService.Logout();

        Thread.Sleep(2000);
        break;
    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
        Thread.Sleep(5000);

        if (retryCount > maxRetry)
        {
            break;
        }

        retryCount++;
        continue;
    }

    break;
}

Thread.Sleep(1000);

// on save le cache
File.WriteAllText("subscribeCache.json", JsonSerializer.Serialize(subscribeCacheEntries));

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
