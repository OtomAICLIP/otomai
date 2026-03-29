using System.Text.Json.Serialization;

namespace BubbleBot.Subscribe;

public class SubscribeCacheEntry
{
    [JsonPropertyName("login")]
    public string Login { get; set; }
    
    [JsonPropertyName("Expiration")]
    public DateTime Expiration { get; set; }
}