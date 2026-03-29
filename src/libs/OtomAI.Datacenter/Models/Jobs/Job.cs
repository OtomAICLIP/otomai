using OtomAI.Datacenter.Attributes;

namespace OtomAI.Datacenter.Models.Jobs;

/// <summary>
/// Job/craft definitions. Mirrors Bubble.Core.Datacenter's Job model set.
/// </summary>
[DatacenterObject("Jobs")]
public sealed class Job
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int IconId { get; set; }
    public int[] ToolIds { get; set; } = [];
}

[DatacenterObject("Recipes")]
public sealed class Recipe
{
    public int ResultId { get; set; }
    public int ResultLevel { get; set; }
    public int JobId { get; set; }
    public RecipeIngredient[] Ingredients { get; set; } = [];
}

public sealed class RecipeIngredient
{
    public int ItemId { get; set; }
    public int Quantity { get; set; }
}
