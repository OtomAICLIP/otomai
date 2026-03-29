namespace Bubble.Core.Datacenter;

public class CatalogModel
{
    [JsonPropertyName("m_InternalIds")]
    public List<string> InternalIds { get; set; } = new();
}