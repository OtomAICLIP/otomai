using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using NicknameGenerator;

namespace BubbleBot.Subscribe;

public class AnkamaService
{
    public WebProxy? Proxy { get; set; }

    private HttpClient _client = new();

    private HttpClientHandler _handler = new();

    public async Task Initialize()
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
        try
        {
            var ip = await _client.GetStringAsync("https://api.ipify.org/?format=text");
            Console.WriteLine($"IP: {ip}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public async Task LoginStep0()
    {
        var url = "https://www.dofus.com/fr/achat-kamas/achat-kamas";

        await _client.GetAsync(url);
    }

    public async Task<(string state, string codeChallenge)> LoginStep1()
    {
        var url =
            $"https://account.ankama.com/webauth/authorize?from=https%3A%2F%2Fwww.dofus.com%2Ffr%2Fachat-kamas";

        var (state, codeChallenge) = await GetState(url);

        Console.WriteLine($"State: {state}");

        return (state, codeChallenge);
    }


    private async Task<(string state, string codeChallenge)> GetState(string url)
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

    public async Task<bool> LoginStep2(string                    user,
                                       string                    password,
                                       string                    codeChallenge,
                                       string                    state,
                                       string                    awsToken,
                                       List<SubscribeCacheEntry> subscribeCacheEntries,
                                       string                    subType)
    {
        // we add a cookie
        _handler.CookieContainer.Add(new Cookie("aws-waf-token", awsToken, "/", "auth.ankama.com"));

        var code = await GetCodeFromBrowser(state, user, password, codeChallenge);

        if (string.IsNullOrEmpty(code))
        {
            Console.WriteLine("Compte incorrect ou banned");
            return false;
        }
        
        if (code == "cloudfront")
        {
            Console.WriteLine("Cloudfront error");
            return true;
        }

        return await Subscribe(user, code, subscribeCacheEntries, subType);
    }

    public async Task Logout()
    {
        const string url = "https://account.ankama.com/sso?action=logout&from=https%3A%2F%2Fwww.dofus.com%2Ffr";

        await _client.GetAsync(url);

        _client.Dispose();
    }

    private static int _retryCount = 0;

    private async Task<bool> Subscribe(string                    user,
                                       string                    loginCode,
                                       List<SubscribeCacheEntry> subscribeCacheEntries,
                                       string                    subType)
    {
        var ankamaUrl = "https://account.ankama.com/authorized?code=" + loginCode;
        _client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                                                              "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:134.0) Gecko/20100101 Firefox/134.0");

        var req = await _client.GetAsync(ankamaUrl);
        var content = await req.Content.ReadAsStringAsync();

        if (req.StatusCode == HttpStatusCode.NotFound)
        {
            return true; // mean we retry
        }

        var oldEntry = subscribeCacheEntries.FirstOrDefault(x => x.Login == user);
        if (oldEntry != null)
        {
            subscribeCacheEntries.Remove(oldEntry);
        }

        await AddNicknameIfNeeded(content);

        if (!IsSubscribed(content, subType, out var expiration))
        {
            if (subType == "paysafecard")
            {
                await SubWithPaysafecard();
            }
            else if (!await BuyOgrines(content))
            {
                if (_retryCount > 1)
                    return true;

                _retryCount++;
                await LoginStep1();
                // on retry juste une fois
            }
        }
        else
        {
            Console.WriteLine("Vous êtes déjà abonné");

            subscribeCacheEntries.Add(new SubscribeCacheEntry
            {
                Login = user,
                Expiration = expiration
            });
        }

        return false;
    }

    private async Task AddNicknameIfNeeded(string rawContent)
    {
        if (!rawContent.Contains("Choisir un pseudo"))
            return;

        _client.DefaultRequestHeaders.TryAddWithoutValidation("x-requested-with", "XMLHttpRequest");

        var sToken = rawContent.Split("test\",\"sToken\":\"")[2].Split("\"")[0];
        var name = NameGeneratorService.GenerateName();
        var req = await _client.PostAsync("https://www.dofus.com/fr/choisir-votre-pseudo",
                                          new FormUrlEncodedContent(new Dictionary<string, string>
                                          {
                                              { "usernickname", name },
                                              { "sAction", "test" },
                                              { "sToken", sToken },
                                              { "sLang", "fr" },
                                              { "sTestField", "usernickname" },
                                              { "aFormData[usernickname]", name },
                                              { "aFormData[sAction]", "submit" },
                                          }));

        var content = await req.Content.ReadAsStringAsync();
        
        _client.DefaultRequestHeaders.TryAddWithoutValidation("X-PJAX", "true");
        _client.DefaultRequestHeaders.TryAddWithoutValidation("X-PJAX-Container", ".ak-form-nickname");

        var req2 = await _client.PostAsync("https://www.dofus.com/fr/choisir-votre-pseudo",
                                          new FormUrlEncodedContent(new Dictionary<string, string>
                                          {
                                              { "usernickname", name },
                                              { "sAction", "submit" },
                                              { "_pjax", ".ak-form-nickname" },
                                          }));
        
        var content2 = await req2.Content.ReadAsStringAsync();
        
        _client.DefaultRequestHeaders.Remove("X-PJAX");
        _client.DefaultRequestHeaders.Remove("X-PJAX-Container");
        _client.DefaultRequestHeaders.Remove("x-requested-with");

        Console.WriteLine("Pseudo ajouté");
    }

    private bool IsSubscribed(string rawContent, string subType, out DateTime expiration)
    {
        expiration = DateTime.MinValue;
        const string pattern =
            @"<div class=""sub-status""[^>]*>\s*([^\r\n<]+)\s*<br\s*/?>\s*Abonné jusqu'au\s*(\d{2}/\d{2}/\d{4})\s*</div>";

        var matches = Regex.Matches(rawContent, pattern, RegexOptions.IgnoreCase);
        
        if (subType == "paysafecard")
        {
            if(matches.Count == 0)
                return false;

            return true;
        }
        
        foreach (Match m in matches)
        {
            Console.WriteLine($"Abonnement: {m.Groups[1].Value.Trim()} | Valide jusqu'au {m.Groups[2].Value}");
            // on convertit en date
            var date = DateTime.ParseExact(m.Groups[2].Value, "dd/MM/yyyy", null);
            // Si c'est dans moins de 2j on renouvelle
            expiration = date;
            
            if (date.Subtract(DateTime.Now).TotalDays < 2)
            {
                return false;
            }

            return true;
        }

        return false;
    }

    private async Task<bool> BuyOgrines(string rawContent)
    {
        var serverList = new Dictionary<string, string>();

        var pattern =
            @"<a\s+[^>]*class=""ak-block-server""[^>]*href=""[^""]*server_id=(\d+)[^""]*""[^>]*>[\s\S]*?<span\s+[^>]*class=""ak-name""[^>]*>([^<]+)</span>";

        var matches = Regex.Matches(rawContent, pattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var serverId = match.Groups[1].Value;
            var serverName = match.Groups[2].Value.Trim();
            Console.WriteLine($"ID: {serverId}, Nom: {serverName}");

            serverList.Add(serverId, serverName);
        }

        if (serverList.Count == 0)
        {
            if (rawContent.Contains("mes kamas sur : "))
            {
                // on cherche ça
                // <option selected="true" value="322">Dakal 4</option>
                var patternServer = @"<option selected=""true"" value=""(\d+)"">([^<]+)</option>";
                var matchServer = Regex.Match(rawContent, patternServer);

                if (matchServer.Success)
                {
                    var serverId = matchServer.Groups[1].Value;
                    var serverName = matchServer.Groups[2].Value.Trim();
                    Console.WriteLine($"ID: {serverId}, Nom: {serverName}");

                    serverList.Add(serverId, serverName);
                }
            }
            else
            {
                // i think we are on CGU page
                Console.WriteLine("CGU page je crois");

                var dicCgu = new Dictionary<string, string>
                {
                    { "postback", "1" },
                    { "from", "https://www.dofus.com/fr" },
                    { "version_cgu", "12" },
                    { "type_cgu", "CGU" },
                    { "validate_cgu", "1" }
                };
                await _client.PostAsync("https://account.ankama.com/fr/cgu?from=https%3A%2F%2Fwww.dofus.com%2Ffr",
                                        new FormUrlEncodedContent(dicCgu));

                // On attend genre 15sec le temps que ça se propage
                Console.WriteLine("CGU accepté, faudra relancer le tool");
                await Task.Delay(2000);

                return true;
            }
        }

        // En suite on fait une requête pour savoir le nombre de kamas du serveur
        var dic = new Dictionary<string, long>();

        foreach (var server in serverList)
        {
            _client.DefaultRequestHeaders.TryAddWithoutValidation("x-requested-with", "XMLHttpRequest");

            var str = await _client.GetAsync(
                $"https://www.dofus.com/fr/achat-kamas/selection-serveur?server={server.Value.Replace(" ", "+")}");

            var content = await str.Content.ReadAsStringAsync();

            var kamas = JsonSerializer.Deserialize<BakKamasTotal>(content.Trim());

            if (kamas != null)
            {
                dic.Add(server.Key, long.Parse(kamas.Current.Replace(" ", "")));
            }

            _client.DefaultRequestHeaders.Remove("x-requested-with");
        }


        // server with the most kamas
        var serverRichest = dic.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;

        Console.WriteLine($"Server with the most kamas: {serverList[serverRichest]}");

        var selectServeReq =
            await _client.GetStringAsync("https://www.dofus.com/fr/achat-kamas/selection-serveur?server_id=" +
                                         serverRichest);
        // On récupère le nombre d'ogrines actuel
        // <span> + 3 963 liées</span>
        var patternOgrines = @"<span>\s*\+\s*([\d\s]+?)\s*liées</span>";
        var matchOgrines = Regex.Match(selectServeReq, patternOgrines);

        if (matchOgrines.Success)
        {
            var nombreLiees = matchOgrines.Groups[1].Value.Trim().Replace(" ", "");
            Console.WriteLine($"Ogrines liées : {nombreLiees}");
            // Output: Ogrines liées : 3963

            if (int.Parse(nombreLiees) >= 1500)
            {
                Console.WriteLine("Vous avez déjà assez");
                await OnOgrinesBought();
                return true;
            }
        }

        var rates = await _client.GetStringAsync("https://www.dofus.com/fr/achat-kamas/achat-ogrines");

        // il faut recupérer le <input type="hidden" name="check_form" value="026deb2570f34ff6f1d42a4368270fe5b44135b0">
        var checkForm = rates.Split("name='check_form' value='")[1].Split("'")[0];

        // there is a <script type="application/json"> containing the rates but there is multiple of them we need the one containing typeoffer":"OGRINES",
        // then we need to parse it to get the rates
        var scriptList = rates.Split("<script type=\"application/json\">");
        foreach (var script in scriptList)
        {
            if (!script.Contains("typeoffer\":\"OGRINES"))
                continue;

            var content = script.Split("</script>")[0];
            var dofusBidOfferOgrines = JsonSerializer.Deserialize<DofusBidOfferOgrines>(content) ?? new();

            // On veut 1500 ogrines donc on doit regarder lequelle à un "Sum" avec au moins le nombre d'Ogrines qu'on veut, ils sont de base trier par taux
            foreach (var offer in dofusBidOfferOgrines.ActiveBid["OGRINES"])
            {
                if (long.Parse(offer.Sum) < 1500)
                    continue;

                Console.WriteLine($"Rate: {offer.Rate}");
                // On fait un post a https://www.dofus.com/fr/achat-kamas/achat-ogrines/acheter

                var data = new Dictionary<string, string>
                {
                    { "check_form", checkForm },
                    { "postback", "1" },
                    { "cancel_url", "%2Ffr%2Fachat-kamas%2Fachat-ogrines" },
                    { "part", "exchange" },
                    { "want", "1500" },
                    { "give", (int.Parse(offer.Rate) * 1500).ToString() },
                    { "rate", offer.Rate },
                    { "server_id", serverRichest },
                    { "confirm", "1" }
                };

                var contentData = new FormUrlEncodedContent(data);
                var req = await _client.PostAsync("https://www.dofus.com/fr/achat-kamas/achat-ogrines/acheter",
                                                  contentData);
                var response = await req.Content.ReadAsStringAsync();

                if (response.Contains("Les ogrines ont été crédités sur votre compte et sont prêtes a être utilisées"))
                {
                    Console.WriteLine("Achat effectué");
                    await OnOgrinesBought();
                }
                else
                {
                    Console.WriteLine("Erreur lors de l'achat");
                }

                break;
            }

            break;
        }

        return true;
    }

    public async Task OnOgrinesBought()
    {
        // On va récupérer l'id de l'article qui nous interesse
        var contentList =
            await _client.GetStringAsync("https://store.ankama.com/fr/729-dofus/785-abonnements#articles");

        var articles = contentList.Split("/fr/729-dofus/785-abonnements/", StringSplitOptions.RemoveEmptyEntries);
        var url = "";
        foreach (var article in articles)
        {
            if (!article.Contains("pack-7-jours"))
                continue;

            var articleEnd = article.Split("\"")[0];

            url = "https://store.ankama.com/fr/729-dofus/785-abonnements/" + articleEnd;
        }

        var articleId = url.Split("-")[3];
        var req = await _client.GetStringAsync(url);
        var token = req.Split("name=\"add_cart_classic[_token]\" value=\"")[1].Split("\"")[0];

        _client.DefaultRequestHeaders.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");

        var resultPaiement = await _client.PostAsJsonAsync("https://store.ankama.com/fr/api/direct-cart/add-article",
                                                           new
                                                           {
                                                               article_id = articleId,
                                                               choices_references = Array.Empty<string>(),
                                                               extra_data = new List<SimpleKv>()
                                                               {
                                                                   new()
                                                                   {
                                                                       Key = "id",
                                                                       Value = articleId
                                                                   },
                                                                   new()
                                                                   {
                                                                       Key = "_token",
                                                                       Value = token
                                                                   },
                                                                   new()
                                                                   {
                                                                       Key = "quantity",
                                                                       Value = "1"
                                                                   }
                                                               },
                                                               quantity = "1"
                                                           });

        _client.DefaultRequestHeaders.Remove("X-Requested-With");

        var reqListPaiement = await _client.GetStringAsync("https://store.ankama.com/fr/direct-cart/payment-choice");

        var resultPaiementModeIds = reqListPaiement.Split("value=\"");
        var resultPaiementModeId = resultPaiementModeIds[^1].Split("\"")[0];

        var dic = new Dictionary<string, string>()
        {
            { "action", "order" },
            { "oc_SaveCardPaymentModeOption_67", "1" },
            { "payment-mode", resultPaiementModeId }
        };

        // as FormData
        var content = new FormUrlEncodedContent(dic);
        var reqBuy = await _client.PostAsync("https://store.ankama.com/fr/direct-cart/payment-choice", content);

        var contentBuy = await reqBuy.Content.ReadAsStringAsync();


        if (contentBuy.Contains("Félicitations"))
        {
            Console.WriteLine("Commande effectuée");
        }
        else
        {
            Console.WriteLine("Erreur lors de la commande");
            Console.WriteLine(contentBuy);
        }


        // as FormData
        //await _client.PostAsync("https://store.ankama.com/fr/order?cart_type=direct-cart", content);
    }

    public async Task SubWithPaysafecard()
    {
        // On va récupérer l'id de l'article qui nous interesse
        var contentList =
            await _client.GetStringAsync("https://store.ankama.com/fr/729-dofus/785-abonnements#articles");

        var articles = contentList.Split("/fr/729-dofus/785-abonnements/", StringSplitOptions.RemoveEmptyEntries);
        var url = "";
        foreach (var article in articles)
        {
            if (!article.Contains("pack-7-jours"))
                continue;

            var articleEnd = article.Split("\"")[0];

            url = "https://store.ankama.com/fr/729-dofus/785-abonnements/" + articleEnd;
        }

        var articleId = url.Split("-")[3];
        var req = await _client.GetStringAsync(url);
        var token = req.Split("name=\"add_cart_classic[_token]\" value=\"")[1].Split("\"")[0];

        _client.DefaultRequestHeaders.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");

        var resultPaiement = await _client.PostAsJsonAsync("https://store.ankama.com/fr/api/direct-cart/add-article",
                                                           new
                                                           {
                                                               article_id = articleId,
                                                               choices_references = Array.Empty<string>(),
                                                               extra_data = new List<SimpleKv>()
                                                               {
                                                                   new()
                                                                   {
                                                                       Key = "id",
                                                                       Value = articleId
                                                                   },
                                                                   new()
                                                                   {
                                                                       Key = "_token",
                                                                       Value = token
                                                                   },
                                                                   new()
                                                                   {
                                                                       Key = "quantity",
                                                                       Value = "1"
                                                                   }
                                                               },
                                                               quantity = "1"
                                                           });

        // we check if we are redirected to https://store.ankama.com/fr/direct-cart/address/create

        _client.DefaultRequestHeaders.Remove("X-Requested-With");

        var reqListPaiementReq = await _client.GetAsync("https://store.ankama.com/fr/direct-cart/payment-choice");
        var reqListPaiement = await reqListPaiementReq.Content.ReadAsStringAsync();
        
        if (reqListPaiementReq.RequestMessage.RequestUri.ToString().Contains("address/create"))
        {
            Console.WriteLine("Adresse manquante");
            var tokenTest = reqListPaiement.Split("name=\"address[_token]\" value=\"")[1];
            
            var addressToken = reqListPaiement.Split("name=\"address[_token]\" value=\"")[1].Split("\"")[0];
            await CreateAddress(addressToken);
            
            reqListPaiementReq = await _client.GetAsync("https://store.ankama.com/fr/direct-cart/payment-choice");
            reqListPaiement = await reqListPaiementReq.Content.ReadAsStringAsync();
        }
        

        var resultPaiementModeIds = reqListPaiement.Split("value=\"");
        var resultPaiementModeId = resultPaiementModeIds[^4].Split("\"")[0];

        var dic = new Dictionary<string, string>()
        {
            { "action", "order" },
            { "oc_SaveCardPaymentModeOption_67", "1" },
            { "payment-mode", resultPaiementModeId }
        };

        // as FormData
        var content = new FormUrlEncodedContent(dic);
        var reqBuy = await _client.PostAsync("https://store.ankama.com/fr/direct-cart/payment-choice", content);

        var contentBuy = await reqBuy.Content.ReadAsStringAsync();

        var pattern = @"&quot;processoutCredentialsId&quot;:&quot;(?<projectId>proj_[A-Za-z0-9]+)&quot;|&quot;invoice-id&quot;:&quot;(?<invoice>iv_[A-Za-z0-9]+)&quot;|&quot;order-id&quot;:&quot;(?<orderId>\d+)&quot;|&quot;order-uid&quot;:&quot;(?<orderUid>[a-f0-9-]+)&quot;|&quot;gateway-id&quot;:&quot;(?<gateway>gway_conf_[a-z0-9]+\.hipaypaysafecard)&quot;";

        var invoiceId = string.Empty;
        var orderId = string.Empty;
        var orderUid = string.Empty;
        var projectId = string.Empty;
        var gatewayConfig = string.Empty;

        var matches = Regex.Matches(contentBuy, pattern);
        foreach (Match m in matches)
        {
            if (m.Groups["invoice"].Success)
                invoiceId = m.Groups["invoice"].Value;
            if (m.Groups["orderId"].Success)
                orderId = m.Groups["orderId"].Value;
            if (m.Groups["orderUid"].Success)
                orderUid = m.Groups["orderUid"].Value;
            if (m.Groups["projectId"].Success)
                projectId = m.Groups["projectId"].Value;
            if (m.Groups["gateway"].Success)
                gatewayConfig = m.Groups["gateway"].Value;
        }

        var payLog = await _client.PostAsJsonAsync("https://store.ankama.com/fr/api/payment/log-action",
                                                   new
                                                   {
                                                       action = "try_pay_click",
                                                       extraData = new
                                                       {
                                                           invoiceId,
                                                       },
                                                       orderId = orderId,
                                                       orderUid = orderUid,
                                                   });

        var payTest =
            await _client.GetAsync(
                $"https://checkout.processout.com/{projectId}/{invoiceId}/redirect/{gatewayConfig}?additional_data[issuer_country]=FR");
        var payContent = await payTest.Content.ReadAsStringAsync();

        var mid = payContent.Split("mid\": \"")[1].Split("\"")[0];
        var mtid = payContent.Split("mtid\": \"")[1].Split("\"")[0];
        var authContextId = payContent.Split("authContextId\" value=\"")[1].Split("\"")[0];
        
        // on lis toute les lignes du fichier paysafecard.txt 
        var lines = await File.ReadAllLinesAsync("paysafecard.txt");
        
        var paysafeCardToUse = string.Empty;
        
        // on itére sur chaque ligne pour connaitre la balance
        foreach (var line in lines)
        {
            var paysafecard = line.Replace(" ", "");
            var pinInfoReq = await _client.PostAsJsonAsync(
                $"https://customer.cc.at.paysafecard.com/rest/payment/classic/pin", new
                {
                    amount = 2.5,
                    authContextId = authContextId,
                    currency = "EUR",
                    locale = "fr_FR",
                    mid = mid,
                    mtid = mtid,
                    pins = new string[] { paysafecard },
                    applyNowData = new {}
                });
            
            var pinInfoContent = await pinInfoReq.Content.ReadAsStringAsync();
            var pinInfo = JsonSerializer.Deserialize<PinCardInfo>(pinInfoContent);
            
            if (pinInfo != null && pinInfo.CardInfoList.Count > 0)
            {
                var cardInfo = pinInfo.CardInfoList[0];
                if (string.IsNullOrEmpty(cardInfo.Amount))
                {
                    var newLines = lines.Where(x => x != line).ToArray();
                    await File.WriteAllLinesAsync("paysafecard.txt", newLines);
                    continue;
                }
                
                if (double.Parse(cardInfo.Amount, CultureInfo.InvariantCulture) >= 2.5)
                {
                    paysafeCardToUse = paysafecard;
                    break;
                }
                else
                {
                    // on supprime la ligne du fichier
                    var newLines = lines.Where(x => x != line).ToArray();
                    await File.WriteAllLinesAsync("paysafecard.txt", newLines);
                }
            }
        }
        
        var accept = await _client.PostAsJsonAsync(
            $"https://customer.cc.at.paysafecard.com/rest/payment/classic/assignCards", new
            {
                amount = 2.5,
                authContextId = authContextId,
                currency = "EUR",
                locale = "fr_FR",
                mid = mid,
                mtid = mtid,
                pins = new[] { paysafeCardToUse },
                termsAndConditionsAccepted = true
            });
        
        var acceptContent = await accept.Content.ReadAsStringAsync();

        if (acceptContent.Contains("paysafecard/success"))
        {
            Console.WriteLine("Commande effectuée");
        }
        else
        {
            Console.WriteLine("Erreur lors de la commande");
            Console.WriteLine(contentBuy);
        }
    }

    private async Task CreateAddress(string token)
    {
        var firstNames = new[]
        {
            "Alphonse", "Benoit", "Cyril", "David", "Emmanuel", "Franck", "Gilles", "Hugo", "Igor", "Jules", "Karl",
            "Lucas",
            "Maurice", "Nicolas", "Olivier", "Pierre", "Quentin", "Romain", "Sylvain", "Thibault", "Ulysse", "Victor",
            "William", "Xavier", "Yann", "Zacharie"
        };
        var lastNames = new[]
        {
            "Teston", "Dupont", "Durand", "Martin", "Bernard", "Dubois", "Thomas", "Robert", "Richard", "Petit",
            "Durand",
            "Leroy", "Moreau", "Simon", "Laurent", "Lefevre", "Michel", "Garcia", "David", "Bertrand", "Roux",
            "Vincent",
            "Fournier", "Morel", "Girard", "Andre"
        };

        var dic = new Dictionary<string, string>()
        {
            { "address[name]", "Bureau" },
            { "address[civility]", "0" },
            { "address[firstname]", firstNames[Random.Shared.Next(firstNames.Length)] },
            { "address[lastname]", lastNames[Random.Shared.Next(lastNames.Length)] },
            { "address[address]", "1 rue de " + lastNames[Random.Shared.Next(lastNames.Length)] },
            { "address[additional_address]", "" },
            { "address[zipcode]", "75001" },
            { "address[city]", "Paris" },
            { "address[states]", "" },
            { "address[_token]", token }
        };

        await _client.PostAsync("https://store.ankama.com/fr/direct-cart/address/create",
                                new FormUrlEncodedContent(dic));
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
}

public class BakKamasTotal
{
    [JsonPropertyName("current")] public required string Current { get; set; }

    [JsonPropertyName("waiting")] public required string Waiting { get; set; }
}

public class DofusBidOfferOgrines
{
    [JsonPropertyName("typeoffer")] public string TypeOffer { get; set; } = "";

    [JsonPropertyName("ActiveBid")] public Dictionary<string, List<DofusBidOfferRate>> ActiveBid { get; set; } = new();
}

public class DofusBidOfferRate
{
    [JsonPropertyName("sum")] public string Sum { get; set; }

    [JsonPropertyName("rate")] public string Rate { get; set; }

    [JsonPropertyName("change")] public double Change { get; set; }
}

public class SimpleKv
{
    [JsonPropertyName("key")] public string Key { get; set; } = "";

    [JsonPropertyName("value")] public string Value { get; set; } = "";
}

public class PinCardInfo
{
    [JsonPropertyName("cardInfoList")]
    public List<CardInfo> CardInfoList { get; set; } = new();
}

public class CardInfo
{
    [JsonPropertyName("cardBalance")] public string Amount { get; set; }
}