using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Serilog;

namespace OtomAI.Protocol.Auth;

/// <summary>
/// Ankama OAuth2 PKCE authentication flow.
/// client_id=102, redirect_uri=zaap://login, User-Agent: Zaap 3.12.21
/// </summary>
public sealed class AnkamaAuth
{
    private const string AuthBase = "https://auth.ankama.com";
    private const string ClientId = "102";
    private const string RedirectUri = "zaap://login";
    private const string UserAgent = "Zaap 3.12.21";

    private readonly HttpClient _http;

    public AnkamaAuth(HttpClient? http = null)
    {
        _http = http ?? new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
    }

    /// <summary>
    /// Full OAuth2 PKCE login. Returns access_token + refresh_token.
    /// </summary>
    public async Task<AuthTokens> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        // 1. Generate PKCE pair
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = ComputeCodeChallenge(codeVerifier);

        // 2. GET login page to extract state
        var loginUrl = $"{AuthBase}/login/ankama?client_id={ClientId}" +
            $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
            $"&code_challenge={codeChallenge}&code_challenge_method=S256&response_type=code";

        var loginPage = await _http.GetStringAsync(loginUrl, ct);
        var state = ExtractState(loginPage);

        // 3. POST credentials
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["login"] = username,
            ["password"] = password,
            ["state"] = state,
        });

        var submitResponse = await _http.PostAsync($"{AuthBase}/login/ankama/form", form, ct);

        // 4. Extract auth code from redirect
        var redirectUrl = submitResponse.Headers.Location?.ToString()
            ?? throw new InvalidOperationException("No redirect after login");

        var authCode = ExtractAuthCode(redirectUrl);

        // 5. Exchange for tokens
        var tokenForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = authCode,
            ["client_id"] = ClientId,
            ["redirect_uri"] = RedirectUri,
            ["code_verifier"] = codeVerifier,
        });

        var tokenResponse = await _http.PostAsync($"{AuthBase}/token", tokenForm, ct);
        tokenResponse.EnsureSuccessStatusCode();
        var tokenJson = await tokenResponse.Content.ReadAsStringAsync(ct);
        var tokens = JsonSerializer.Deserialize<AuthTokens>(tokenJson)
            ?? throw new InvalidOperationException("Failed to parse token response");

        Log.Information("Authenticated as {Username}", username);
        return tokens;
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static string ComputeCodeChallenge(string verifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return Convert.ToBase64String(hash)
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static string ExtractState(string html)
    {
        // Look for state parameter in the login form
        const string marker = "name=\"state\" value=\"";
        int idx = html.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0) throw new InvalidOperationException("Cannot find state in login page");
        idx += marker.Length;
        int end = html.IndexOf('"', idx);
        return html[idx..end];
    }

    private static string ExtractAuthCode(string redirectUrl)
    {
        var uri = new Uri(redirectUrl);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        return query["code"] ?? throw new InvalidOperationException("No auth code in redirect");
    }
}

public sealed class AuthTokens
{
    public string AccessToken { get; init; } = "";
    public string RefreshToken { get; init; } = "";
    public int ExpiresIn { get; init; }
}
