#nullable enable

extern alias LethalCompany;
extern alias UnityEngine;

using System;
using BeeOverlay.Core;
using BeeOverlay.Core.Ports;
using BeeOverlay.Core.Snapshots;
using BepInEx.Logging;
using HarmonyLib;
using LethalCompany;
using PlayerControllerB = LethalCompany::GameNetcodeStuff.PlayerControllerB;
using UnityEngine::UnityEngine;
using UnityObject = UnityEngine::UnityEngine.Object;

namespace BeeOverlay.Interop.Game;

/// <summary>
/// Samples live bee and player objects once and converts them to Core observations.
/// </summary>
internal sealed class BeeObservationSource : IOverlayObservationSource
{
    private const float BeeEyeFallbackYOffset = 0.35f;

    private readonly AccessTools.FieldRef<RedLocustBees, bool>? syncedLastKnownHivePositionRef;

    public BeeObservationSource(ManualLogSource logger)
    {
        syncedLastKnownHivePositionRef = CreateSyncedLastKnownHivePositionRef(logger);
    }

    public OverlayFrameObservation Capture()
    {
        var bees = UnityObject.FindObjectsOfType<RedLocustBees>();
        var localPlayer = GameNetworkManager.Instance != null
            ? GameNetworkManager.Instance.localPlayerController
            : null;
        var playerObservation = CreatePlayerObservation(localPlayer);
        var observations = new BeeObservation[bees.Length];

        for (var i = 0; i < bees.Length; i++)
        {
            observations[i] = CaptureBee(bees[i], localPlayer);
        }

        return new OverlayFrameObservation(localPlayer: playerObservation, bees: observations);
    }

    private BeeObservation CaptureBee(RedLocustBees bee, PlayerControllerB? localPlayer)
    {
        var beePosition = bee.transform.position;
        var beeEyePosition = bee.eye != null
            ? bee.eye.position
            : beePosition + Vector3.up * BeeEyeFallbackYOffset;
        if (bee.hive == null)
        {
            return new BeeObservation(
                identity: bee.thisEnemyIndex,
                eyePosition: FromUnityVector3(beeEyePosition),
                hive: null,
                canSeeLocalPlayer: false
            );
        }

        var hivePosition = bee.hive.transform.position;
        var visiblePlayer = bee.CheckLineOfSightForPlayer(
            360f,
            (int)OverlayRules.PlayerLineOfSightDistance,
            1
        );
        var lastKnownHivePosition = bee.lastKnownHivePosition;
        var lastKnownHive = new LastKnownHiveObservation(
            position: FromUnityVector3(lastKnownHivePosition),
            linecastBlocked: IsLinecastBlocked(
                start: beeEyePosition,
                end: lastKnownHivePosition
            ),
            isPositionSynced: GetSyncedLastKnownHivePosition(bee)
        );
        var hive = new HiveObservation(
            position: FromUnityVector3(hivePosition),
            defenseDistance: bee.defenseDistance,
            pickupProxyLinecastBlocked: IsLinecastBlocked(
                start: beeEyePosition,
                end: hivePosition
            ),
            lastKnownHive: lastKnownHive
        );

        return new BeeObservation(
            identity: bee.thisEnemyIndex,
            eyePosition: FromUnityVector3(beeEyePosition),
            hive: hive,
            canSeeLocalPlayer: visiblePlayer != null && visiblePlayer == localPlayer
        );
    }

    private static PlayerObservation? CreatePlayerObservation(PlayerControllerB? player)
    {
        if (player == null)
        {
            return null;
        }

        // State 0 -> 1 checks the player's body against defenseDistance, not the camera. Keeping a
        // separate body helper avoids accidentally reusing the camera target from the visibility
        // line and overstating whether the player is actually inside the hive defense radius.
        var bodyPosition = player.transform.position;

        // Match the player's camera when available because the bee line-of-sight check is about
        // whether the bee can see the player, not merely where the player's feet are on the floor.
        var sightTargetPosition = player.gameplayCamera != null
            ? player.gameplayCamera.transform.position
            : bodyPosition + Vector3.up * 1.6f;
        return new PlayerObservation(
            bodyPosition: FromUnityVector3(bodyPosition),
            sightTargetPosition: FromUnityVector3(sightTargetPosition)
        );
    }

    private static bool IsLinecastBlocked(Vector3 start, Vector3 end)
    {
        return StartOfRound.Instance == null || Physics.Linecast(
            start,
            end,
            StartOfRound.Instance.collidersAndRoomMaskAndDefault,
            QueryTriggerInteraction.Ignore
        );
    }

    private bool? GetSyncedLastKnownHivePosition(RedLocustBees bee)
    {
        // Harmony's FieldRef keeps the private base-game sync flag read-only from this mod. A null
        // result means "could not inspect", not "false"; callers preserve that distinction so the
        // overlay does not pretend to know a transition gate it could not read.
        return syncedLastKnownHivePositionRef != null
            ? syncedLastKnownHivePositionRef(bee)
            : null;
    }

    private static AccessTools.FieldRef<RedLocustBees, bool>?
        CreateSyncedLastKnownHivePositionRef(ManualLogSource logger)
    {
        try
        {
            return AccessTools.FieldRefAccess<RedLocustBees, bool>(
                "syncedLastKnownHivePosition"
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                $"Could not access RedLocustBees.syncedLastKnownHivePosition: {ex.Message}"
            );
            return null;
        }
    }

    private static Vector3Value FromUnityVector3(Vector3 value)
    {
        return new Vector3Value(x: value.x, y: value.y, z: value.z);
    }
}
