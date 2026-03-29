using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bubble.Shared.Ankama;

namespace BubbleBot.Connect;

public class AnkamaService
{
    public WebProxy Proxy { get; set; } = new();

    private HttpClient _client = new();

    private HttpClientHandler _handler = new();

    public void Initialize()
    {
        _handler = new HttpClientHandler
        {
            Proxy = Proxy,
            UseCookies = true,
            AllowAutoRedirect = true,
            CookieContainer = new CookieContainer(),
            UseDefaultCredentials = false,
            PreAuthenticate = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
        };
        _client = new HttpClient(_handler);
        
        _client.DefaultRequestHeaders.TryAddWithoutValidation("Accept",
                                                              "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
        _client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br, zstd");
        _client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "fr-FR,fr;q=0.9,en-US;q=0.8,en;q=0.7");
        _client.DefaultRequestHeaders.TryAddWithoutValidation("Cache-Control", "max-age=0");
        _client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36");
    }

    public async Task<(string codeVerifier, string codeChallenge, string state)> LoginStep1()
    {
        const string clientId = "102";
        
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = CreateCodeChallenge(codeVerifier);


        var url = $"https://auth.ankama.com/login/ankama?code_challenge={codeChallenge}&redirect_uri=zaap://login&client_id={clientId}&direct=true&origin_tracker=https://www.ankama-launcher.com/launcher";

        var state = await GetState(url);
        
        Console.WriteLine($"State: {state}");
        
        return (codeVerifier, codeChallenge, state);
    }

    public async Task<AnkamaAccountInfo> GetAccountInfo(string apiKey)
    {
        const string ankamaUrl = "https://haapi.ankama.com/json/Ankama/v5/Account/Account";
        _client.DefaultRequestHeaders.TryAddWithoutValidation("apikey", apiKey);
        
        var req = await _client.GetAsync(ankamaUrl);
        
        var response = await req.Content.ReadAsStringAsync();
        
        return JsonSerializer.Deserialize<AnkamaAccountInfo>(response)!;
    }

    private async Task<string> GetState(string url)
    {
        using var response = await _client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        if (content.Contains("name=\"state\" value=\""))
        {
            var stateTemp = content.Split("name=\"state\" value=\"")[1];
            var state = stateTemp.Split("\">")[0];
            
            return state;
        }
        
        return string.Empty;
    }


    private string GenerateCodeVerifier()
    {
        var length = Random.Shared.Next(43, 128);
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";

        var code = new char[length];
        for (var i = 0; i < length; i++)
        {
            code[i] = chars[Random.Shared.Next(chars.Length)];
        }

        return new string(code);
    }
    
    private string CreateCodeChallenge(string verifier)
    {
        // SHA256 hash
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(verifier));

        // Base64 URL encode
        var base64 = Convert.ToBase64String(hash)
                            .Replace('+', '-')
                            .Replace('/', '_')
                            .TrimEnd('=');

        return base64;
    }

    public async Task<(string accessToken, string refreshToken, bool retry)> LoginStep2(string user, string password, string codeVerifier, string codeChallenge, string state, string awsToken)
    {
        // we add a cookie
        _handler.CookieContainer.Add(new Cookie("aws-waf-token", awsToken, "/", "auth.ankama.com"));

        const string clientId = "102";
        var code = await GetCodeFromBrowser(state, user, password, awsToken);
        
        if (code == "cloudfront" || string.IsNullOrEmpty(code))
        {
            Console.WriteLine("Cloudfront error");
            return (string.Empty, string.Empty, true);
        }

        var (accessToken, refreshToken) = await GetTokens(code, codeVerifier, clientId);
        
        return (accessToken, refreshToken, false);
    }

    private async Task<(string accessToken, string refreshToken)> GetTokens(string loginCode, string codeVerifier, string clientId)
    {
        var dic = new Dictionary<string, string>
        {
            {"grant_type", "authorization_code"},
            {"code", loginCode},
            {"client_id", clientId},
            {"redirect_uri", "zaap://login"},
            {"code_verifier", codeVerifier},
        };
        
        const string ankamaUrl = "https://auth.ankama.com/token";
        var content = new FormUrlEncodedContent(dic);
        _client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Zaap 3.12.21");

        var req = await _client.PostAsync(ankamaUrl, content);
        
        var response = await req.Content.ReadAsStringAsync();
        
        var zaapLoginResponse = JsonSerializer.Deserialize<ZaapLoginResponse>(response);
        
        if(zaapLoginResponse == null)
            return (string.Empty, string.Empty);
        
        return (zaapLoginResponse.AccessToken, zaapLoginResponse.RefreshToken);
    }

    private async Task<string> GetCodeFromBrowser(string state, string login, string password, string awsCookie)
    {
        const string ankamaUrl = "https://auth.ankama.com/login/ankama/form";
        
        var data = new Dictionary<string, string>
        {
            {"state", state},
            {"login", login},
            {"password", password},
        };
        
        var content = new FormUrlEncodedContent(data);
        var req = await _client.PostAsync(ankamaUrl, content);
        var response = await req.Content.ReadAsStringAsync();

        if (response.Contains("zaap://login?code="))
        {
            var loginCodeTemp = response.Split("zaap://login?code=")[1];
            var loginCode = loginCodeTemp.Split("\"")[0];
            return loginCode;
        }
        else if(response.Contains("The request could not be satisfied"))
        {
            return "cloudfront";
        }
        else
        {
            return string.Empty;
        }
    }
}

public class ZaapLoginResponse
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; set; }
    
    [JsonPropertyName("refresh_token")]
    public required string RefreshToken { get; set; }
    
    [JsonPropertyName("exp")]
    public required string Exp { get; set; }
}

