using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bubble.Core;
using Bubble.Core.Services;
using Bubble.Shared.Ankama;

namespace BubbleBot.Cli.Services;

public class AnkamaService : Singleton<AnkamaService>
{
    public async Task<(string, string certificateId, string certificateHash, WebProxy? webProxy)> ConnectAsync(
        string username, string proxyIp, int proxyPort, string proxyUsername, string proxyPassword)
    {
        var account = AccountService.Instance.GetAccount(username);

        if (account == null)
            throw new Exception("Invalid credentials.");
        
        if(!account.ToLoad)
            throw new Exception("Account not to load.");

        var keyData = await GetCertificateAsync(account.Id);

        WebProxy? proxy = null;

        if (!string.IsNullOrEmpty(proxyIp))
        {
            proxy = new WebProxy()
            {
                Address = new Uri($"socks5://{proxyIp}:{proxyPort}"),
                Credentials = new NetworkCredential(proxyUsername, proxyPassword),
            };
        }
        
        var httpClientHandler = new HttpClientHandler()
        {
            Proxy = proxy,
            UseCookies = true,
            AllowAutoRedirect = true,
            CookieContainer = new CookieContainer(),
            UseDefaultCredentials = false,
            PreAuthenticate = true,
            AutomaticDecompression =
                DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
        };

        using var httpClient = new HttpClient(httpClientHandler);

        // get current timestamp and compare with keyData.RefreshDate
        // 2 jours après il faut refresh la key
        if (keyData.RefreshDate + 1_728e5 < DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
        {
            // keyData expired, refresh it
            var accountInfo = await RefreshApiKey(httpClient, keyData.Key, keyData.RefreshToken);

            keyData.Key = accountInfo.Key;
            keyData.RefreshToken = accountInfo.RefreshToken;
            keyData.RefreshDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var keyDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                           "zaap",
                                           "keydata");
            var keyDataFile = Path.Combine(keyDataPath, $".keydata{accountInfo.AccountId}");

            await CryptoHelper.EncryptToFileWithUuid(keyDataFile, keyData);
        }

        var certificateHash = string.Empty;
        var certificateId = string.Empty;

        if (keyData.Certificate != null && !string.IsNullOrEmpty(keyData.Certificate.EncodedCertificate))
        {
            certificateId = keyData.Certificate.Id.ToString();
            certificateHash = await CryptoHelper.GenerateHashFromCertif(keyData.Certificate);
        }

        return (await GetToken(httpClient, keyData.Key, certificateId, certificateHash, username), certificateId, certificateHash, proxy);
    }

    private async Task<KeyData> GetCertificateAsync(int accountId)
    {
        var keyDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                       "zaap",
                                       "keydata");

        if (!Directory.Exists(keyDataPath))
            throw new Exception("Keydata not found.");

        var keyDataFile = Path.Combine(keyDataPath, $".keydata{accountId}");

        if (!File.Exists(keyDataFile))
            throw new Exception("Keydata file not found.");

        var keyData = await CryptoHelper.DecryptFromFileWithUuid<KeyData>(keyDataFile);

        if (keyData == null)
            throw new Exception("Failed to decrypt keydata.");

        return keyData;
    }

    private async Task<AnkamaAccountInfoWithKey> RefreshApiKey(HttpClient httpClient, string apiKey,
                                                               string     refreshToken)
    {
        var req = new HttpRequestMessage()
        {
            RequestUri = new Uri($"https://haapi.ankama.com/json/Ankama/v5/Api/RefreshApiKey"),
            Method = HttpMethod.Post,
        };

        var dic = new Dictionary<string, string>
        {
            { "refresh_token", refreshToken },
            { "long_life_token", "true" }
        };

        req.Content = new FormUrlEncodedContent(dic);

        req.Headers.TryAddWithoutValidation("apiKey", apiKey);
        req.Headers.TryAddWithoutValidation("User-Agent", "Zaap 3.12.21");

        var res = await httpClient.SendAsync(req);

        var content = await res.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<AnkamaAccountInfoWithKey>(content)!;
    }

    private async Task<string> GetToken(HttpClient httpClient, string apiKey, string certificateId,
                                        string certificateHash, string email)
    {
        var req = new HttpRequestMessage()
        {
            RequestUri =
                new Uri(
                    $"https://haapi.ankama.com/json/Ankama/v5/Account/CreateToken?game=1&certificate_id={certificateId}&certificate_hash={certificateHash}"),
            Method = HttpMethod.Get,
        };

        req.Headers.TryAddWithoutValidation("apiKey", apiKey);

        var res = await httpClient.SendAsync(req);

        var dataRaw = await res.Content.ReadAsStringAsync();
        try
        {
            var data = JsonSerializer.Deserialize<CreateTokenResult>(dataRaw);

            if (data == null)
            {
                Console.WriteLine("Failed to get token");
                return string.Empty;
            }

            return data.Token;
        }
        catch (Exception e)
        {
            Console.WriteLine(dataRaw);
            try
            {
                if (File.Exists("accounts.txt"))
                {
                    var accountsList = await File.ReadAllLinesAsync("accounts.txt");
                    var account =
                        accountsList.FirstOrDefault(x => x.Contains(email, StringComparison.OrdinalIgnoreCase));

                    if (account != null)
                    {
                        Process.Start(new ProcessStartInfo()
                        {
                            FileName = "BubbleBot.Connect.exe",
                            Arguments = account,
                            UseShellExecute = true,
                            RedirectStandardOutput = false,
                            CreateNoWindow = true,
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return string.Empty;
        }
    }
}

public class CreateTokenResult
{
    [JsonPropertyName("token")] public required string Token { get; set; }
}