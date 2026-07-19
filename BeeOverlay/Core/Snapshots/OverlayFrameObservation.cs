#nullable enable

using System.Collections.Generic;

namespace BeeOverlay.Core.Snapshots;

/// <summary>
/// Direct game observations captured together for one overlay update.
/// </summary>
internal sealed class OverlayFrameObservation
{
    public PlayerObservation? LocalPlayer { get; }

    public IReadOnlyList<BeeObservation> Bees { get; }

    public OverlayFrameObservation(
        PlayerObservation? localPlayer,
        IReadOnlyList<BeeObservation> bees
    )
    {
        LocalPlayer = localPlayer;
        Bees = bees;
    }
}

/// <summary>
/// Local-player positions with body and sight targets kept distinct.
/// </summary>
internal sealed class PlayerObservation
{
    public Vector3Value BodyPosition { get; }

    public Vector3Value SightTargetPosition { get; }

    public PlayerObservation(Vector3Value bodyPosition, Vector3Value sightTargetPosition)
    {
        BodyPosition = bodyPosition;
        SightTargetPosition = sightTargetPosition;
    }
}

/// <summary>
/// Direct observations for one bee without retaining Unity or base-game objects.
/// </summary>
internal sealed class BeeObservation
{
    public int Identity { get; }

    public Vector3Value EyePosition { get; }

    public HiveObservation? Hive { get; }

    public bool CanSeeLocalPlayer { get; }

    public BeeObservation(
        int identity,
        Vector3Value eyePosition,
        HiveObservation? hive,
        bool canSeeLocalPlayer
    )
    {
        Identity = identity;
        EyePosition = eyePosition;
        Hive = hive;
        CanSeeLocalPlayer = canSeeLocalPlayer;
    }
}

/// <summary>
/// Current-hive values and the direct pickup-proxy linecast observation.
/// </summary>
internal sealed class HiveObservation
{
    public Vector3Value Position { get; }

    public int DefenseDistance { get; }

    public bool PickupProxyLinecastBlocked { get; }

    public LastKnownHiveObservation LastKnownHive { get; }

    public HiveObservation(
        Vector3Value position,
        int defenseDistance,
        bool pickupProxyLinecastBlocked,
        LastKnownHiveObservation lastKnownHive
    )
    {
        Position = position;
        DefenseDistance = defenseDistance;
        PickupProxyLinecastBlocked = pickupProxyLinecastBlocked;
        LastKnownHive = lastKnownHive;
    }
}

/// <summary>
/// Remembered-hive values and direct observations used by the missing-hive probe.
/// </summary>
internal sealed class LastKnownHiveObservation
{
    public Vector3Value Position { get; }

    public bool LinecastBlocked { get; }

    public bool? IsPositionSynced { get; }

    public LastKnownHiveObservation(
        Vector3Value position,
        bool linecastBlocked,
        bool? isPositionSynced
    )
    {
        Position = position;
        LinecastBlocked = linecastBlocked;
        IsPositionSynced = isPositionSynced;
    }
}
