using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Robust.Client.Player;

public interface IPlayerManager : ISharedPlayerManager
{
    /// <summary>
    /// Invoked when the list of sessions/players gets updated.
    /// </summary>
    event Action? PlayerListUpdated;

    /// <summary>
    /// Invoked when <see cref="ISharedPlayerManager.LocalSession"/> gets attached to a new entity. See also <see cref="LocalPlayerAttachedEvent"/>
    /// </summary>
    event Action<EntityUid>? LocalPlayerAttached;

    /// <summary>
    /// Invoked when <see cref="ISharedPlayerManager.LocalSession"/> gets detached from new entity. See also <see cref="LocalPlayerDetachedEvent"/>
    /// </summary>
    event Action<EntityUid>? LocalPlayerDetached;

    void ApplyPlayerStates(IReadOnlyCollection<SessionState> list);

    /// <summary>
    /// Sets up a single player game. This creates a dummy <see cref="ISharedPlayerManager.LocalSession"/>  without an
    /// <see cref="INetChannel"/>.
    /// </summary>
    void SetupSinglePlayer(string name);

    /// <summary>
    /// Sets up the manager for a multiplayer game. This creates a <see cref="ISharedPlayerManager.LocalSession"/>
    /// using the given <see cref="INetChannel"/>.
    /// </summary>
    void SetupMultiplayer(INetChannel channel);

    [Obsolete("Use LocalSession instead")]
    LocalPlayer? LocalPlayer { get;}
}

/// <summary>
/// ECS event that gets raised when the local player gets attached to a new entity. The event is both broadcast and
/// raised directed at the new entity.
/// </summary>
public sealed class LocalPlayerAttachedEvent : EntityEventArgs
{
    public LocalPlayerAttachedEvent(EntityUid entity)
    {
        Entity = entity;
    }

    public EntityUid Entity { get; }
}

/// <summary>
/// ECS event that gets raised when the local player gets detached from an entity. The event is both broadcast and
/// raised directed at the new entity.
/// </summary>
public sealed class LocalPlayerDetachedEvent : EntityEventArgs
{
    public LocalPlayerDetachedEvent(EntityUid entity)
    {
        Entity = entity;
    }

    public EntityUid Entity { get; }
}