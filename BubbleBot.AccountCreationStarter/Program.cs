using System.Diagnostics;

record ProxyEntry(string ResetUrl, string Proxy);

public class Program
{
    private const string ConfigFileName = "account-creation-starter.txt";

    /// <summary>
    /// The entry point of the application.
    /// </summary>
    /// <param name="args">Command line arguments (not used).</param>
    public static void Main(string[] args)
    {
        var proxies = LoadProxyEntries();
        if (proxies.Count == 0)
        {
            Console.WriteLine(
                $"No proxy entries configured. Add '{ConfigFileName}' with one '<resetUrl> <proxy>' entry per line.");
            return;
        }

        // Create and start a dedicated thread for each proxy.
        foreach (var proxy in proxies)
        {
            var thread = new Thread(() => RunProxy(proxy))
            {
                IsBackground = true
            };
            thread.Start();
        }

        // Prevent the main thread from exiting.
        Thread.Sleep(Timeout.Infinite);
    }

    /// <summary>
    /// Continuously starts the account creation process using the provided proxy.
    /// </summary>
    /// <param name="proxy">The proxy information containing the reset URL and proxy string.</param>
    private static void RunProxy(ProxyEntry proxy)
    {
        while (true)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "BubbleBot.AccountCreation.exe",
                    UseShellExecute = true,
                    RedirectStandardOutput = false,
                    CreateNoWindow = true,
                };

                // Add the reset URL and proxy as command-line arguments.
                startInfo.ArgumentList.Add(proxy.ResetUrl);
                startInfo.ArgumentList.Add(proxy.Proxy);

                using Process process = Process.Start(startInfo)!;
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                // Log the error and continue the loop.
                Console.WriteLine($"Error for proxy '{proxy.Proxy}': {ex.Message}");
            }
        }
    }

    private static List<ProxyEntry> LoadProxyEntries()
    {
        if (!File.Exists(ConfigFileName))
        {
            return [];
        }

        return File.ReadAllLines(ConfigFileName)
                   .Select(line => line.Trim())
                   .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'))
                   .Select(ParseProxyEntry)
                   .ToList();
    }

    private static ProxyEntry ParseProxyEntry(string line)
    {
        var separatorIndex = line.IndexOf(' ');
        if (separatorIndex <= 0 || separatorIndex == line.Length - 1)
        {
            throw new InvalidOperationException(
                $"Invalid line in '{ConfigFileName}'. Expected '<resetUrl> <proxy>' but got '{line}'.");
        }

        var resetUrl = line[..separatorIndex].Trim();
        var proxy = line[(separatorIndex + 1)..].Trim();
        return new ProxyEntry(resetUrl, proxy);
    }
}
