#nullable enable

using System.Linq;
using BeeOverlay.Core.Models;
using BeeOverlay.Core.Snapshots;

namespace BeeOverlay.Core.UseCases;

/// <summary>
/// Derives every HUD and world-guide value from one coherent game observation.
/// </summary>
internal sealed class BuildOverlayFrameUseCase
{
    public OverlayFrame Execute(OverlayFrameObservation frameObservation)
    {
        // Stable ordering makes the top-left status useful for screenshots and frame-to-frame
        // comparison. Unity's object enumeration order is not a good identity signal by itself.
        var orderedBees = frameObservation.Bees.OrderBy(bee => bee.Identity).ToArray();
        var diagnostics = new BeeDiagnostic[orderedBees.Length];

        for (var i = 0; i < orderedBees.Length; i++)
        {
            diagnostics[i] = BuildBeeDiagnostic(
                observation: orderedBees[i],
                localPlayer: frameObservation.LocalPlayer,
                displayNumber: i + 1
            );
        }

        return new OverlayFrame(diagnostics);
    }

    private static BeeDiagnostic BuildBeeDiagnostic(
        BeeObservation observation,
        PlayerObservation? localPlayer,
        int displayNumber
    )
    {
        if (observation.Hive == null)
        {
            return new BeeDiagnostic(
                observation: observation,
                localPlayer: localPlayer,
                displayNumber: displayNumber,
                beeToPlayerDistance: null,
                hive: null
            );
        }

        var hive = observation.Hive;
        var beeToPlayerDistance = localPlayer != null
            ? observation.EyePosition.DistanceTo(localPlayer.BodyPosition)
            : (float?)null;
        var playerToHiveDistance = localPlayer != null
            ? localPlayer.BodyPosition.DistanceTo(hive.Position)
            : (float?)null;
        var eyeToHiveDistance = observation.EyePosition.DistanceTo(hive.Position);
        // The hive ray is a pickup-position proxy, not a general-purpose hive visibility check.
        // Match the base game's player sight range so SEEN only means "the bee could have seen a
        // player standing at the hive pickup point" under the same 16u gate as state 0 -> state 1.
        var hiveSight = new HiveSightProbe(
            hivePosition: hive.Position,
            eyeToHiveDistance: eyeToHiveDistance,
            linecastBlocked: hive.PickupProxyLinecastBlocked,
            withinPlayerSightRange: eyeToHiveDistance < OverlayRules.PlayerLineOfSightDistance
        );

        var lastKnownHive = hive.LastKnownHive;
        var eyeToLastKnownHiveDistance = observation.EyePosition.DistanceTo(
            lastKnownHive.Position
        );
        var nearTrigger = eyeToLastKnownHiveDistance < OverlayRules.HiveMissingNearDistance;
        var lineOfSightTrigger =
            eyeToLastKnownHiveDistance < OverlayRules.HiveMissingLineOfSightDistance
            && !lastKnownHive.LinecastBlocked;

        // This mirrors only the spatial/sync gate inside IsHiveMissing(). The hive-held branch is
        // intentionally not displayed per the current investigation goal, and the actual transition
        // still depends on the base game's hive state checks.
        var canEvaluateMissing =
            lastKnownHive.IsPositionSynced != false && (nearTrigger || lineOfSightTrigger);
        var hiveMissing = new HiveMissingProbe(
            lastKnownHivePosition: lastKnownHive.Position,
            eyeToLastKnownHiveDistance: eyeToLastKnownHiveDistance,
            linecastBlocked: lastKnownHive.LinecastBlocked,
            nearTrigger: nearTrigger,
            lineOfSightTrigger: lineOfSightTrigger,
            canEvaluateMissing: canEvaluateMissing,
            syncedLastKnownHivePosition: lastKnownHive.IsPositionSynced
        );

        return new BeeDiagnostic(
            observation: observation,
            localPlayer: localPlayer,
            displayNumber: displayNumber,
            beeToPlayerDistance: beeToPlayerDistance,
            hive: new HiveDiagnostic(
                observation: hive,
                playerToHiveDistance: playerToHiveDistance,
                sight: hiveSight,
                missing: hiveMissing
            )
        );
    }
}
