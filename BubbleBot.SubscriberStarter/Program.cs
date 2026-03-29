
using System.Diagnostics;

if (!File.Exists("accounts.txt"))
{
    Console.WriteLine("Erreur: fichier accounts.txt introuvable");
    return;
}

var type = "kamas";

if (File.Exists("paysafecard.txt"))
{
    type = "paysafecard";
}

var accounts = File.ReadAllLines("accounts.txt");
foreach (var account in accounts)
{
    var process = Process.Start(new ProcessStartInfo()
    {
        FileName = "BubbleBot.Subscribe.exe",
        ArgumentList = { account, type },
        UseShellExecute = true,
        RedirectStandardOutput = false,
        CreateNoWindow = true,
    });
    
    // wait for the process to exit
    process!.WaitForExit();
}