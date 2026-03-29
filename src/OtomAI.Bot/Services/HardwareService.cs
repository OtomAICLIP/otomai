using System.Security.Cryptography;
using System.Text;

namespace OtomAI.Bot.Services;

/// <summary>
/// HWID generation for anti-bot fingerprinting.
/// Mirrors Bubble.D3.Bot's HardwareService.
/// </summary>
public static class HardwareService
{
    public static string GenerateHwid()
    {
        var data = $"{Environment.MachineName}|{Environment.UserName}|{Environment.ProcessorCount}";
        var hash = SHA512.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexStringLower(hash);
    }

    public static string GenerateHwid(string seed)
    {
        var hash = SHA512.HashData(Encoding.UTF8.GetBytes(seed));
        return Convert.ToHexStringLower(hash);
    }
}
