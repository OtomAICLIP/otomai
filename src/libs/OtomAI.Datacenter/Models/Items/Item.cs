using OtomAI.Datacenter.Attributes;

namespace OtomAI.Datacenter.Models.Items;

/// <summary>
/// Item definition. Mirrors Bubble.Core.Datacenter's Items model set.
/// </summary>
[DatacenterObject("Items")]
public sealed class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int TypeId { get; set; }
    public int Level { get; set; }
    public int Weight { get; set; }
    public bool Usable { get; set; }
    public bool Targetable { get; set; }
    public bool Exchangeable { get; set; }
    public int Price { get; set; }
    public bool TwoHanded { get; set; }
    public int[] PossibleEffects { get; set; } = [];
    public int[] RecipeIds { get; set; } = [];
    public int ItemSetId { get; set; }
    public int CriteriaExpression { get; set; }
}

[DatacenterObject("ItemTypes")]
public sealed class ItemType
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SuperTypeId { get; set; }
    public bool IsInvisible { get; set; }
}

[DatacenterObject("ItemSets")]
public sealed class ItemSet
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int[] Items { get; set; } = [];
}
