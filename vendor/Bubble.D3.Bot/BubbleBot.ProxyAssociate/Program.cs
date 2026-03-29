// See https://aka.ms/new-console-template for more information

using System.Text;

var accounts = File.ReadAllLines("accounts.txt");

var serverAccounts = new Dictionary<string, List<string>>();
var proxyServer = new Dictionary<(string, string), List<string>>();
var proxyCount = new Dictionary<string, int>();

// Chaque server doit avoir maximum 2 proxy

foreach (var account in accounts)
{
    var server = "server1";
    var index = 0;
    // Count how much : there is in the string
    if (account.Count(x => x == ':') == 2)
    {
        server = account.Split(':')[0];
        index = 1;
    }

    var userPw = account.Split(':')[index] + ":" + account.Split(':')[index+1];

    if (!serverAccounts.TryGetValue(server, out var value))
    {
        value = [];
        serverAccounts.Add(server, value);
    }

    value.Add(userPw);
}

var proxies = File.ReadAllLines("proxies.txt");
foreach (var proxy in proxies)
{
    foreach (var server in serverAccounts)
    {
        proxyServer.Add((server.Key, proxy), []);
    }
}

// On doit associer chaque proxy à un compte
foreach (var server in serverAccounts)
{
    var serverId = server.Key;

    foreach (var account in server.Value)
    {    
        var proxy = GetFirstProxy(serverId, proxyServer, proxyCount);
        if (string.IsNullOrEmpty(proxy))
        {
            proxyCount = [];
            proxy = GetFirstProxy(serverId, proxyServer, proxyCount);
        }
        
        if(string.IsNullOrEmpty(proxy))
        {
            throw new Exception("No proxy available");
            break;
        }
        
        proxyServer[(serverId, proxy)].Add(account);
        proxyCount[proxy] = proxyCount.GetValueOrDefault(proxy, 0) + 1;
    }
}

var strBuilder = new StringBuilder();

// On print pour vérifier
var strList = new List<string>();

foreach (var proxy in proxyServer)
{
    Console.WriteLine($"Server: {proxy.Key.Item1} Proxy: {proxy.Key.Item2} Accounts: {string.Join(", ", proxy.Value)}");

    foreach (var account in proxy.Value)
    {
        strList.Add($"{proxy.Key.Item2} {account}");
    }
}

// we shuffle it
var rnd = new Random();
for (var i = 0; i < strList.Count; i++)
{
    var j = rnd.Next(i, strList.Count);
    (strList[i], strList[j]) = (strList[j], strList[i]);
}

foreach (var str in strList)
{
    strBuilder.AppendLine(str);
}


File.WriteAllText("output.txt", strBuilder.ToString());

static string GetFirstProxy(string serverId, Dictionary<(string, string), List<string>> proxyServers, Dictionary<string, int> proxyCount)
{
    // we iterate and find the first proxy
    foreach (var proxyServer in proxyServers)
    {
        if(proxyCount.TryGetValue(proxyServer.Key.Item2, out var count) && count >= 1)
        {
            continue;
        }
        
        if(proxyServer.Key.Item1 == serverId && (proxyServer.Value.Count < 2 || serverId == "server1"))
        {
            return proxyServer.Key.Item2;
        }
        
        
    }
    
    return "";
}

