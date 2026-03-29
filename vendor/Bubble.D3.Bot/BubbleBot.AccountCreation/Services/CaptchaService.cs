using System.Net;
using System.Text;
using System.Text.Json;
using Bubble.Core.Services;
using Serilog;

namespace BubbleBot.AccountCreation.Services;

public class CaptchaService : Singleton<CaptchaService>
{
    public async Task<string> GetAmazonWaf(string url, string key, string iv, string context, string urlCaptcha)
    {
        var apiKey = GetApiKey();
        var success = false;
        while (!success)
        {
            using var httpCaptchaClient = new HttpClient();
            var req = await httpCaptchaClient.PostAsync("https://api.capsolver.com/createTask",
                                                        new StringContent(
                                                            JsonSerializer.Serialize(new CapmonsterRequest
                                                            {
                                                                ClientKey =
                                                                    apiKey,
                                                                Task = new AwsCaptchaRequest
                                                                {
                                                                    Type = "AntiAwsWafTaskProxyLess",
                                                                    WebsiteURL = url,
                                                                    AwsKey = key,
                                                                    AwsIv = iv,
                                                                    AwsContext = context,
                                                                    AwsChallengeJS = urlCaptcha
                                                                }
                                                            }),
                                                            Encoding.UTF8,
                                                            "application/json"));

            var captchaReqContent = await req.Content.ReadAsStringAsync();
            var captchaGetTask = JsonSerializer.Deserialize<CapMonsterReqResult>(captchaReqContent)!;
            if (!string.IsNullOrEmpty(captchaGetTask.ErrorCode))
            {
                Log.Error("Erreur lors de la création du captcha: {ErrorCode} - {ErrorDescription}",
                          captchaGetTask.ErrorCode,
                          captchaGetTask.ErrorDescription);

                success = false;
                continue;
            }

            Console.WriteLine("Task ID: {0}", captchaGetTask.TaskId);
            var cookie = await GetAnswer(httpCaptchaClient, captchaGetTask);

            if(string.IsNullOrEmpty(cookie))
            {
                Log.Error("Erreur, demande d'un nouveau captcha");
                success = false;
                continue;
            }
            
            Console.WriteLine("Captcha solved.");
            Console.WriteLine("Captcha code: {0}", cookie);
            
            return cookie;
        }
        
        return string.Empty;
    }

    private static async Task<string> GetAnswer(HttpClient httpCaptchaClient, CapMonsterReqResult captchaGetTask)
    {
        var apiKey = GetApiKey();
        var successAnswer = false;
        var cookie = string.Empty;

        while (!successAnswer)
        {
            try
            {
                var response =
                    await httpCaptchaClient.PostAsync("https://api.capsolver.com/getTaskResult",
                                                      new StringContent(
                                                          JsonSerializer.Serialize(new CapMonsterGetTaskRequest
                                                          {
                                                              TaskId = captchaGetTask.TaskId,
                                                              ClientKey = apiKey
                                                          }),
                                                          Encoding.UTF8,
                                                          "application/json"));

                var captchaResContent = await response.Content.ReadAsStringAsync();
                var captchaRes = JsonSerializer.Deserialize<CapMonsterGetTaskResult>(captchaResContent)!;
                if (captchaRes.Status == "failed")
                {
                    Log.Error("Erreur lors de la résolution du captcha " + captchaResContent);
                    return cookie;
                }

                if (captchaRes.Status != "ready")
                {
                    Log.Information("Captcha en cours de résolution " + captchaRes.Status);
                    await Task.Delay(5000);
                    continue;
                }

                cookie = captchaRes.Solution.Cookie;
                successAnswer = true;
            }
            catch (WebException e)
            {
                Log.Error(e, "Erreur lors de la résolution du captcha");
                Thread.Sleep(5000);
            }
        }

        return cookie;
    }

    private static string GetApiKey()
    {
        var apiKey = Environment.GetEnvironmentVariable("BUBBLE_CAPSOLVER_API_KEY");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            return apiKey;
        }

        throw new InvalidOperationException(
            "Missing Capsolver API key. Set the BUBBLE_CAPSOLVER_API_KEY environment variable.");
    }
}
