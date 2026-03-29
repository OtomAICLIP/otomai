using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Bubble.Core;


public static class CryptoHelper
{
    private static string _machineId = string.Empty;
    
    private static string GetMachineIdCommand()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "ioreg -rd1 -c IOPlatformExpertDevice";
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var winDir = Environment.GetEnvironmentVariable("windir");
            var is32BitOn64 = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432") != null && 
                              RuntimeInformation.ProcessArchitecture == Architecture.X86;
            
            var basePath = is32BitOn64 
                ? $"{winDir}\\sysnative\\cmd.exe /c {winDir}\\System32"
                : $"{winDir}\\System32";
            
            return $"{basePath}\\REG.exe QUERY HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Cryptography /v MachineGuid";
        }
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "( cat /var/lib/dbus/machine-id /etc/machine-id 2> /dev/null || hostname ) | head -n 1 || :";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            return "kenv -q smbios.system.uuid || sysctl -n kern.hostuuid";

        throw new PlatformNotSupportedException($"Unsupported platform: {RuntimeInformation.OSDescription}");
    }

    private static string CleanMachineId(string output)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var parts = output.Split(new[] { "IOPlatformUUID" }, StringSplitOptions.None);
            return Regex.Replace(parts[1].Split('\n')[0], @"\=|\s+|\""", "").ToLower();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var parts = output.Split(new[] { "REG_SZ" }, StringSplitOptions.None);
            return Regex.Replace(parts[1], @"\r+|\n+|\s+", "").ToLower();
        }
        else // Linux and FreeBSD
        {
            return Regex.Replace(output, @"\r+|\n+|\s+", "").ToLower();
        }
    }

    private static string HashMachineId(string machineId)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(machineId));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }

    private static async Task<string> ExecuteCommandAsync(string command)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/bash",
            Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"/c {command}" : $"-c \"{command}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        return output;
    }

    private static async Task<string> GetMachineId()
    {
        try
        {
            if (!string.IsNullOrEmpty(_machineId))
                return _machineId;
            
            var command = GetMachineIdCommand();
            //Console.WriteLine($"Executing command: {command}");
            
            var output = await ExecuteCommandAsync(command);
            var cleanId = CleanMachineId(output);
            var hashedId = HashMachineId(cleanId);
            
            //Console.WriteLine($"Clean Machine ID: {cleanId}");
            //Console.WriteLine($"Hashed Machine ID: {hashedId}");
            
            _machineId = hashedId;
            
            return hashedId;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error getting machine ID: {ex}");
            return "#";
        }
    }
    
    public static string GetPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "win32";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "darwin";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "linux";
        else
            return "unknown";
    }
    
    public static string GetArch()
    {
        return RuntimeInformation.OSArchitecture.ToString().ToLower();
    }

    public static int GetCpuCount()
    {
        return Environment.ProcessorCount;
    }
    
    private static async Task<string> GetUUID()
    {
        var machineId = await GetMachineId();
        var components = new[]
        {
            GetPlatform(),
            GetArch(),
            machineId,
            GetCpuCount().ToString(),
            GetProcessorModel()
        };

        var uuid = string.Join(",", components);
        /*Console.WriteLine($"UUID Components: {JsonSerializer.Serialize(new
        {
            platform = RuntimeInformation.OSDescription,
            arch = RuntimeInformation.ProcessArchitecture,
            machineId,
            cpuCount = Environment.ProcessorCount,
            cpuModel = GetProcessorModel()
        })}");*/

        return uuid;
    }

    private static string GetProcessorModel()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("select * from Win32_Processor"))
                using (var collection = searcher.Get())
                {
                    return collection.Cast<ManagementObject>().FirstOrDefault()?["Name"]?.ToString() ?? "Unknown";
                }
            }
            catch
            {
                return "Unknown";
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            try
            {
                var cpuInfo = File.ReadAllLines("/proc/cpuinfo");
                return cpuInfo.FirstOrDefault(l => l.StartsWith("model name"))?.Split(':').Last().Trim() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
        return "Unknown";
    }

    private static byte[] CreateHashFromString(string str)
    {
        using (var md5 = MD5.Create())
        {
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(str));
            //Console.WriteLine($"Key hash (hex): {BitConverter.ToString(hash).Replace("-", "").ToLower()}");
            return hash;
        }
    }

    public static async Task<T> DecryptFromFileWithUuid<T>(string filePath)
    {
        var uuid = await GetUUID();
        return await DecryptFromFile<T>(filePath, uuid);
    }

    public static async Task<T> DecryptFromFile<T>(string filePath, string key)
    {
        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            return Decrypt<T>(content.Trim(), key);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"[1019 CRYPTO_HELPER] cannot decrypt from file {filePath}: {ex}");
            throw;
        }
    }
    
    public static async Task EncryptToFileWithUuid<T>(string filePath, T data)
    {
        var uuid = await GetUUID();
        await EncryptToFile(data, filePath, uuid);
    }
    
    public static async Task EncryptToFile<T>(T data, string filePath, string key)
    {
        try
        {
            var encrypted = Encrypt(data, key);
            await File.WriteAllTextAsync(filePath, encrypted);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"[1019 CRYPTO_HELPER] cannot encrypt to file {filePath}: {ex}");
            throw;
        }
    }

    public static string Encrypt<T>(T data, string key)
    {
        var keyHash = CreateHashFromString(key);
        // generate an iv
        var iv = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(iv);
        }
        
        using (var aes = Aes.Create())
        {
            aes.Key = keyHash;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (var encryptor = aes.CreateEncryptor())
            using (var msEncrypt = new MemoryStream())
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                var json = JsonSerializer.Serialize(data);
                swEncrypt.Write(json);
                swEncrypt.Flush();
                csEncrypt.FlushFinalBlock();
                return $"{Convert.ToHexStringLower(iv)}|{Convert.ToHexStringLower(msEncrypt.ToArray())}";
            }
        }
    }

    public static T Decrypt<T>(string encryptedData, string key)
    {
        var parts = encryptedData.Split('|');
        if (parts.Length != 2)
            throw new ArgumentException("Invalid encrypted data format. Expected IV|EncryptedData");

        //Console.WriteLine($"Decryption attempt with key: {key}");
        var iv = Convert.FromHexString(parts[0]);
        var encryptedText = Convert.FromHexString(parts[1]);
        var keyHash = CreateHashFromString(key);

        using (var aes = Aes.Create())
        {
            aes.Key = keyHash;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (var decryptor = aes.CreateDecryptor())
            using (var msDecrypt = new MemoryStream(encryptedText))
            using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            using (var srDecrypt = new StreamReader(csDecrypt))
            {
                var decrypted = srDecrypt.ReadToEnd();
                return JsonSerializer.Deserialize<T>(decrypted);
            }
        }
    }
    
    public static string GenerateHashFromCertif(CertificateKeyData e, string t, string n)
    {
        // Convert the key 'n' to a byte array
        var keyBytes = Encoding.UTF8.GetBytes(n);

        if (keyBytes.Length != 32)
        {
            throw new ArgumentException("Key must be 32 bytes (256 bits) for AES-256 decryption.");
        }

        // Initialize AES with the specified parameters
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = keyBytes;
        // No IV is used for ECB mode

        // Create the decryptor
        var decryptor = aes.CreateDecryptor();

        // Decode the base64-encoded certificate
        var encryptedData = Convert.FromBase64String(e.EncodedCertificate);

        // Decrypt the data
        var decryptedBytes = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);

        // Convert decrypted bytes to string (UTF-8 encoding)
        var decryptedString = Encoding.UTF8.GetString(decryptedBytes);

        // Concatenate 't' and decrypted string
        var concatenatedString = t + decryptedString;

        // Compute SHA-256 hash
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(concatenatedString));

        // Convert hash to hexadecimal string
        var hexHash = Convert.ToHexStringLower(hashBytes);
        return hexHash;
    }
    
    // New method converted from 'async generateHashFromCertif(e)'
    public static async Task<string> GenerateHashFromCertif(CertificateKeyData e)
    {
        var (hm1, hm2) = await CreateHmEncoders();
        return GenerateHashFromCertif(e, hm1, hm2);
    }

    // New method converted from 'async createHmEncoders()'
    public static async Task<(string hm1, string hm2)> CreateHmEncoders()
    {
        var t = new List<string>
        {
            GetArch(),
            GetPlatform()
        };

        try
        {
            t.Add(await GetMachineId());
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync("[AUTH] could not fetch MachineID: " + ex.Message);
        }

        t.Add(Environment.UserName);
        t.Add(GetOsVersion());
        t.Add(GetComputerRam());

        // Join the list into a single string
        var concatenatedString = string.Concat(t);

        // Compute SHA-256 hash of the concatenated string
        var n = CreateHashFromStringSha(concatenatedString);

        // Reverse the hash string
        var r = new string(n.Reverse().ToArray());

        return (hm1: n, hm2: r);
    }

    
    // Helper method to compute SHA-256 hash and return as hex string
    public static string CreateHashFromStringSha(string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);
        var hashBytes = SHA256.HashData(bytes);
        var hashHex = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        return hashHex[0..32]; // Return the first 32 characters
    }
    


    // Method to get the computer's RAM as a power of two in megabytes
    public static string GetComputerRam()
    {
        // Get total physical memory in bytes
        var totalMemoryBytes = GetTotalPhysicalMemory();

        // Convert bytes to megabytes
        var totalMemoryMb = totalMemoryBytes / 1024.0 / 1024.0;

        // Calculate the logarithm base 2 of the total memory in MB
        var logBase2 = Math.Log(totalMemoryMb) / Math.Log(2);

        // Round to the nearest integer
        var roundedLog = (int)Math.Round(logBase2);

        // Compute 2 raised to the power of the rounded logarithm
        var ramInMB = Math.Pow(2, roundedLog);

        // Convert to integer and return as string
        return ((int)ramInMB).ToString();
    }
        
    // Helper method to get total physical memory in bytes
    private static ulong GetTotalPhysicalMemory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // For Windows, use MEMORYSTATUSEX
            var memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                return memStatus.ullTotalPhys;
            }
            else
            {
                throw new Exception("Unable to get total physical memory.");
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // For Linux, read from /proc/meminfo
            try
            {
                var lines = File.ReadAllLines("/proc/meminfo");
                var memTotalLine = lines.FirstOrDefault(line => line.StartsWith("MemTotal:"));
                if (memTotalLine != null)
                {
                    var parts = memTotalLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 && ulong.TryParse(parts[1], out var memKb))
                    {
                        return memKb * 1024; // Convert kB to bytes
                    }
                }
            }
            catch
            {
                throw new Exception("Unable to read total physical memory from /proc/meminfo.");
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // For macOS, use sysctl
            try
            {
                var output = ExecuteCommand("sysctl hw.memsize");
                var parts = output.Split(':');
                if (parts.Length == 2 && ulong.TryParse(parts[1].Trim(), out var memBytes))
                {
                    return memBytes;
                }
            }
            catch
            {
                throw new Exception("Unable to get total physical memory using sysctl.");
            }
        }

        throw new PlatformNotSupportedException("Unsupported platform.");
    }
    
    // PInvoke for Windows
    [DllImport("kernel32.dll")]
    private extern static bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    // Method to execute shell commands
    private static string ExecuteCommand(string command)
    {
        var output = string.Empty;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows command execution
            var process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/C " + command;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();

            output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
        }
        else
        {
            // Unix-based command execution
            var process = new Process();
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = "-c \"" + command + "\"";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();

            output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
        }

        return output.Trim();
    }

    // Method to get the OS version as a float (major.minor)
    public static string GetOsVersion()
    {
        // Get OS version string
        var osVersionString = GetOsVersionString();

        // Split the version string into parts
        var versionParts = osVersionString.Split('.');

        if (versionParts.Length >= 2)
        {
            // Combine the major and minor versions
            var majorMinor = $"{versionParts[0]}.{versionParts[1]}";

            // Parse to float
            if (float.TryParse(majorMinor, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var version))
            {
                return version.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        throw new Exception("Unable to parse OS version.");
    }

    // Helper method to get the OS version string
    private static string GetOsVersionString()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var version = Environment.OSVersion.Version;
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Use uname -r
            try
            {
                var output = ExecuteCommand("uname -r");
                return output;
            }
            catch
            {
                throw new Exception("Unable to get OS version using uname.");
            }
        }

        throw new PlatformNotSupportedException("Unsupported platform.");
    }

}


public class KeyData
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;
    
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;
    
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;
    
    [JsonPropertyName("isStayLoggedIn")]
    public bool IsStayLoggedIn { get; set; }
    
    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }
    
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;
    
    [JsonPropertyName("certificate")]
    public CertificateKeyData? Certificate { get; set; } = new();
    
    [JsonPropertyName("refreshDate")]
    public long RefreshDate { get; set; }
}

public class CertificateKeyData
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("encodedCertificate")]
    public string EncodedCertificate { get; set; } = string.Empty;
    
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;
}