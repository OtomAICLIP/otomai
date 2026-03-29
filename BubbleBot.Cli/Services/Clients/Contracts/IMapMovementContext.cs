using Bubble.Core.Datacenter.Datacenter.WorldGraph;
using Bubble.Shared.Protocol;
using BubbleBot.Cli.Models;
using BubbleBot.Cli.Repository.Maps;
using BubbleBot.Cli.Services.Maps;

namespace BubbleBot.Cli.Services.Clients.Contracts;

public interface IMapMovementContext : IClientLogger
{
    CharacterInfo Info { get; set; }
    Map? Map { get; set; }
    WorldPath WorldPath { get; }
    List<WorldGraphEdge> AutoPath { get; set; }
    int AutoPathIndex { get; set; }
    long AutoPathEndMapId { get; set; }
    long PlayerId { get; }
    bool IsInFight { get; set; }

    Task SendRequestWithDelay(IProtoMessage             message,
                              string                    typeUrl,
                              int                       delay,
                              Predicate<IProtoMessage>? predicate = null);

    void SendRequest(IProtoMessage message, string typeUrl, bool setUid = false);
    void UpdateCharacterInfoFrom(EntityLook                                                         look,
                                 ActorPositionInformation.ActorInformation.RolePlayActor.NamedActor namedActorValue,
                                 EntityDisposition                                                  actorDisposition);
    void OnCellChanged();
    void OnCellPathNotFound();
    void ResetWorldPath();
}
