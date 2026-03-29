using Serilog;

namespace BubbleBot.Cli.Services.Maps;

public class HavenBag
{
    private readonly BotGameClient _client;
    public HavenBagEnterReason Reason { get; set; } = HavenBagEnterReason.NoReason;

    public HavenBag(BotGameClient client)
    {
        _client = client;
    }

    public void EnterHavenBag(HavenBagEnterReason reason)
    {
        if (_client.Map == null || _client.MapCurrentEvent == null)
            return;

        Reason = reason;
        _client.LogInfo("On demande d'entrer dans l'havre sac avec la raison: {reason}", Reason);

        _client.AutoPathEndMapId = -1;

        if (_client.Map.IsHavenBag)
        {
            OnEnterHavenBag(_client.MapCurrentEvent);
            return;
        }

        _client.SendRequest(new HavenBagEnterRequest
                            {
                                Owner = _client.PlayerId
                            },
                            HavenBagEnterRequest.TypeUrl);
    }

    public void OnEnterHavenBag(MapComplementaryInformationEvent message)
    { 
        _client.NeedToTakeHavenBagAsSoonAsPossible = false;

        _client.LogInfo("Enter HavenBag with reason {reason}", Reason);
        if (Reason == HavenBagEnterReason.LeaveInstant)
        {
            _client.LeaveHavenBagAsSoonAsPossible = true;
            return;
        }
        
        if (Reason != HavenBagEnterReason.NoReason)
        {
            _client.TeleportDestinationData.Reason = Reason switch
            {
                HavenBagEnterReason.TakeHunt      => TeleportDestinationOpenedReason.GoToHuntHouse,
                HavenBagEnterReason.GoToFirstStep => TeleportDestinationOpenedReason.GoToFirstStep,
                HavenBagEnterReason.Empty         => TeleportDestinationOpenedReason.Empty,
                HavenBagEnterReason.BuyStuff      => TeleportDestinationOpenedReason.BuyStuff,
                HavenBagEnterReason.CreateGuild   => TeleportDestinationOpenedReason.CreateGuild,
                HavenBagEnterReason.GoToTrajet    => TeleportDestinationOpenedReason.GoToTrajet,
                _                                 => _client.TeleportDestinationData.Reason
            };
            

            var interactive = message.InteractiveElements.First(x => x.ElementTypeId == 16);
            _ = _client.SendRequestWithDelay(new InteractiveUseRequest
                                             {
                                                 ElementId = interactive.ElementId,
                                                 SkillInstanceUid = interactive.EnabledSkills.First().SkillInstanceUid
                                             },
                                             InteractiveUseRequest.TypeUrl,
                                             1500);
        }

        Reason = HavenBagEnterReason.NoReason;
    }

    public void LeaveHavenBag()
    {
        _client.SendRequest(new HavenBagExitRequest(), HavenBagExitRequest.TypeUrl);
    }
}