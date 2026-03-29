namespace OtomAI.Protocol.Shared.Api;

public class HttpRetryClient
{
    public static HttpClient GetClient()
    {
        return new HttpClient(new RetryHandler(new HttpClientHandler()));
    }
}
