using System.Text.Json.Serialization;

namespace BubbleBot.FrigostAdapter.Models;

public class Settings
{
    [JsonPropertyName("icon")]
    public string Icon { get; set; }

    [JsonPropertyName("alias")]
    public string Alias { get; set; }

    [JsonPropertyName("account")]
    public string Account { get; set; }

    [JsonPropertyName("password")]
    public string Password { get; set; }

    [JsonPropertyName("network")]
    public string Network { get; set; }

    [JsonPropertyName("proxy")]
    public string Proxy { get; set; }

    [JsonPropertyName("proxy_ip")]
    public string ProxyIp { get; set; }

    [JsonPropertyName("proxy_port")]
    public string ProxyPort { get; set; }

    [JsonPropertyName("proxy_username")]
    public string ProxyUsername { get; set; }

    [JsonPropertyName("proxy_password")]
    public string ProxyPassword { get; set; }

    [JsonPropertyName("proxy_type")]
    public string ProxyType { get; set; }

    [JsonPropertyName("confort_settings")]
    public ConfortSettings ConfortSettings { get; set; }

    [JsonPropertyName("bot_settings")]
    public BotSettings BotSettings { get; set; }

}

public class MinMax
{
    [JsonPropertyName("min")] public int Min { get; set; }

    [JsonPropertyName("max")] public int Max { get; set; }
}

public class BotSettings
{
    [JsonPropertyName("fight_settings")] public FightSettings FightSettings { get; set; }
}

public class ConfortSettings
{
    [JsonPropertyName("team_number")] public int TeamNumber { get; set; }

    [JsonPropertyName("lock_fps")] public bool LockFps { get; set; }

    [JsonPropertyName("lock_fps_value")] public int LockFpsValue { get; set; }

    [JsonPropertyName("auto_accept_exchange")]
    public bool AutoAcceptExchange { get; set; }

    [JsonPropertyName("auto_accept_party")]
    public bool AutoAcceptParty { get; set; }

    [JsonPropertyName("auto_accept_dungeon")]
    public bool AutoAcceptDungeon { get; set; }

    [JsonPropertyName("auto_accept_delay")]
    public MinMax AutoAcceptDelay { get; set; }

    [JsonPropertyName("auto_switch_exchange")]
    public int AutoSwitchExchange { get; set; }

    [JsonPropertyName("hide_players")] public bool HidePlayers { get; set; }

    [JsonPropertyName("hide_players_except_mine")]
    public bool HidePlayersExceptMine { get; set; }

    [JsonPropertyName("hide_monsters")] public bool HideMonsters { get; set; }

    [JsonPropertyName("hide_npc")] public bool HideNpc { get; set; }

    [JsonPropertyName("auto_switch")] public bool AutoSwitch { get; set; }

    [JsonPropertyName("auto_switch_button")]
    public long AutoSwitchButton { get; set; }

    [JsonPropertyName("auto_switch_next")] public bool AutoSwitchNext { get; set; }

    [JsonPropertyName("auto_switch_next_button")]
    public long AutoSwitchNextButton { get; set; }

    [JsonPropertyName("auto_switch_previous")]
    public bool AutoSwitchPrevious { get; set; }

    [JsonPropertyName("auto_switch_previous_button")]
    public long AutoSwitchPreviousButton { get; set; }

    [JsonPropertyName("auto_follow")] public bool AutoFollow { get; set; }

    [JsonPropertyName("auto_follow_button")]
    public int AutoFollowButton { get; set; }

    [JsonPropertyName("auto_follow_delay")]
    public MinMax AutoFollowDelay { get; set; }

    [JsonPropertyName("auto_click")] public bool AutoClick { get; set; }

    [JsonPropertyName("auto_click_button")]
    public int AutoClickButton { get; set; }

    [JsonPropertyName("auto_invite")] public bool AutoInvite { get; set; }

    [JsonPropertyName("auto_invite_button")]
    public long AutoInviteButton { get; set; }

    [JsonPropertyName("toggle_fight_bot")] public bool ToggleFightBot { get; set; }

    [JsonPropertyName("toggle_fight_bot_button")]
    public long ToggleFightBotButton { get; set; }

    [JsonPropertyName("toggle_los_calculator")]
    public bool ToggleLosCalculator { get; set; }

    [JsonPropertyName("toggle_los_calculator_button")]
    public long ToggleLosCalculatorButton { get; set; }

    [JsonPropertyName("speed_animation")] public bool SpeedAnimation { get; set; }

    [JsonPropertyName("speed_animation_multiplier")]
    public double SpeedAnimationMultiplier { get; set; }

    [JsonPropertyName("toggle_tactical")] public int ToggleTactical { get; set; }

    [JsonPropertyName("disable_fight_popup")]
    public bool DisableFightPopup { get; set; }

    [JsonPropertyName("disable_level_up_popup")]
    public bool DisableLevelUpPopup { get; set; }

    [JsonPropertyName("auto_switch_turn")] public bool AutoSwitchTurn { get; set; }

    [JsonPropertyName("auto_switch_fight_end")]
    public bool AutoSwitchFightEnd { get; set; }

    [JsonPropertyName("auto_pass_turn")] public bool AutoPassTurn { get; set; }

    [JsonPropertyName("auto_pass_turn_delay")]
    public MinMax AutoPassTurnDelay { get; set; }

    [JsonPropertyName("auto_ready_type")] public int AutoReadyType { get; set; }

    [JsonPropertyName("auto_ready_delay")] public MinMax AutoReadyDelay { get; set; }

    [JsonPropertyName("auto_join")] public bool AutoJoin { get; set; }

    [JsonPropertyName("auto_join_delay")] public MinMax AutoJoinDelay { get; set; }

    [JsonPropertyName("block_spectators")] public bool BlockSpectators { get; set; }

    [JsonPropertyName("block_spectators_delay")]
    public MinMax BlockSpectatorsDelay { get; set; }

    [JsonPropertyName("block_fight_access")]
    public bool BlockFightAccess { get; set; }

    [JsonPropertyName("block_fight_access_delay")]
    public MinMax BlockFightAccessDelay { get; set; }

    [JsonPropertyName("auto_my_turn_los_calculator")]
    public bool AutoMyTurnLosCalculator { get; set; }

    [JsonPropertyName("disable_by_click_los_calculator")]
    public bool DisableByClickLosCalculator { get; set; }

    [JsonPropertyName("count_harebourg_simulator")]
    public bool CountHarebourgSimulator { get; set; }

    [JsonPropertyName("count_harebourg_simulator_target_button")]
    public long CountHarebourgSimulatorTargetButton { get; set; }

    [JsonPropertyName("count_harebourg_simulator_move_button")]
    public long CountHarebourgSimulatorMoveButton { get; set; }
}

public class FightSettings
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("kick_others")]
    public bool KickOthers { get; set; }

    [JsonPropertyName("kick_others_delay")]
    public MinMax KickOthersDelay { get; set; }

    [JsonPropertyName("placement")]
    public int Placement { get; set; }

    [JsonPropertyName("placement_delay")]
    public MinMax PlacementDelay { get; set; }

    [JsonPropertyName("play_turn_after")]
    public MinMax PlayTurnAfter { get; set; }

    [JsonPropertyName("resume_turn_after")]
    public MinMax ResumeTurnAfter { get; set; }

    [JsonPropertyName("pass_turn_after")]
    public MinMax PassTurnAfter { get; set; }

    [JsonPropertyName("casters")]
    public List<object> Casters { get; set; }
}

