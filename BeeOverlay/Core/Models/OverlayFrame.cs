#nullable enable

using System.Collections.Generic;
using BeeOverlay.Core.Snapshots;

namespace BeeOverlay.Core.Models;

/// <summary>
/// Immutable diagnostic model consumed by every presentation path for one update.
/// </summary>
internal sealed class OverlayFrame
{
    public IReadOnlyList<BeeDiagnostic> Bees { get; }

    public OverlayFrame(IReadOnlyList<BeeDiagnostic> bees)
    {
        Bees = bees;
    }
}

/// <summary>
/// One bee observation plus the conditions derived from that same observation.
/// </summary>
internal sealed class BeeDiagnostic
{
    public BeeObservation Observation { get; }

    public PlayerObservation? LocalPlayer { get; }

    public int DisplayNumber { get; }

    public float? BeeToPlayerDistance { get; }

    public HiveDiagnostic? Hive { get; }

    public BeeDiagnostic(
        BeeObservation observation,
        PlayerObservation? localPlayer,
        int displayNumber,
        float? beeToPlayerDistance,
        HiveDiagnostic? hive
    )
    {
        Observation = observation;
        LocalPlayer = localPlayer;
        DisplayNumber = displayNumber;
        BeeToPlayerDistance = beeToPlayerDistance;
        Hive = hive;
    }
}

/// <summary>
/// Current-hive observation and all conditions derived for its presentation.
/// </summary>
internal sealed class HiveDiagnostic
{
    public HiveObservation Observation { get; }

    public float? PlayerToHiveDistance { get; }

    public HiveSightProbe Sight { get; }

    public HiveMissingProbe Missing { get; }

    public HiveDiagnostic(
        HiveObservation observation,
        float? playerToHiveDistance,
        HiveSightProbe sight,
        HiveMissingProbe missing
    )
    {
        Observation = observation;
        PlayerToHiveDistance = playerToHiveDistance;
        Sight = sight;
        Missing = missing;
    }
}

/// <summary>
/// Diagnostic pickup-position proxy derived from a direct linecast observation.
/// </summary>
internal sealed class HiveSightProbe
{
    public Vector3Value HivePosition { get; }

    public float EyeToHiveDistance { get; }

    public bool LinecastBlocked { get; }

    public bool WithinPlayerSightRange { get; }

    // This is the HUD/line color decision for the pickup proxy. It deliberately combines range
    // and linecast so the player sees one practical answer: "would this hive point stand in for
    // a visible player under the base game's 16u player sight rule?"
    public bool CanSeePickupProxy => WithinPlayerSightRange && !LinecastBlocked;

    public HiveSightProbe(
        Vector3Value hivePosition,
        float eyeToHiveDistance,
        bool linecastBlocked,
        bool withinPlayerSightRange
    )
    {
        HivePosition = hivePosition;
        EyeToHiveDistance = eyeToHiveDistance;
        LinecastBlocked = linecastBlocked;
        WithinPlayerSightRange = withinPlayerSightRange;
    }
}

/// <summary>
/// Spatial and synchronization subset of the base game's missing-hive decision.
/// </summary>
internal sealed class HiveMissingProbe
{
    public Vector3Value LastKnownHivePosition { get; }

    public float EyeToLastKnownHiveDistance { get; }

    public bool LinecastBlocked { get; }

    public bool NearTrigger { get; }

    public bool LineOfSightTrigger { get; }

    public bool CanEvaluateMissing { get; }

    // Null means the private sync field was not readable. That is intentionally different from
    // false, because false is an actual base-game gate that suppresses the missing-hive branch.
    public bool? SyncedLastKnownHivePosition { get; }

    public HiveMissingProbe(
        Vector3Value lastKnownHivePosition,
        float eyeToLastKnownHiveDistance,
        bool linecastBlocked,
        bool nearTrigger,
        bool lineOfSightTrigger,
        bool canEvaluateMissing,
        bool? syncedLastKnownHivePosition
    )
    {
        LastKnownHivePosition = lastKnownHivePosition;
        EyeToLastKnownHiveDistance = eyeToLastKnownHiveDistance;
        LinecastBlocked = linecastBlocked;
        NearTrigger = nearTrigger;
        LineOfSightTrigger = lineOfSightTrigger;
        CanEvaluateMissing = canEvaluateMissing;
        SyncedLastKnownHivePosition = syncedLastKnownHivePosition;
    }
}
