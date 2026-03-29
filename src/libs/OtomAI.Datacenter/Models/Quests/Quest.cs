using OtomAI.Datacenter.Attributes;

namespace OtomAI.Datacenter.Models.Quests;

/// <summary>
/// Quest definitions. Mirrors Bubble.Core.Datacenter's Quest model set.
/// </summary>
[DatacenterObject("Quests")]
public sealed class Quest
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int[] StepIds { get; set; } = [];
    public bool IsRepeatable { get; set; }
}

[DatacenterObject("QuestSteps")]
public sealed class QuestStep
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int QuestId { get; set; }
    public int[] ObjectiveIds { get; set; } = [];
}
