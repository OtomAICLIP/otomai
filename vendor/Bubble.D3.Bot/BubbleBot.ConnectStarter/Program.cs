using System.Diagnostics;

if (!File.Exists("accounts.txt"))
{
    Console.WriteLine("Erreur: fichier accounts.txt introuvable");
    return;
}

var accounts = File.ReadAllLines("accounts.txt");
foreach (var account in accounts)
{
    var process = Process.Start(new ProcessStartInfo()
    {
        FileName = "BubbleBot.Connect.exe",
        ArgumentList = { account },
        UseShellExecute = true,
        RedirectStandardOutput = false,
        CreateNoWindow = true,
    });
    
    // wait for the process to exit
    process!.WaitForExit();
}