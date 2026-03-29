using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BubbleBot.AccountCreation.Services;

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
            AutomaticDecompression =
                DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
        };
        _client = new HttpClient(_handler);

        _client.DefaultRequestHeaders.TryAddWithoutValidation("Accept",
                                                              "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
        _client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br, zstd");
        _client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "fr-FR,fr;q=0.9,en-US;q=0.8,en;q=0.7");
        _client.DefaultRequestHeaders.TryAddWithoutValidation("Cache-Control", "max-age=0");
        _client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                                                              "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36");
    }

    public async Task<(string codeVerifier, string codeChallenge, string state)> LoginStep1()
    {
        const string clientId = "102";

        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = CreateCodeChallenge(codeVerifier);


        var url =
            $"https://auth.ankama.com/login/ankama?code_challenge={codeChallenge}&redirect_uri=zaap://login&client_id={clientId}&direct=true&origin_tracker=https://www.ankama-launcher.com/launcher";

        var state = await GetState(url);

        Console.WriteLine($"State: {state}");

        return (codeVerifier, codeChallenge, state);
    }

    public async Task<(string content, string url)> RegisterStep1(string state)
    {
        var url =
            $"https://auth.ankama.com/register/ankama/form?origin_tracker=https://www.ankama-launcher.com/launcher&redirect_uri=https://auth.ankama.com/login-authorized?state%3{state}";

        var response = await _client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        return (content, url);
    }

    public async Task<string> RegisterStep2(string state,     string email,       string password, string lastName,
                                            string firstName, string birthDayDay, string birthDayMonth,
                                            string birthDayYear)
    {
        var url = $"https://auth.ankama.com/register/ankama/form-submit";

        var dic = new Dictionary<string, string>
        {
            { "state", state },
            { "email", email },
            { "password", password },
            { "lastname", lastName },
            { "firstname", firstName },
            { "birthday-day", birthDayDay },
            { "birthday-month", birthDayMonth },
            { "birthday-year", birthDayYear },
        };

        var data = new FormUrlEncodedContent(dic);
        var response = await _client.PostAsync(url, data);
        var content = await response.Content.ReadAsStringAsync();

        //Console.WriteLine(content);

        if (content.Contains("ankama-registration-birthday-year"))
        {
            return string.Empty;
        }

        var newState = content.Split("state\" value=\"")[1].Split("\"")[0];

        return newState;
    }

    public async Task ConfirmRegister(string state, string n1, string n2, string n3, string n4, string n5, string n6)
    {
        var url = $"https://auth.ankama.com/register/ankama/code";

        var dic = new Dictionary<string, string>
        {
            { "state", state },
            { "code", n1 + n2 + n3 + n4 + n5 + n6 },
            { "n1", n1 },
            { "n2", n2 },
            { "n3", n3 },
            { "n4", n4 },
            { "n5", n5 },
            { "n6", n6 },
        };

        var data = new FormUrlEncodedContent(dic);
        var response = await _client.PostAsync(url, data);
        var content = await response.Content.ReadAsStringAsync();

        //Console.WriteLine(content);
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

    public async Task<(string accessToken, string refreshToken, bool retry)> LoginStep2(
        string user, string password, string codeVerifier, string codeChallenge, string state, string awsToken)
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

    private async Task<(string accessToken, string refreshToken)> GetTokens(
        string loginCode, string codeVerifier, string clientId)
    {
        var dic = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", loginCode },
            { "client_id", clientId },
            { "redirect_uri", "zaap://login" },
            { "code_verifier", codeVerifier },
        };

        const string ankamaUrl = "https://auth.ankama.com/token";
        var content = new FormUrlEncodedContent(dic);
        _client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Zaap 3.12.21");

        var req = await _client.PostAsync(ankamaUrl, content);

        var response = await req.Content.ReadAsStringAsync();

        var zaapLoginResponse = JsonSerializer.Deserialize<ZaapLoginResponse>(response);

        if (zaapLoginResponse == null)
            return (string.Empty, string.Empty);

        return (zaapLoginResponse.AccessToken, zaapLoginResponse.RefreshToken);
    }

    private async Task<string> GetCodeFromBrowser2(string state, string login, string password)
    {
        const string ankamaUrl = "https://auth.ankama.com/login/ankama/form";

        var data = new Dictionary<string, string>
        {
            { "state", state },
            { "login", login },
            { "password", password },
        };

        var content = new FormUrlEncodedContent(data);
        var req = await _client.PostAsync(ankamaUrl, content);
        var response = await req.Content.ReadAsStringAsync();

        if (response.Contains("?code="))
        {
            var loginCodeTemp = response.Split("?code=")[1];
            var loginCode = loginCodeTemp.Split("\"")[0];
            return loginCode;
        }
        else if (response.Contains("The request could not be satisfied"))
        {
            return "cloudfront";
        }
        else
        {
            return string.Empty;
        }
    }

    public void SetCookie(string awsToken)
    {
        // remove a old aws-waf-token cookie if it exists
        var cookies = _handler.CookieContainer.GetCookies(new Uri("https://auth.ankama.com"));
        foreach (Cookie cookie in cookies)
        {
            if (cookie.Name == "aws-waf-token")
            {
                cookie.Expired = true;
            }
        }

        // add a new aws-waf-token cookie
        _handler.CookieContainer.Add(new Cookie("aws-waf-token", awsToken, "/", "auth.ankama.com"));
    }

    public async Task ConfirmEmail(string confirmEmail)
    {
        await _client.GetAsync(confirmEmail);
    }

    public async Task ConnectAnkama(string login, string password)
    {
        var (state, codeChallenge) =
            await GetState2(
                "https://account.ankama.com/webauth/authorize?from=https%3A%2F%2Faccount.ankama.com%2Ffr%2Fcompte%2Fsecuriser-mon-compte");

        var loginCode = await GetCodeFromBrowser2(state, login, password);
        Console.WriteLine($"Login code: {loginCode}");

        var ankamaUrl = "https://account.ankama.com/authorized?code=" + loginCode;
        _client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                                                              "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:134.0) Gecko/20100101 Firefox/134.0");

        var req = await _client.GetAsync(ankamaUrl);
        var content = await req.Content.ReadAsStringAsync();

        //Console.WriteLine("Connect Ankama response:");
        //Console.WriteLine(content);
        if (req.StatusCode == HttpStatusCode.NotFound)
        {
            Console.WriteLine("Error 404");
            return; // mean we retry
        }

        //await DisableShield();
    }

    public async Task ConnectAnkamaShield(string code)
    {
        var url = "https://account.ankama.com/fr/securite/double-authentification?authorisation-from=https%3A%2F%2Faccount.ankama.com%2Ffr%2Fsecurite%2Fmode-restreint%3Ff%3Dhttps%3A%2F%2Faccount.ankama.com%2Ffr&authorisation-type=add-shield";
        // post
        var dic = new Dictionary<string, string>
        {
            { "security_code", code },
        };
        
        var content = new FormUrlEncodedContent(dic);
        var req = await _client.PostAsync(url, content);
        
        var response = await req.Content.ReadAsStringAsync();
        
        //Console.WriteLine("Connect Ankama Shield response:");
        // Console.WriteLine(response);
        
        if (req.StatusCode == HttpStatusCode.NotFound)
        {
            Console.WriteLine("Error 404");
            return; // mean we retry
        }
        
        // we get the location last redirected url 
        var location = req.RequestMessage.RequestUri;
        // we do a simple post on it
        dic = new Dictionary<string, string>
        {
            { "submit", "Oui (Enregistrer l'appareil)" },
        };
        var req2 = await _client.PostAsync(location, new FormUrlEncodedContent(dic));
        var response2 = await req2.Content.ReadAsStringAsync();
        
        // Console.WriteLine("Connect Ankama Shield POST response:");
        // Console.WriteLine(response2);
        
        await DisableShield();
    }

    public async Task<bool> DisableShield(string code)
    {
        const string url = "https://account.ankama.com/fr/securite/ankama-shield/desactiver";
        var dic = new Dictionary<string, string>
        {
            { "postback", "1" },
            { "code", code },
        };

        var req = await _client.PostAsync(url, new FormUrlEncodedContent(dic));
        var response = await req.Content.ReadAsStringAsync();

        Console.WriteLine("Disable shield POST response:");
        Console.WriteLine(response);

        if (response.Contains("La sécurité par email a bien été désactivé sur votre"))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private async Task DisableShield()
    {
        const string url = "https://account.ankama.com/fr/securite/ankama-shield/desactiver";
        var req = await _client.GetAsync(url);
        var response = await req.Content.ReadAsStringAsync();

        // Console.WriteLine("Disable shield GET response:");
        // Console.WriteLine(response);
    }

    private async Task<string> GetCodeFromBrowser(string state, string login, string password, string codeChallenge)
    {
        const string ankamaUrl = "https://auth.ankama.com/login/ankama/form";

        var data = new Dictionary<string, string>
        {
            { "state", state },
            { "login", login },
            { "password", password },
        };

        var content = new FormUrlEncodedContent(data);
        var req = await _client.PostAsync(ankamaUrl, content);
        var response = await req.Content.ReadAsStringAsync();

        if (response.Contains("?code="))
        {
            var loginCodeTemp = response.Split("?code=")[1];
            var loginCode = loginCodeTemp.Split("\"")[0];
            return loginCode;
        }
        else if (response.Contains("The request could not be satisfied"))
        {
            return "cloudfront";
        }
        else
        {
            return string.Empty;
        }
    }

    private async Task<(string state, string codeChallenge)> GetState2(string url)
    {
        using var response = await _client.GetAsync(url);
        _client.DefaultRequestHeaders.TryAddWithoutValidation("Referer",
                                                              "https://www.dofus.com/fr/achat-kamas/achat-kamas");
        var content = await response.Content.ReadAsStringAsync();

        var codeChallenge = string.Empty;

        if (content.Contains("code_challenge="))
        {
            var codeChallTemp = content.Split("code_challenge=")[1];
            codeChallenge = codeChallTemp.Split("&amp;")[0];
        }

        var response2 = await _client.GetAsync(
            $"https://auth.ankama.com/login/ankama?direct=&origin_tracker=https://www.dofus.com/fr/achat-kamas&code_challenge={codeChallenge}&redirect_uri=https://account.ankama.com/authorized&client_id=0");
        content = await response2.Content.ReadAsStringAsync();


        if (content.Contains("name=\"state\" value=\""))
        {
            var stateTemp = content.Split("name=\"state\" value=\"")[1];
            var state = stateTemp.Split("\">")[0];

            return (state, codeChallenge);
        }

        return (string.Empty, string.Empty);
    }
}

public class ZaapLoginResponse
{
    [JsonPropertyName("access_token")] public required string AccessToken { get; set; }

    [JsonPropertyName("refresh_token")] public required string RefreshToken { get; set; }

    [JsonPropertyName("exp")] public required string Exp { get; set; }
}