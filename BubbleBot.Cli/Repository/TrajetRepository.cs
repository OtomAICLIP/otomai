using System.Text.Json;
using Bubble.Core.Datacenter;
using Bubble.Core.Services;
using BubbleBot.Cli.Models;

namespace BubbleBot.Cli.Repository;

public class TrajetRepository : Singleton<TrajetRepository>
{

    public TrajetSettings? LoadTrajet(string id)
    {
        if(File.Exists($"trajets/{id}.json"))
        {
            return JsonSerializer.Deserialize<TrajetSettings>(File.ReadAllText($"Trajets/{id}.json"));
        }
        
        return null;
    }
}