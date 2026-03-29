using System.Text;
using System.Text.Json;
using Bubble.Core.Datacenter;
using Bubble.Core.Datacenter.Datacenter.Quest.TreasureHunt;
using Bubble.Core.Services;
using BubbleBot.Cli.Repository.Maps;
using BubbleBot.Cli.Services.TreasureHunts.Models;
using Serilog;

namespace BubbleBot.Cli.Services.TreasureHunts;

public class CluesSolver : Singleton<CluesSolver>
{
    private DofusPourLesNoobsFile? _clues;
    private Dictionary<string, List<DofusDbResponseData>> _cache = new();

    public static string RemoveAccents(string str)
    {
        Dictionary<char, char> replacements = new()
        {
            { 'à', 'a' },
            { 'ç', 'c' },
            { 'é', 'e' },
            { 'è', 'e' },
            { 'ê', 'e' },
            { 'ë', 'e' },
            { 'ô', 'o' },
            { 'û', 'u' },
            { 'ù', 'u' }
        };

        Dictionary<char, string> strReplacements = new()
        {
            { 'œ', "oe" },
            { 'Œ', "Oe" }
        };

        StringBuilder stringBuilder = new();

        foreach (char c in str)
        {
            if (replacements.TryGetValue(c, out char r))
            {
                stringBuilder.Append(r);
            }
            else if (strReplacements.TryGetValue(c, out var s))
            {
                stringBuilder.Append(s);
            }
            else
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString();
    }

    public async Task Initialize()
    {
        if (File.Exists("Data/clues.json"))
        {
            var cacheFile = await File.ReadAllTextAsync("Data/clues.json");
            _cache = JsonSerializer.Deserialize<Dictionary<string, List<DofusDbResponseData>>>(cacheFile) ?? new();
        }
        
        var cluesInGame = DatacenterService.Load<PointOfInterest>();
        
        var cluesLocal = await File.ReadAllTextAsync("Data/dofuspourlesnoobs_clues.json");
        
        var clues = JsonSerializer.Deserialize<DofusPourLesNoobsFile>(cluesLocal);
        
        var cluesMapping = new Dictionary<int, int>();
        foreach (var clue in clues!.Clues)
        {
            var gameClue = cluesInGame.Values.FirstOrDefault(x
                                                                 => string.Equals(RemoveAccents(x.Name), RemoveAccents(clue.HintFr), StringComparison.CurrentCultureIgnoreCase));

            if (gameClue == null)
            {
                continue;
            }
            
            cluesMapping[clue.ClueId] = gameClue.Id;
        }

        foreach (var map in clues.Maps)
        {
            map.Clues = map.Clues.Select(x => cluesMapping.TryGetValue(x, out var v) ? v : x).ToList();
        }
        
        _clues = clues;
    }

    public (int x, int y) SolveClueFromLocal(int clueId, int fromX, int fromY, Direction direction)
    {
        if(_clues == null)
            return (666, 666);
        
        var maxDistance = 10;

        for (var i = 1; i <= maxDistance; i++)
        {
            var (x, y) = (fromX, fromY);
            switch (direction)
            {
                case Direction.North:
                    y -= i;
                    break;
                case Direction.South:
                    y += i;
                    break;
                case Direction.East:
                    x += i;
                    break;
                case Direction.West:
                    x -= i;
                    break;
            }
            
            var mapInfo =  _clues.Maps.FirstOrDefault(z => z.X == x && z.Y == y);
            
            if (mapInfo == null)
                continue;
            
            if (mapInfo.Clues.Contains(clueId))
            {
                return (x, y);
            }
        }

        return (666, 666);
    }

    public (int x, int y) SolveClue(int clueId, int x, int y, int direction)
    {
        try
        {
            var cacheKey = $"{x}_{y}_{direction}";

            if (!_cache.TryGetValue(cacheKey, out var clues))
            {
                return (666, 666);
            }
            
            var clueRelated = clues.FirstOrDefault(z => z.Pois.Any(p => p.Id == clueId));

            if (clueRelated == null)
            {
                return (666, 666);
            }

            return (clueRelated.PosX, clueRelated.PosY);
        }
        catch (Exception e)
        {
            return (666, 666);
        }
    }


    
}