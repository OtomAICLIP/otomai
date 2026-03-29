namespace Bubble.Core.Datacenter.Datacenter.Items;

[Flags]
public enum ItemFlags : ushort
{
    Cursed = 1,
    Usable = 2,
    Targetable = 4,
    Exchangeable = 8,
    TwoHanded = 16,
    Etheral = 32,
    HideEffects = 64,
    Enhanceable = 128,
    NonUsableOnAnother = 256,
    SecretRecipe = 512,
    ObjectIsDisplayOnWeb = 1024,
    BonusIsSecret = 2048,
    NeedUseConfirm = 4096,
    IsDestructible = 8192,
    IsSaleable = 16384,
    IsLegendary = 32768
}