using Bubble.Core.Datacenter.Attributes;

namespace Bubble.Core.Datacenter.Datacenter.Monster;

[DatacenterObject("Core.DataCenter.Metadata.Monster", "Monsters", "Ankama.Dofus.Core.DataCenter", nameof(Id))]
public sealed partial class Monsters : IDofusRootObject
{
    public static string FileName => "data_assets_monstersroot.asset.bundle";
    
    
    public required ushort Flags { get; set; }
    public required ushort Id { get; set; }
    [DatacenterPropertyText]
    public required int NameId { get; set; }
    public required ushort GfxId { get; set; }
    public required short Race { get; set; }
    public required List<MonsterGrade> Grades { get; set; }
    public required string Look { get; set; } = string.Empty;
    public required List<AnimFunMonsterData> AnimFunList { get; set; }
    public required List<MonsterDrop> Drops { get; set; }
    public required List<MonsterDrop> TemporisDrops { get; set; }
    public required List<uint> Subareas { get; set; }
    public required List<int> Spells { get; set; }
    public required List<string> SpellGrades { get; set; }
    public required ushort FavoriteSubareaId { get; set; }
    public required ushort CorrespondingMiniBossId { get; set; }
    public required sbyte SpeedAdjust { get; set; }
    public required sbyte CreatureBoneId { get; set; }
    public required List<uint> IncompatibleIdols { get; set; }
    public required List<uint> IncompatibleChallenges { get; set; }
    public required byte AggressiveZoneSize { get; set; }
    public required short AggressiveLevelDiff { get; set; }
    public required string AggressiveImmunityCriterion { get; set; } = string.Empty;
    public required short AggressiveAttackDelay { get; set; }
    public required byte ScaleGradeRef { get; set; }
    public required List<List<float>> CharacRatios { get; set; }
    
    public bool UseSummonSlot => (Flags & (ushort)MonsterFlags.UseSummonSlot) != 0;
    public bool UseBombSlot => (Flags & (ushort)MonsterFlags.UseBombSlot) != 0;
    public bool IsBoss => (Flags & (ushort)MonsterFlags.IsBoss) != 0;
    public bool IsMiniBoss => (Flags & (ushort)MonsterFlags.IsMiniBoss) != 0;
    public bool IsQuestMonster => (Flags & (ushort)MonsterFlags.IsQuestMonster) != 0;
    public bool FastAnimsFun => (Flags & (ushort)MonsterFlags.FastAnimsFun) != 0;
    public bool CanPlay => (Flags & (ushort)MonsterFlags.CanPlay) != 0;
    public bool CanTackle => (Flags & (ushort)MonsterFlags.CanTackle) != 0;
    public bool CanBePushed => (Flags & (ushort)MonsterFlags.CanBePushed) != 0;
    public bool CanSwitchPos => (Flags & (ushort)MonsterFlags.CanSwitchPos) != 0;
    public bool CanSwitchPosOnTarget => (Flags & (ushort)MonsterFlags.CanSwitchPosOnTarget) != 0;
    public bool CanBeCarried => (Flags & (ushort)MonsterFlags.CanBeCarried) != 0;
    public bool CanUsePortal => (Flags & (ushort)MonsterFlags.CanUsePortal) != 0;
    public bool AllIdolsDisabled => (Flags & (ushort)MonsterFlags.AllIdolsDisabled) != 0;
    public bool UseRaceValues => (Flags & (ushort)MonsterFlags.UseRaceValues) != 0;
    public bool SoulCaptureForbidden => (Flags & (ushort)MonsterFlags.SoulCaptureForbidden) != 0;

    
}

[DatacenterObject("Core.DataCenter.Metadata.Monster", "MonsterGrade", "Ankama.Dofus.Core.DataCenter", "0")]
public sealed partial class MonsterGrade
{
    public required MonsterBonusCharacteristics BonusCharacteristics { get; set; }
    public required int Grade { get; set; }
    public required ushort MonsterId { get; set; }
    public required ushort Level { get; set; }
    public required int LifePoints { get; set; }
    public required short ActionPoints { get; set; }
    public required short MovementPoints { get; set; }
    public required int Vitality { get; set; }
    public required short PaDodge { get; set; }
    public required short PmDodge { get; set; }
    public required ushort Wisdom { get; set; }
    public required short EarthResistance { get; set; }
    public required short AirResistance { get; set; }
    public required short FireResistance { get; set; }
    public required short WaterResistance { get; set; }
    public required short NeutralResistance { get; set; }
    public required int GradeXp { get; set; }
    public required byte DamageReflect { get; set; }
    public required byte HiddenLevel { get; set; }
    public required ushort Strength { get; set; }
    public required ushort Intelligence { get; set; }
    public required ushort Chance { get; set; }
    public required ushort Agility { get; set; }
    public required int StartingSpellId { get; set; }
    public required sbyte BonusRange { get; set; }
}

[DatacenterObject("Core.DataCenter.Metadata.Monster", "MonsterBonusCharacteristics", "Ankama.Dofus.Core.DataCenter", "0")]
public sealed partial class MonsterBonusCharacteristics
{
    public required int LifePoints { get; set; }
    public required ushort Strength { get; set; }
    public required ushort Wisdom { get; set; }
    public required ushort Chance { get; set; }
    public required ushort Agility { get; set; }
    public required ushort Intelligence { get; set; }
    public required short EarthResistance { get; set; }
    public required short FireResistance { get; set; }
    public required short WaterResistance { get; set; }
    public required short AirResistance { get; set; }
    public required short NeutralResistance { get; set; }
    public required byte TackleEvade { get; set; }
    public required byte TackleBlock { get; set; }
    public required byte BonusEarthDamage { get; set; }
    public required byte BonusFireDamage { get; set; }
    public required byte BonusWaterDamage { get; set; }
    public required byte BonusAirDamage { get; set; }
    public required byte APRemoval { get; set; }
    
}

[DatacenterObject("Core.DataCenter.Metadata.Monster", "AnimFunMonsterData", "Ankama.Dofus.Core.DataCenter", "0")]
public sealed partial class AnimFunMonsterData
{
    public required int AnimId { get; set; }
    public required int EntityId { get; set; }
    public required string AnimName { get; set; } = string.Empty;
    public required int AnimWeight { get; set; }
}

[DatacenterObject("Core.DataCenter.Metadata.Monster", "MonsterDrop", "Ankama.Dofus.Core.DataCenter", "0")]
public sealed partial class MonsterDrop
{
    public required int DropId { get; set; }
    public required int MonsterId { get; set; }
    public required int ObjectId { get; set; }
    public required float PercentDropForGrade1 { get; set; }
    public required float PercentDropForGrade2 { get; set; }
    public required float PercentDropForGrade3 { get; set; }
    public required float PercentDropForGrade4 { get; set; }
    public required float PercentDropForGrade5 { get; set; }
    public required int Count { get; set; }
    public required string Criteria { get; set; } = string.Empty;
    public required bool HasCriteria { get; set; }
    public required bool HiddenIfInvalidCriteria { get; set; }
    public required List<MonsterDropCoefficient> SpecificDropCoefficient { get; set; }
}

[DatacenterObject("Core.DataCenter.Metadata.Monster", "MonsterDropCoefficient", "Ankama.Dofus.Core.DataCenter", "0")]
public sealed partial class MonsterDropCoefficient
{  
    public required int MonsterId { get; set; }
    public required int MonsterGrade { get; set; }
    public required float DropCoefficient { get; set; }
    public required string Criteria { get; set; } = string.Empty;
}