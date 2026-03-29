using System.Diagnostics;
using System.Text.Json;
using BubbleBot.Split;

Console.WriteLine("Hello, World!");

AccountService.Instance.LoadAccounts();

var accounts = AccountService.Instance.GetAccounts();

// On divise par groupe de 20, il faut aussi prendre en compte si il y a moins de 20 comptes

var groups = new List<List<SaharachAccount>>();
foreach (var account in accounts)
{
    if(!account.ToLoad)
        continue;
    
    if (groups.Count == 0 || groups.Last().Count == 25)
    {
        groups.Add([]);
    }
    
    groups.Last().Add(account);
}

var i = 0;
foreach (var group in groups)
{
    // on créé un dossier pour chaque groupe avec comme nom l'index
    var path = Path.Combine(Environment.CurrentDirectory, i.ToString());
    Directory.CreateDirectory(path);
    
    // on écrit les comptes dans un fichier
    var json = JsonSerializer.Serialize(group);
    File.WriteAllText(Path.Combine(path, "accounts.json"), json);
    
    i++;
    
    // On copie le dossier "Data"
    var dataPath = Path.Combine(Environment.CurrentDirectory, "Data");
    if (Directory.Exists(dataPath))
    {
        var destPath = Path.Combine(path, "Data");
        Directory.CreateDirectory(destPath);
        
        foreach (var file in Directory.GetFiles(dataPath))
        {
            File.Copy(file, Path.Combine(destPath, Path.GetFileName(file)), true);
        }
    }
    
    // On copie le fichier classdata.tpk
    var classDataPath = Path.Combine(Environment.CurrentDirectory, "classdata.tpk");
    if (File.Exists(classDataPath))
    {
        File.Copy(classDataPath, Path.Combine(path, "classdata.tpk"), true);
    }
    
    // On copie le fichier accounts.txt
    var accountsPath = Path.Combine(Environment.CurrentDirectory, "accounts.txt");
    if (File.Exists(accountsPath))
    {
        File.Copy(accountsPath, Path.Combine(path, "accounts.txt"), true);
    }
    
    // On copie le fichier accounts.txt
    var proxiesPath = Path.Combine(Environment.CurrentDirectory, "proxies.txt");
    if (File.Exists(proxiesPath))
    {
        File.Copy(proxiesPath, Path.Combine(path, "proxies.txt"), true);
    }

    // On copie le fichier BubbleBot.Cli.exe
    var exePath = Path.Combine(Environment.CurrentDirectory, "BubbleBot.Cli.exe");
    if (File.Exists(exePath))
    {
        File.Copy(exePath, Path.Combine(path, "BubbleBot.Cli.exe"), true);
    }
    
    // On copie le fichier BubbleBot.Connect.exe
    var exeConnectPath = Path.Combine(Environment.CurrentDirectory, "BubbleBot.Connect.exe");
    if (File.Exists(exeConnectPath))
    {
        File.Copy(exeConnectPath, Path.Combine(path, "BubbleBot.Connect.exe"), true);
    }
    // On copie le fichier autoopen et special
    var autoopenPath = Path.Combine(Environment.CurrentDirectory, "autoopen");
    if (File.Exists(autoopenPath))
    {
        File.Copy(autoopenPath, Path.Combine(path, "autoopen"), true);
    }
    
    var specialPath = Path.Combine(Environment.CurrentDirectory, "special");
    if (File.Exists(specialPath))
    {
        File.Copy(specialPath, Path.Combine(path, "special"), true);
    }
    
    // le fichier aspnetcorev2_inprocess.dll
    var aspNetCorePath = Path.Combine(Environment.CurrentDirectory, "aspnetcorev2_inprocess.dll");
    if (File.Exists(aspNetCorePath))
    {
        File.Copy(aspNetCorePath, Path.Combine(path, "aspnetcorev2_inprocess.dll"), true);
    }
    
    // le fichier emptytobank
    var emptyToBankPath = Path.Combine(Environment.CurrentDirectory, "emptytobank");
    if (File.Exists(emptyToBankPath))
    {
        File.Copy(emptyToBankPath, Path.Combine(path, "emptytobank"), true);
    }
}


// maintenant pareil mais on start les bots, donc le fichier BubbleBot.Cli.exe dans chaque dossier
i = 0;
foreach (var group in groups)
{
    if(group.Count == 0 || !group.Any(x => x.ToLoad))
        continue;
    
    var path = Path.Combine(Environment.CurrentDirectory, i.ToString());
    var exePath = Path.Combine(Environment.CurrentDirectory, i.ToString(), "BubbleBot.Cli.exe");
    
    var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = path,
            UseShellExecute = true,
        }
    };
    
    process.Start();
    
    i++;
}
