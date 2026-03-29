using System.Security.Cryptography;
using System.Text;

namespace OtomAI.Core.Crypto;

/// <summary>
/// Encryption utilities for keydata files and HAAPI key storage.
/// Mirrors Bubble.D3.Bot's CryptoHelper.
/// </summary>
public static class CryptoHelper
{
    public static byte[] Encrypt(byte[] data, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var ms = new MemoryStream();
        ms.Write(aes.IV);
        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            cs.Write(data);
        return ms.ToArray();
    }

    public static byte[] Decrypt(byte[] data, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = data[..16];
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
            cs.Write(data, 16, data.Length - 16);
        return ms.ToArray();
    }

    public static string ComputeHwid()
    {
        var machineId = Environment.MachineName + Environment.UserName;
        var hash = SHA512.HashData(Encoding.UTF8.GetBytes(machineId));
        return Convert.ToHexStringLower(hash);
    }
}
