using Bubble.Core.Datacenter;
using Bubble.Core.Datacenter.Datacenter.Spells;
using Bubble.Core.Services;

namespace BubbleBot.Cli.Repository;

public class SpellRepository : Singleton<SpellRepository>
{
    private Dictionary<ushort, Spells> _spells;
    private Dictionary<int, SpellLevels> _spellLevels;
    private Dictionary<int, SpellStates> _spellStates;
    
    private static readonly Dictionary<int, IList<SpellLevels>> SpellLevelBySpellIds = new();

    public SpellRepository()
    {
        _spells = new Dictionary<ushort, Spells>();
        _spellLevels = new Dictionary<int, SpellLevels>();
        _spellStates = new Dictionary<int, SpellStates>();
    }

    public void Initialize()
    {
        _spells = (DatacenterService.Load<Spells>()).Values.ToDictionary(x => x.Id);
        _spellLevels = (DatacenterService.Load<SpellLevels>()).Values.ToDictionary(x => x.Id);
        _spellStates = (DatacenterService.Load<SpellStates>()).Values.ToDictionary(x => x.Id);
        
        foreach (var spell in _spells)
        {
            SpellLevelBySpellIds.Add(spell.Key, spell.Value.SpellLevels.Select(x => _spellLevels[(int)x]).ToArray());
        }
    }
    
    public Spells? GetSpell(int id)
    {
        return _spells.TryGetValue((ushort)id, out var spell) ? spell : null;
    }
    
    public SpellLevels? GetSpellLevel(int id)
    {
        return _spellLevels.TryGetValue(id, out var spellLevel) ? spellLevel : null;
    }
    
    public SpellLevels? GetSpellLevel(int spellId, short level)
    {
        return !SpellLevelBySpellIds.TryGetValue(spellId, out var spell)
            ? null
            : spell.FirstOrDefault(x => x.Grade == level);
    }

    public bool TryGetSpellState(short stateId, out SpellStates? o)
    {
        return _spellStates.TryGetValue(stateId, out o);
    }
}