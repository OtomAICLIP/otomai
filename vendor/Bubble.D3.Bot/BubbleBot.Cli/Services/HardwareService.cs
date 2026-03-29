using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BubbleBot.Cli.Services;


public static class HardwareService
{
    private static List<string> _networkInterfaces = new();
    
    public static List<string> GetNetworkInterfaces()
    {
        if(_networkInterfaces.Count > 0)
        {
            return _networkInterfaces;
        }
        
        var interfaces = NetworkInterface.GetAllNetworkInterfaces().OrderByDescending(x => x.GetIPv4Statistics().BytesReceived);
        var addresses = new List<string>();

        foreach (var networkInterface in interfaces)
        {
            if (networkInterface.NetworkInterfaceType != NetworkInterfaceType.Wireless80211 && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Ethernet)
            {
                continue;
            }

            var address = networkInterface.Name;
            
            var ipv4 = networkInterface.GetIPProperties()
                .UnicastAddresses
                .FirstOrDefault(x => x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            
            if (ipv4 != null)
            {
                address += $" ({ipv4.Address})";
            }
            
            if (string.IsNullOrEmpty(address))
            {
                continue;
            }
            
            addresses.Add(address);
        }

        _networkInterfaces = addresses;
        return addresses;
    }
    
    
    public static List<string> GenerateRandomHwidAddresses()
    {
        const int numbers = 1000;
        
        var hwids = new List<string>();
        if (hwids.Count >= numbers)
        {
            return hwids;
        }
        
        // we just create thing that looks like a mac address
        for (var i = 0; i < numbers; i++)
        {
            var hwid = "";
            for (var j = 0; j < 6; j++)
            {
                hwid += $"{(char) new Random().Next(48, 58)}{(char) new Random().Next(48, 58)}";
            }
            
            // generate a sha1
            var hash = SHA1.HashData(Encoding.UTF8.GetBytes(hwid));
            // convert to string
            hwid = BitConverter.ToString(hash).Replace("-", "").ToLower();
            
            hwids.Add(hwid);
        }
        
        return hwids;
    }
}