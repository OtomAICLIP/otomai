using Serilog;

namespace BubbleBot.Cli;

public class CharacterInfo
{
    public Character.CharacterBasicInformation? Information { get; set; }

    public int CellId { get; set; }

    public int Direction { get; set; }
    public bool IsRiding { get; set; }
    public string Name => Information?.Name ?? "Unknown";

    public void UpdateFrom(EntityLook                                                         look, 
                           ActorPositionInformation.ActorInformation.RolePlayActor.NamedActor actor,
                           EntityDisposition                                                  disposition)
    {
        if (!string.IsNullOrEmpty(Name))
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                   "BubbleBot");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            var path = Path.Combine(dir, $"{Name}.txt");
            
            if (!File.Exists(path))
            {
                File.WriteAllText(path, Name);
            }
        }

        IsRiding = look.SubEntities.Any(sub => sub.BindingPointCategory == SubEntityInformation.BindingPointCategoryEnum.HookPointCategoryMountDriver);
        CellId = disposition.CellId;
        Direction = (int)disposition.Direction;
    }

}

