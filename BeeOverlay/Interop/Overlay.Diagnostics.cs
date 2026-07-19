#nullable enable

extern alias LethalCompany;
extern alias UnityEngine;

using System;
using System.Collections.Generic;
using HarmonyLib;
using LethalCompany;
using UnityEngine::UnityEngine;
using PlayerControllerB = LethalCompany::GameNetcodeStuff.PlayerControllerB;

namespace BeeOverlay.Interop;

internal sealed partial class Overlay
{
    private void DrawBee(RedLocustBees bee, HashSet<int> seen)
    {
        if (bee == null || bee.hive == null)
        {
            return;
        }

        var view = GetView(bee.thisEnemyIndex);
        var beePosition = bee.transform.position;
        var hive = bee.hive.transform.position;
        var beeEyePosition = bee.eye != null ? bee.eye.position : beePosition + Vector3.up * WorldYOffset;
        var visiblePlayer = bee.CheckLineOfSightForPlayer(360f, (int)PlayerLineOfSightDistance, 1);
        var localPlayer = GameNetworkManager.Instance != null ? GameNetworkManager.Instance.localPlayerController : null;
        var localPlayerPosition = GetPlayerSightTargetPosition(localPlayer);
        var canSeeLocalPlayer = visiblePlayer != null && visiblePlayer == localPlayer;
        // Keep both probes even though they look similar in the scene. bee-hive is the pickup
        // position proxy for state 0 -> 1 reasoning, while lastKnownHive is the remembered-position
        // probe used by the state 0 -> 2 missing-hive path.
        var hiveMissingProbe = GetHiveMissingProbe(bee, beeEyePosition);
        var hiveSightProbe = GetHiveSightProbe(beeEyePosition, hive);

        view.SetSpatialGuides(
            hive + Vector3.up * WorldYOffset,
            bee.defenseDistance,
            beeEyePosition,
            localPlayerPosition,
            canSeeLocalPlayer,
            hiveMissingProbe,
            hiveSightProbe
        );
        seen.Add(bee.thisEnemyIndex);
    }

    private static Vector3? GetPlayerSightTargetPosition(PlayerControllerB? player)
    {
        if (player == null)
        {
            return null;
        }

        // Match the player's camera when available because the bee line-of-sight check is about
        // whether the bee can see the player, not merely where the player's feet are on the floor.
        if (player.gameplayCamera != null)
        {
            return player.gameplayCamera.transform.position;
        }

        return player.transform.position + Vector3.up * 1.6f;
    }

    private static Vector3? GetPlayerBodyPosition(PlayerControllerB? player)
    {
        // State 0 -> 1 checks the player's body against defenseDistance, not the camera. Keeping a
        // separate body helper avoids accidentally reusing the camera target from the visibility
        // line and overstating whether the player is actually inside the hive defense radius.
        return player != null ? player.transform.position : null;
    }

    private static string GetBeeStatusLine(
        RedLocustBees bee,
        int displayBeeNumber,
        PlayerControllerB? localPlayer,
        Vector3? localPlayerPosition
    )
    {
        if (bee == null)
        {
            return $"{Tag("bee:n/a", BeeColor)}";
        }

        if (bee.hive == null)
        {
            return $"{Tag($"bee:{displayBeeNumber}", BeeColor)}  hive n/a";
        }

        var beeEyePosition = bee.eye != null ? bee.eye.position : bee.transform.position + Vector3.up * WorldYOffset;
        var hivePosition = bee.hive.transform.position;
        var visiblePlayer = bee.CheckLineOfSightForPlayer(360f, (int)PlayerLineOfSightDistance, 1);
        var canSeeLocalPlayer = localPlayer != null && visiblePlayer != null && visiblePlayer == localPlayer;
        var playerToHiveDistance = localPlayerPosition.HasValue
            ? Vector3.Distance(localPlayerPosition.Value, hivePosition)
            : (float?)null;
        var beeToPlayerDistance = localPlayerPosition.HasValue
            ? Vector3.Distance(beeEyePosition, localPlayerPosition.Value)
            : (float?)null;
        var hiveMissingProbe = GetHiveMissingProbe(bee, beeEyePosition);
        var hiveSightProbe = GetHiveSightProbe(beeEyePosition, hivePosition);

        // HUD rows intentionally avoid transition-derived labels such as missProbe. In practice the
        // player needs quick distances and current game visibility booleans, not another copy of
        // the C# branch structure. The colored terms map to the same entity colors as the 3D
        // dots/lines so the player can glance between HUD and world probes.
        return string.Join(
            "  ",
            Tag($"bee:{displayBeeNumber}", BeeColor),
            Tag($"bee-player={FmtDistance(beeToPlayerDistance)}/{SeenBlocked(canSeeLocalPlayer)}", PlayerColor),
            Tag($"hive-player={FmtDistance(playerToHiveDistance)}/{InsideOutside(playerToHiveDistance, bee.defenseDistance)}", HiveColor),
            Tag($"bee-hive={hiveSightProbe.EyeToHiveDistance:F2}u/{SeenBlocked(hiveSightProbe.CanSeePickupProxy)}", PickupProxyColor),
            Tag(
                $"bee-knownHive={hiveMissingProbe.EyeToLastKnownHiveDistance:F2}u/{SeenBlocked(!hiveMissingProbe.LinecastBlocked)}",
                LastKnownHiveColor
            )
        );
    }

    private static HiveSightProbe GetHiveSightProbe(Vector3 beeEyePosition, Vector3 hivePosition)
    {
        var eyeToHiveDistance = Vector3.Distance(beeEyePosition, hivePosition);
        // The hive ray is a pickup-position proxy, not a general-purpose hive visibility check.
        // Match the base game's player sight range so SEEN only means "the bee could have seen a
        // player standing at the hive pickup point" under the same 16u gate as state 0 -> state 1.
        var withinPlayerSightRange = eyeToHiveDistance < PlayerLineOfSightDistance;
        var linecastBlocked = StartOfRound.Instance == null || Physics.Linecast(
            beeEyePosition,
            hivePosition,
            StartOfRound.Instance.collidersAndRoomMaskAndDefault,
            QueryTriggerInteraction.Ignore
        );
        return new HiveSightProbe(hivePosition, eyeToHiveDistance, linecastBlocked, withinPlayerSightRange);
    }

    private static HiveMissingProbe GetHiveMissingProbe(RedLocustBees bee, Vector3 beeEyePosition)
    {
        var lastKnownHivePosition = bee.lastKnownHivePosition;
        var eyeToLastKnownHiveDistance = Vector3.Distance(beeEyePosition, lastKnownHivePosition);
        var linecastBlocked = StartOfRound.Instance == null || Physics.Linecast(
            beeEyePosition,
            lastKnownHivePosition,
            StartOfRound.Instance.collidersAndRoomMaskAndDefault,
            QueryTriggerInteraction.Ignore
        );
        var synced = GetSyncedLastKnownHivePosition(bee);
        var nearTrigger = eyeToLastKnownHiveDistance < HiveMissingNearDistance;
        var lineOfSightTrigger = eyeToLastKnownHiveDistance < HiveMissingLineOfSightDistance && !linecastBlocked;

        // This mirrors only the spatial/sync gate inside IsHiveMissing(). The hive-held branch is
        // intentionally not displayed per the current investigation goal, and the actual transition
        // still depends on the base game's hive state checks.
        var canEvaluateMissing = synced != false && (nearTrigger || lineOfSightTrigger);
        return new HiveMissingProbe(
            lastKnownHivePosition,
            eyeToLastKnownHiveDistance,
            linecastBlocked,
            nearTrigger,
            lineOfSightTrigger,
            canEvaluateMissing,
            synced
        );
    }

    private static bool? GetSyncedLastKnownHivePosition(RedLocustBees bee)
    {
        // Harmony's FieldRef keeps the private base-game sync flag read-only from this mod. A null
        // result means "could not inspect", not "false"; callers preserve that distinction so the
        // overlay does not pretend to know a transition gate it could not read.
        return SyncedLastKnownHivePositionRef != null ? SyncedLastKnownHivePositionRef(bee) : null;
    }

    private static AccessTools.FieldRef<RedLocustBees, bool>? CreateSyncedLastKnownHivePositionRef()
    {
        try
        {
            return AccessTools.FieldRefAccess<RedLocustBees, bool>("syncedLastKnownHivePosition");
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogWarning($"Could not access RedLocustBees.syncedLastKnownHivePosition: {ex.Message}");
            return null;
        }
    }

    private static string FmtDistance(float? distance)
    {
        return distance.HasValue ? $"{distance.Value:F2}u" : "n/a";
    }

    private static string SeenBlocked(bool canSee)
    {
        return canSee ? "SEEN" : "blocked";
    }

    private static string InsideOutside(float? distance, float radius)
    {
        if (!distance.HasValue || radius <= 0f)
        {
            return "n/a";
        }

        return distance.Value < radius ? "INSIDE" : "outside";
    }

    private static string Tag(string text, Color color)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>";
    }

    private readonly struct HiveMissingProbe
    {
        public HiveMissingProbe(
            Vector3 lastKnownHivePosition,
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

        public Vector3 LastKnownHivePosition { get; }

        public float EyeToLastKnownHiveDistance { get; }

        public bool LinecastBlocked { get; }

        public bool NearTrigger { get; }

        public bool LineOfSightTrigger { get; }

        public bool CanEvaluateMissing { get; }

        // Null means the private sync field was not readable. That is intentionally different from
        // false, because false is an actual base-game gate that suppresses the missing-hive branch.
        public bool? SyncedLastKnownHivePosition { get; }
    }

    private readonly struct HiveSightProbe
    {
        public HiveSightProbe(
            Vector3 hivePosition,
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

        public Vector3 HivePosition { get; }

        public float EyeToHiveDistance { get; }

        public bool LinecastBlocked { get; }

        public bool WithinPlayerSightRange { get; }

        // This is the HUD/line color decision for the pickup proxy. It deliberately combines range
        // and linecast so the player sees one practical answer: "would this hive point stand in for
        // a visible player under the base game's 16u player sight rule?"
        public bool CanSeePickupProxy => WithinPlayerSightRange && !LinecastBlocked;
    }

}
