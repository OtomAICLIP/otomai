using Bubble.Core.Datacenter.Attributes;
using Bubble.Core.Datacenter.Datacenter.Effects;

namespace Bubble.Core.Datacenter.Datacenter.Items;

[DatacenterObject("Core.DataCenter.Metadata.Item", "Items", "Ankama.Dofus.Core.DataCenter", nameof(Id))]
public partial class Items : IDofusRootObject
{
    public static string FileName => "data_assets_itemsroot.asset.bundle";

    [DatacenterPropertyAs<ushort>]
    public required ItemFlags Flags { get; set; }

    public required ushort Id { get; set; }

    [DatacenterPropertyText]
    public required uint NameId { get; set; }

    public required ushort TypeId { get; set; }

    [DatacenterPropertyText]
    public required uint DescriptionId { get; set; }

    public required int IconId { get; set; }

    public required byte Level { get; set; }

    public required ushort RealWeight { get; set; }

    public required sbyte UseAnimationId { get; set; }

    public required float Price { get; set; }

    public required short ItemSetId { get; set; }

    public required string Criteria { get; set; }

    public required string CriteriaTarget { get; set; }

    public required ushort AppearanceId { get; set; }

    public required bool IsColorable { get; set; }

    public required byte RecipeSlots { get; set; }

    public required List<ushort> RecipeIds { get; set; }

    public required List<ushort> DropMonsterIds { get; set; }

    public required List<ushort> DropTemporisMonsterIds { get; set; }

    [DatacenterPropertyLinked<EffectInstanceDice>("PossibleEffects")]
    public required List<long> PossibleEffectRids { get; set; }

    public required List<ushort> EvolutiveEffectIds { get; set; }

    public required List<short> FavoriteSubAreas { get; set; }

    public required ushort FavoriteSubAreasBonus { get; set; }

    public required short CraftXpRatio { get; set; }

    public required string CraftVisible { get; set; }

    public required string CraftConditional { get; set; }

    public required string CraftFeasible { get; set; }

    public required string Visibility { get; set; }

    public required float RecyclingNuggets { get; set; }

    public required List<int> FavoriteRecyclingSubAreas { get; set; }

    public required List<List<int>> ResourcesBySubarea { get; set; }

    public required string ImportantNoticeId { get; set; }

    public required string ChangeVersion { get; set; }

    public required double TooltipExpirationDate { get; set; }
    
    public bool Cursed => (Flags & ItemFlags.Cursed) != 0;
    public bool Usable => (Flags & ItemFlags.Usable) != 0;
    public bool Targetable => (Flags & ItemFlags.Targetable) != 0;
    public bool Exchangeable => (Flags & ItemFlags.Exchangeable) != 0;
    public bool TwoHanded => (Flags & ItemFlags.TwoHanded) != 0;
    public bool Etheral => (Flags & ItemFlags.Etheral) != 0;
    public bool HideEffects => (Flags & ItemFlags.HideEffects) != 0;
    public bool Enhanceable => (Flags & ItemFlags.Enhanceable) != 0;
    public bool NonUsableOnAnother => (Flags & ItemFlags.NonUsableOnAnother) != 0;
    public bool SecretRecipe => (Flags & ItemFlags.SecretRecipe) != 0;
    public bool ObjectIsDisplayOnWeb => (Flags & ItemFlags.ObjectIsDisplayOnWeb) != 0;
    public bool BonusIsSecret => (Flags & ItemFlags.BonusIsSecret) != 0;
    public bool NeedUseConfirm => (Flags & ItemFlags.NeedUseConfirm) != 0;
    public bool IsDestructible => (Flags & ItemFlags.IsDestructible) != 0;
    public bool IsSaleable => (Flags & ItemFlags.IsSaleable) != 0;
    public bool IsLegendary => (Flags & ItemFlags.IsLegendary) != 0;

}