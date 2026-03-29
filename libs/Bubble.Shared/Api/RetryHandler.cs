namespace Bubble.Shared.Api;

public class RetryHandler(HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler)
{
    // Strongly consider limiting the number of retries - "retry forever" is
    // probably not the most user friendly way you could respond to "the
    // network cable got pulled out."
    private const int MaxRetries = 3;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            return response;
        }
        
        for (var i = 1; i < MaxRetries; i++)
        {
            response = await base.SendAsync(request, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return response;
            }
        }

        return response;
    }
}
