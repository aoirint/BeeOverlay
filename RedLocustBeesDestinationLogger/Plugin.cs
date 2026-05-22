#nullable enable

extern alias LethalCompany;
extern alias UnityEngine;

using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LethalCompany;
using UnityEngine::UnityEngine;
using UnityEngine::UnityEngine.AI;
using PlayerControllerB = LethalCompany::GameNetcodeStuff.PlayerControllerB;
using UiImage = LethalCompany::UnityEngine.UI.Image;
using UiText = LethalCompany::UnityEngine.UI.Text;
using UnityObject = UnityEngine::UnityEngine.Object;

namespace RedLocustBeesDestinationLogger;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("Lethal Company.exe")]
public sealed class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource? Log { get; private set; }

    private Harmony? harmony;

    private void Awake()
    {
        Log = Logger;
        Overlay.Instance = new Overlay();
        harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll(typeof(Plugin).Assembly);
        Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} loaded.");
    }
}

internal sealed class Overlay
{
    // The overlay is intentionally color-coded by semantic role instead of object type names in
    // Unity. The target debugging question is whether the live NavMesh destination sits far enough
    // from the hive, so destination and hive need to remain visually distinct at a glance.
    private static readonly Color BeeColor = new(0.2f, 0.55f, 1f, 0.95f);
    private static readonly Color HiveColor = new(0.15f, 1f, 0.25f, 0.95f);
    private static readonly Color DestinationColor = new(1f, 0.15f, 0.1f, 0.95f);
    private static readonly Color LineColor = new(1f, 0.85f, 0.1f, 0.95f);
    private static readonly Color ThresholdLineColor = new(1f, 0.1f, 0.05f, 0.95f);
    private static readonly Color DefenseDistanceColor = new(0.1f, 0.75f, 1f, 0.7f);
    private static readonly Color SightLineColor = new(0.65f, 1f, 1f, 0.95f);
    private static readonly Color LastKnownHiveColor = new(0.85f, 0.35f, 1f, 0.95f);
    private static readonly Color HiveMissingNearColor = new(1f, 0.45f, 0.05f, 0.85f);
    private static readonly Color HiveMissingLineOfSightColor = new(0.65f, 0.35f, 1f, 0.65f);
    private static readonly Color HiveMissingActiveLineColor = new(1f, 0.2f, 0.05f, 0.95f);
    private static readonly Color HiveMissingInactiveLineColor = new(0.45f, 0.45f, 0.5f, 0.65f);
    private static readonly Color HiveVisibleLineColor = new(0.25f, 1f, 0.35f, 0.95f);
    private static readonly Color HiveBlockedLineColor = new(0.35f, 0.4f, 0.38f, 0.65f);

    // Four Unity units is the investigation threshold from the observed bee return behavior.
    // Keep it near the overlay colors because it drives both the status text and the warning line.
    private const float DestinationToHiveThresholdDistance = 4f;
    private const float PlayerLineOfSightDistance = 16f;
    private const float HiveMissingNearDistance = 4f;
    private const float HiveMissingLineOfSightDistance = 8f;
    private const int DefenseDistanceCircleSegments = 96;

    // Markers are lifted a little above the sampled positions so they are not hidden by terrain,
    // hive meshes, or the bee body while still representing the same horizontal point.
    private const float WorldYOffset = 0.35f;
    private const float UiMarkerSize = 12f;
    private const float UiLineThickness = 3f;

    private readonly Dictionary<int, BeeView> views = new();
    private readonly Font? font = Resources.GetBuiltinResource<Font>("Arial.ttf");

    // LineRenderer vertex colors are updated per segment every frame. A white material avoids
    // tinting those runtime colors and lets the normal/warning colors match the HUD lines.
    private readonly Material worldLineMaterial = CreateMaterial(Color.white);
    private readonly Material beeMaterial = CreateMaterial(BeeColor);
    private readonly Material hiveMaterial = CreateMaterial(HiveColor);
    private readonly Material destinationMaterial = CreateMaterial(DestinationColor);
    private readonly Material lastKnownHiveMaterial = CreateMaterial(LastKnownHiveColor);

    private static readonly AccessTools.FieldRef<RedLocustBees, bool>? SyncedLastKnownHivePositionRef =
        CreateSyncedLastKnownHivePositionRef();

    private RectTransform? hudRoot;
    private UiText? statusText;
    private Transform? attachedHudContainer;
    private float nextWaitingLogTime;

    public static Overlay? Instance { get; set; }

    public void Tick()
    {
        if (!TryEnsureHudRoot())
        {
            HideAll();
            LogWaitingForHud();
            return;
        }

        if (!TryGetGameplayCamera(out var camera))
        {
            HideAll();
            SetStatus("RLB destination overlay | waiting for gameplay camera");
            return;
        }

        var bees = UnityObject.FindObjectsOfType<RedLocustBees>();

        // Stable ordering makes the top-left status useful for screenshots and frame-to-frame
        // comparison. Unity's object enumeration order is not a good identity signal by itself.
        Array.Sort(bees, static (left, right) => left.thisEnemyIndex.CompareTo(right.thisEnemyIndex));

        var seen = new HashSet<int>();
        var statusBuilder = new StringBuilder();
        statusBuilder.Append($"RLB destination overlay | bees={bees.Length}");
        foreach (var bee in bees)
        {
            DrawBee(camera, bee, seen);
            statusBuilder.AppendLine();
            statusBuilder.Append(GetBeeStatusLine(bee));
        }

        foreach (var pair in views)
        {
            if (!seen.Contains(pair.Key))
            {
                pair.Value.SetVisible(false);
            }
        }

        // The status text is rebuilt from the current frame instead of cached so stale bee rows
        // disappear immediately when a bee despawns or no longer has readable navigation data.
        SetStatus(statusBuilder.ToString());
    }

    private bool TryEnsureHudRoot()
    {
        var hudContainer = HUDManager.Instance != null ? HUDManager.Instance.HUDContainer : null;
        if (hudContainer == null)
        {
            return false;
        }

        if (hudRoot != null && attachedHudContainer == hudContainer.transform)
        {
            return true;
        }

        // The HUD container can be recreated across scene transitions. Rebuild this overlay when
        // the parent changes so RectTransforms do not stay attached to a destroyed canvas.
        if (hudRoot != null)
        {
            UnityObject.Destroy(hudRoot.gameObject);
        }

        views.Clear();
        attachedHudContainer = hudContainer.transform;

        var rootObject = new GameObject("RedLocustBeesDestinationOverlay", typeof(RectTransform));
        rootObject.transform.SetParent(attachedHudContainer, false);
        hudRoot = rootObject.GetComponent<RectTransform>();
        Stretch(hudRoot);

        var statusObject = new GameObject("Status", typeof(RectTransform), typeof(UiText));
        statusObject.transform.SetParent(rootObject.transform, false);
        var statusRect = statusObject.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 1f);
        statusRect.anchorMax = new Vector2(0f, 1f);
        statusRect.pivot = new Vector2(0f, 1f);
        statusRect.anchoredPosition = new Vector2(16f, -16f);

        // Reserve enough room for several bees. Overflow remains enabled below because this is a
        // diagnostic overlay and clipped lines are worse than imperfect layout in edge cases.
        statusRect.sizeDelta = new Vector2(1040f, 320f);

        statusText = statusObject.GetComponent<UiText>();
        statusText.font = font;
        statusText.fontSize = 15;
        statusText.fontStyle = FontStyle.Bold;
        statusText.alignment = TextAnchor.UpperLeft;
        statusText.color = LineColor;
        statusText.raycastTarget = false;
        statusText.horizontalOverflow = HorizontalWrapMode.Overflow;
        statusText.verticalOverflow = VerticalWrapMode.Overflow;

        Plugin.Log?.LogInfo($"Overlay attached to HUDContainer='{hudContainer.name}'.");
        return true;
    }

    private void DrawBee(Camera camera, RedLocustBees bee, HashSet<int> seen)
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
        var visiblePlayerPosition = GetPlayerSightTargetPosition(visiblePlayer);
        var hiveMissingProbe = GetHiveMissingProbe(bee, beeEyePosition);
        var hiveSightProbe = GetHiveSightProbe(beeEyePosition, hive);

        view.SetSpatialGuides(
            hive + Vector3.up * WorldYOffset,
            bee.defenseDistance,
            beeEyePosition,
            visiblePlayerPosition,
            hiveMissingProbe,
            hiveSightProbe
        );
        seen.Add(bee.thisEnemyIndex);

        if (!TryGetAgentDestination(bee, out var destination))
        {
            // No fallback is intentional. The overlay exists to inspect the real NavMeshAgent
            // destination, and drawing another field in the same red "destination" role would make
            // a bad or missing agent look like valid evidence. Leaving the bee undrawn makes the
            // missing prerequisite obvious through the top-left n/a status row.
            view.SetDestinationVisible(false);
            view.SetHudVisible(false);
            return;
        }

        var beeToDestinationDistance = Vector3.Distance(beePosition, destination);
        var destinationToHiveDistance = Vector3.Distance(destination, hive);

        // Only the destination-to-hive segment changes to the warning color because that is the
        // distance being tested. Keeping bee-to-destination yellow preserves direction context.
        var thresholdColor = destinationToHiveDistance >= DestinationToHiveThresholdDistance
            ? ThresholdLineColor
            : LineColor;

        view.SetWorld(
            beePosition + Vector3.up * WorldYOffset,
            destination + Vector3.up * WorldYOffset,
            hive + Vector3.up * WorldYOffset,
            Mathf.Max(beeToDestinationDistance, destinationToHiveDistance),
            thresholdColor
        );

        var beeUi = WorldToHudPoint(camera, beePosition + Vector3.up * WorldYOffset);
        var hiveUi = WorldToHudPoint(camera, hive + Vector3.up * WorldYOffset);
        var destinationUi = WorldToHudPoint(camera, destination + Vector3.up * WorldYOffset);
        view.SetHud(
            beeUi,
            destinationUi,
            hiveUi,
            thresholdColor,
            $"bee:{bee.thisEnemyIndex}  bee-dest {beeToDestinationDistance:F2}u  dest-hive {destinationToHiveDistance:F2}u  >=4 {(destinationToHiveDistance >= DestinationToHiveThresholdDistance ? "YES" : "NO")}"
        );
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

    private BeeView GetView(int beeIndex)
    {
        if (views.TryGetValue(beeIndex, out var view))
        {
            return view;
        }

        view = BeeView.Create(
            beeIndex,
            hudRoot!,
            font,
            worldLineMaterial,
            beeMaterial,
            hiveMaterial,
            destinationMaterial,
            lastKnownHiveMaterial
        );
        views.Add(beeIndex, view);
        return view;
    }

    private Vector2 WorldToHudPoint(Camera camera, Vector3 worldPosition)
    {
        var screen = camera.WorldToScreenPoint(worldPosition);
        if (screen.z < 0f)
        {
            // Points behind the camera project to mirrored screen coordinates. Flipping keeps the
            // off-screen indicator clamped to a useful edge instead of jumping across the HUD.
            screen *= -1f;
        }

        var margin = 16f;
        var clamped = new Vector2(
            Mathf.Clamp(screen.x, margin, Screen.width - margin),
            Mathf.Clamp(screen.y, margin, Screen.height - margin)
        );

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(hudRoot, clamped, null, out var localPoint)
            ? localPoint
            : clamped;
    }

    private static string GetBeeStatusLine(RedLocustBees bee)
    {
        if (bee == null)
        {
            return "bee:n/a  agentDest-hive=n/a  >=4 n/a  hiveLOS n/a  lkh-eye=n/a  missProbe n/a";
        }

        if (bee.hive == null)
        {
            return $"bee:{bee.thisEnemyIndex}  agentDest-hive=n/a  >=4 n/a  hiveLOS n/a  lkh-eye=n/a  missProbe n/a  no hive";
        }

        var beeEyePosition = bee.eye != null ? bee.eye.position : bee.transform.position + Vector3.up * WorldYOffset;
        var hiveMissingProbe = GetHiveMissingProbe(bee, beeEyePosition);
        var hiveMissingStatus = FormatHiveMissingProbeStatus(hiveMissingProbe);
        var hiveSightStatus = FormatHiveSightProbeStatus(GetHiveSightProbe(beeEyePosition, bee.hive.transform.position));

        if (!TryGetAgentDestination(bee, out var agentDestination))
        {
            // Keep the status row strict for the same reason as the overlay: every numeric distance
            // must come from NavMeshAgent.destination, otherwise the >=4 answer can look precise
            // while measuring the wrong piece of game AI state.
            return $"bee:{bee.thisEnemyIndex}  agentDest-hive=n/a  >=4 n/a  {hiveSightStatus}  {hiveMissingStatus}  no navmesh agent";
        }

        var distance = Vector3.Distance(agentDestination, bee.hive.transform.position);
        var overThreshold = distance >= DestinationToHiveThresholdDistance ? "YES" : "NO";
        return $"bee:{bee.thisEnemyIndex}  agentDest-hive={distance:F2}u  >=4 {overThreshold}  {hiveSightStatus}  {hiveMissingStatus}";
    }

    private static HiveSightProbe GetHiveSightProbe(Vector3 beeEyePosition, Vector3 hivePosition)
    {
        var linecastBlocked = StartOfRound.Instance == null || Physics.Linecast(
            beeEyePosition,
            hivePosition,
            StartOfRound.Instance.collidersAndRoomMaskAndDefault,
            QueryTriggerInteraction.Ignore
        );
        return new HiveSightProbe(hivePosition, Vector3.Distance(beeEyePosition, hivePosition), linecastBlocked);
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

    private static string FormatHiveMissingProbeStatus(HiveMissingProbe probe)
    {
        var synced = probe.SyncedLastKnownHivePosition.HasValue
            ? (probe.SyncedLastKnownHivePosition.Value ? "YES" : "NO")
            : "n/a";
        var los = probe.LinecastBlocked ? "blocked" : "clear";
        var active = probe.CanEvaluateMissing ? "YES" : "NO";
        return $"lkh-eye={probe.EyeToLastKnownHiveDistance:F2}u  lkhLOS={los}  lkhSync={synced}  missProbe {active}";
    }

    private static string FormatHiveSightProbeStatus(HiveSightProbe probe)
    {
        var los = probe.LinecastBlocked ? "blocked" : "clear";
        return $"hiveLOS={los}  hive-eye={probe.EyeToHiveDistance:F2}u";
    }

    internal static bool TryGetAgentDestination(RedLocustBees bee, out Vector3 destination)
    {
        var agent = bee.agent;
        if (agent != null && agent.isOnNavMesh)
        {
            // NavMeshAgent.destination is read only after the agent is confirmed to be on the
            // NavMesh. That guard is both a Unity safety check and the entire data-quality boundary
            // for this diagnostic mod.
            destination = agent.destination;
            return true;
        }

        // Return false instead of substituting bee.destination. bee.destination has represented
        // remembered or hive-adjacent state in the cases this mod is investigating, so using it as
        // a replacement would reintroduce the original ambiguity.
        destination = Vector3.zero;
        return false;
    }

    private static bool TryGetGameplayCamera(out Camera camera)
    {
        camera = null!;
        var player = GameNetworkManager.Instance != null
            ? GameNetworkManager.Instance.localPlayerController
            : null;
        if (player == null || player.isPlayerDead || player.gameplayCamera == null)
        {
            return false;
        }

        camera = player.gameplayCamera;
        return true;
    }

    private void SetStatus(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
        }
    }

    private void HideAll()
    {
        foreach (var view in views.Values)
        {
            view.SetVisible(false);
        }
    }

    private void LogWaitingForHud()
    {
        if (Time.realtimeSinceStartup < nextWaitingLogTime)
        {
            return;
        }

        nextWaitingLogTime = Time.realtimeSinceStartup + 5f;
        Plugin.Log?.LogInfo("Overlay waiting for HUDManager.HUDContainer.");
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static Material CreateMaterial(Color color)
    {
        var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Hidden/Internal-Colored");
        return new Material(shader)
        {
            color = color,
        };
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

        public bool? SyncedLastKnownHivePosition { get; }
    }

    private readonly struct HiveSightProbe
    {
        public HiveSightProbe(Vector3 hivePosition, float eyeToHiveDistance, bool linecastBlocked)
        {
            HivePosition = hivePosition;
            EyeToHiveDistance = eyeToHiveDistance;
            LinecastBlocked = linecastBlocked;
        }

        public Vector3 HivePosition { get; }

        public float EyeToHiveDistance { get; }

        public bool LinecastBlocked { get; }
    }

    private sealed class BeeView
    {
        private readonly GameObject rootObject;
        private readonly RectTransform beeToDestinationLineRect;
        private readonly RectTransform destinationToHiveLineRect;
        private readonly RectTransform beeMarkerRect;
        private readonly RectTransform hiveMarkerRect;
        private readonly RectTransform destinationMarkerRect;
        private readonly RectTransform labelRect;
        private readonly UiText label;
        private readonly GameObject worldRoot;
        private readonly LineRenderer beeToDestinationWorldLine;
        private readonly LineRenderer destinationToHiveWorldLine;
        private readonly LineRenderer defenseDistanceCircle;
        private readonly LineRenderer visiblePlayerSightLine;
        private readonly LineRenderer lastKnownHiveNearCircle;
        private readonly LineRenderer lastKnownHiveLineOfSightCircle;
        private readonly LineRenderer beeEyeToLastKnownHiveLine;
        private readonly LineRenderer beeEyeToHiveLine;
        private readonly GameObject beeMarker;
        private readonly GameObject hiveMarker;
        private readonly GameObject destinationMarker;
        private readonly GameObject lastKnownHiveMarker;

        private BeeView(
            GameObject rootObject,
            RectTransform beeToDestinationLineRect,
            RectTransform destinationToHiveLineRect,
            RectTransform beeMarkerRect,
            RectTransform hiveMarkerRect,
            RectTransform destinationMarkerRect,
            RectTransform labelRect,
            UiText label,
            GameObject worldRoot,
            LineRenderer beeToDestinationWorldLine,
            LineRenderer destinationToHiveWorldLine,
            LineRenderer defenseDistanceCircle,
            LineRenderer visiblePlayerSightLine,
            LineRenderer lastKnownHiveNearCircle,
            LineRenderer lastKnownHiveLineOfSightCircle,
            LineRenderer beeEyeToLastKnownHiveLine,
            LineRenderer beeEyeToHiveLine,
            GameObject beeMarker,
            GameObject hiveMarker,
            GameObject destinationMarker,
            GameObject lastKnownHiveMarker
        )
        {
            this.rootObject = rootObject;
            this.beeToDestinationLineRect = beeToDestinationLineRect;
            this.destinationToHiveLineRect = destinationToHiveLineRect;
            this.beeMarkerRect = beeMarkerRect;
            this.hiveMarkerRect = hiveMarkerRect;
            this.destinationMarkerRect = destinationMarkerRect;
            this.labelRect = labelRect;
            this.label = label;
            this.worldRoot = worldRoot;
            this.beeToDestinationWorldLine = beeToDestinationWorldLine;
            this.destinationToHiveWorldLine = destinationToHiveWorldLine;
            this.defenseDistanceCircle = defenseDistanceCircle;
            this.visiblePlayerSightLine = visiblePlayerSightLine;
            this.lastKnownHiveNearCircle = lastKnownHiveNearCircle;
            this.lastKnownHiveLineOfSightCircle = lastKnownHiveLineOfSightCircle;
            this.beeEyeToLastKnownHiveLine = beeEyeToLastKnownHiveLine;
            this.beeEyeToHiveLine = beeEyeToHiveLine;
            this.beeMarker = beeMarker;
            this.hiveMarker = hiveMarker;
            this.destinationMarker = destinationMarker;
            this.lastKnownHiveMarker = lastKnownHiveMarker;
        }

        public static BeeView Create(
            int beeIndex,
            RectTransform hudParent,
            Font? font,
            Material lineMaterial,
            Material beeMaterial,
            Material hiveMaterial,
            Material destinationMaterial,
            Material lastKnownHiveMaterial
        )
        {
            var rootObject = new GameObject($"BeeOverlay_{beeIndex}", typeof(RectTransform));
            rootObject.transform.SetParent(hudParent, false);
            Stretch(rootObject.GetComponent<RectTransform>());

            var beeToDestinationLineRect = CreateHudLine("BeeToDestinationLine", rootObject.transform);
            var destinationToHiveLineRect = CreateHudLine("DestinationToHiveLine", rootObject.transform);
            var beeMarkerRect = CreateHudMarker("BeeMarker", rootObject.transform, BeeColor);
            var hiveMarkerRect = CreateHudMarker("HiveMarker", rootObject.transform, HiveColor);
            var destinationMarkerRect = CreateHudMarker("DestinationMarker", rootObject.transform, DestinationColor);
            var (labelRect, label) = CreateHudLabel(rootObject.transform, font);

            var worldRoot = new GameObject($"BeeWorldOverlay_{beeIndex}");
            UnityObject.DontDestroyOnLoad(worldRoot);

            // World-space primitives are separate from the HUD hierarchy so they render at the real
            // in-game positions even when the HUD canvas scales or changes anchoring.
            var beeToDestinationWorldLine = CreateWorldLine("BeeToDestinationWorldLine", worldRoot.transform, lineMaterial);
            var destinationToHiveWorldLine = CreateWorldLine("DestinationToHiveWorldLine", worldRoot.transform, lineMaterial);
            var defenseDistanceCircle = CreateWorldLine("DefenseDistanceCircle", worldRoot.transform, lineMaterial);
            var visiblePlayerSightLine = CreateWorldLine("VisiblePlayerSightLine", worldRoot.transform, lineMaterial);
            var lastKnownHiveNearCircle = CreateWorldLine("LastKnownHiveNearCircle", worldRoot.transform, lineMaterial);
            var lastKnownHiveLineOfSightCircle = CreateWorldLine("LastKnownHiveLineOfSightCircle", worldRoot.transform, lineMaterial);
            var beeEyeToLastKnownHiveLine = CreateWorldLine("BeeEyeToLastKnownHiveLine", worldRoot.transform, lineMaterial);
            var beeEyeToHiveLine = CreateWorldLine("BeeEyeToHiveLine", worldRoot.transform, lineMaterial);
            var beeMarker = CreateWorldMarker("BeeWorldMarker", worldRoot.transform, beeMaterial);
            var hiveMarker = CreateWorldMarker("HiveWorldMarker", worldRoot.transform, hiveMaterial);
            var destinationMarker = CreateWorldMarker("DestinationWorldMarker", worldRoot.transform, destinationMaterial);
            var lastKnownHiveMarker = CreateWorldMarker("LastKnownHiveWorldMarker", worldRoot.transform, lastKnownHiveMaterial);

            // These guides are conditional frame-by-frame. Start hidden so a newly allocated view
            // never flashes stale geometry before the first real sample is written.
            defenseDistanceCircle.gameObject.SetActive(false);
            visiblePlayerSightLine.gameObject.SetActive(false);
            lastKnownHiveNearCircle.gameObject.SetActive(false);
            lastKnownHiveLineOfSightCircle.gameObject.SetActive(false);
            beeEyeToLastKnownHiveLine.gameObject.SetActive(false);
            beeEyeToHiveLine.gameObject.SetActive(false);
            lastKnownHiveMarker.SetActive(false);

            return new BeeView(
                rootObject,
                beeToDestinationLineRect,
                destinationToHiveLineRect,
                beeMarkerRect,
                hiveMarkerRect,
                destinationMarkerRect,
                labelRect,
                label,
                worldRoot,
                beeToDestinationWorldLine,
                destinationToHiveWorldLine,
                defenseDistanceCircle,
                visiblePlayerSightLine,
                lastKnownHiveNearCircle,
                lastKnownHiveLineOfSightCircle,
                beeEyeToLastKnownHiveLine,
                beeEyeToHiveLine,
                beeMarker,
                hiveMarker,
                destinationMarker,
                lastKnownHiveMarker
            );
        }

        public void SetHud(Vector2 bee, Vector2 destination, Vector2 hive, Color destinationToHiveColor, string labelText)
        {
            if (rootObject == null)
            {
                return;
            }

            rootObject.SetActive(true);
            beeMarkerRect.anchoredPosition = bee;
            hiveMarkerRect.anchoredPosition = hive;
            destinationMarkerRect.anchoredPosition = destination;

            // The HUD uses two line RectTransforms instead of a polyline component because the game
            // already ships Unity UI Image and the overlay only needs two straight segments.
            SetHudLine(beeToDestinationLineRect, bee, destination, LineColor);
            SetHudLine(destinationToHiveLineRect, destination, hive, destinationToHiveColor);

            labelRect.anchoredPosition = destination + Vector2.up * 18f;
            label.text = labelText;
        }

        public void SetHudVisible(bool visible)
        {
            if (rootObject != null)
            {
                rootObject.SetActive(visible);
            }
        }

        public void SetSpatialGuides(
            Vector3 hive,
            int defenseDistance,
            Vector3 beeEye,
            Vector3? visiblePlayer,
            HiveMissingProbe hiveMissingProbe,
            HiveSightProbe hiveSightProbe
        )
        {
            if (worldRoot == null)
            {
                return;
            }

            worldRoot.SetActive(true);

            // RedLocustBees stores defenseDistance as an integer radius around the hive. Drawing it
            // as a horizontal ring lets the player judge "near hive" before crossing the trigger
            // region, which is more useful than another late true/false row in the HUD.
            SetWorldCircle(defenseDistanceCircle, hive, defenseDistance, DefenseDistanceColor);
            SetHiveMissingProbe(beeEye, hiveMissingProbe);
            SetHiveSightProbe(beeEye, hiveSightProbe);

            // The player line is drawn only when the game-side visibility query currently returns
            // a player. Absence of the line therefore means "not currently seen by this check",
            // without adding a fallback or guessing through walls.
            if (visiblePlayer.HasValue)
            {
                visiblePlayerSightLine.gameObject.SetActive(true);
                SetWorldLine(visiblePlayerSightLine, beeEye, visiblePlayer.Value, SightLineColor);
            }
            else
            {
                visiblePlayerSightLine.gameObject.SetActive(false);
            }
        }

        private void SetHiveSightProbe(Vector3 beeEye, HiveSightProbe probe)
        {
            // This is a predictive helper for the pickup moment: if the player is effectively at
            // the hive, the bee-to-hive ray is the closest stable proxy for whether the bee could
            // see that pickup position before the player collider is actually there.
            var hiveTarget = probe.HivePosition + Vector3.up * WorldYOffset;
            var lineColor = probe.LinecastBlocked ? HiveBlockedLineColor : HiveVisibleLineColor;
            SetWorldLine(beeEyeToHiveLine, beeEye, hiveTarget, lineColor);
            beeEyeToHiveLine.gameObject.SetActive(true);
        }

        private void SetHiveMissingProbe(Vector3 beeEye, HiveMissingProbe probe)
        {
            var lastKnownHive = probe.LastKnownHivePosition + Vector3.up * WorldYOffset;
            lastKnownHiveMarker.SetActive(true);
            lastKnownHiveMarker.transform.position = lastKnownHive;

            // IsHiveMissing() first asks whether the bee is close enough to, or has line of sight
            // to, lastKnownHivePosition. Centering these rings on that remembered point makes the
            // state-2 risk zone visible independently from the current hive position.
            SetWorldCircle(lastKnownHiveNearCircle, lastKnownHive, HiveMissingNearDistance, HiveMissingNearColor);
            SetWorldCircle(
                lastKnownHiveLineOfSightCircle,
                lastKnownHive,
                HiveMissingLineOfSightDistance,
                HiveMissingLineOfSightColor
            );

            var probeLineColor = probe.CanEvaluateMissing
                ? HiveMissingActiveLineColor
                : HiveMissingInactiveLineColor;
            SetWorldLine(beeEyeToLastKnownHiveLine, beeEye, lastKnownHive, probeLineColor);
            beeEyeToLastKnownHiveLine.gameObject.SetActive(true);

            // Keep the remembered hive marker slightly smaller than the three primary state points
            // so it reads as diagnostic context instead of a fourth object competing with hive.
            var markerScale = Mathf.Clamp(probe.EyeToLastKnownHiveDistance * 0.03f, 0.14f, 0.32f);
            lastKnownHiveMarker.transform.localScale = Vector3.one * markerScale;
        }

        public void SetWorld(Vector3 bee, Vector3 destination, Vector3 hive, float markerDistance, Color destinationToHiveColor)
        {
            if (worldRoot == null)
            {
                return;
            }

            worldRoot.SetActive(true);
            SetDestinationVisible(true);

            // Use two LineRenderers so each segment can keep an independent color. A single
            // three-point LineRenderer would interpolate colors across the shared destination point.
            SetWorldLine(beeToDestinationWorldLine, bee, destination, LineColor);
            SetWorldLine(destinationToHiveWorldLine, destination, hive, destinationToHiveColor);
            beeMarker.transform.position = bee;
            hiveMarker.transform.position = hive;
            destinationMarker.transform.position = destination;

            // Scale markers by the larger nearby segment so tiny movements remain visible without
            // letting long-distance cases dominate the scene.
            var markerScale = Mathf.Clamp(markerDistance * 0.04f, 0.18f, 0.45f);
            beeMarker.transform.localScale = Vector3.one * markerScale;
            hiveMarker.transform.localScale = Vector3.one * markerScale;
            destinationMarker.transform.localScale = Vector3.one * markerScale;
        }

        public void SetDestinationVisible(bool visible)
        {
            beeToDestinationWorldLine.gameObject.SetActive(visible);
            destinationToHiveWorldLine.gameObject.SetActive(visible);
            beeMarker.SetActive(visible);
            hiveMarker.SetActive(visible);
            destinationMarker.SetActive(visible);
        }

        public void SetVisible(bool visible)
        {
            if (rootObject != null)
            {
                rootObject.SetActive(visible);
            }

            if (worldRoot != null)
            {
                worldRoot.SetActive(visible);
            }
        }

        private static void SetHudLine(RectTransform rect, Vector2 start, Vector2 end, Color color)
        {
            var delta = end - start;
            rect.anchoredPosition = start + delta * 0.5f;
            rect.sizeDelta = new Vector2(delta.magnitude, UiLineThickness);
            rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            rect.GetComponent<UiImage>().color = color;
        }

        private static void SetWorldLine(LineRenderer line, Vector3 start, Vector3 end, Color color)
        {
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startColor = color;
            line.endColor = color;
        }

        private static void SetWorldCircle(LineRenderer line, Vector3 center, float radius, Color color)
        {
            if (radius <= 0f)
            {
                line.gameObject.SetActive(false);
                return;
            }

            line.gameObject.SetActive(true);
            line.positionCount = DefenseDistanceCircleSegments + 1;
            line.startColor = color;
            line.endColor = color;

            for (var i = 0; i <= DefenseDistanceCircleSegments; i++)
            {
                var radians = Mathf.PI * 2f * i / DefenseDistanceCircleSegments;
                var offset = new Vector3(Mathf.Cos(radians) * radius, 0f, Mathf.Sin(radians) * radius);
                line.SetPosition(i, center + offset);
            }
        }

        private static RectTransform CreateHudLine(string name, Transform parent)
        {
            var lineObject = new GameObject(name, typeof(RectTransform), typeof(UiImage));
            lineObject.transform.SetParent(parent, false);
            var rect = lineObject.GetComponent<RectTransform>();
            Center(rect);
            lineObject.GetComponent<UiImage>().color = LineColor;
            return rect;
        }

        private static RectTransform CreateHudMarker(string name, Transform parent, Color color)
        {
            var markerObject = new GameObject(name, typeof(RectTransform), typeof(UiImage));
            markerObject.transform.SetParent(parent, false);
            var rect = markerObject.GetComponent<RectTransform>();
            Center(rect);
            rect.sizeDelta = new Vector2(UiMarkerSize, UiMarkerSize);
            markerObject.GetComponent<UiImage>().color = color;
            return rect;
        }

        private static (RectTransform Rect, UiText Text) CreateHudLabel(Transform parent, Font? font)
        {
            var labelObject = new GameObject("DistanceLabel", typeof(RectTransform), typeof(UiText));
            labelObject.transform.SetParent(parent, false);
            var rect = labelObject.GetComponent<RectTransform>();
            Center(rect);
            rect.sizeDelta = new Vector2(360f, 24f);

            var text = labelObject.GetComponent<UiText>();
            text.font = font;
            text.fontSize = 16;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return (rect, text);
        }

        private static LineRenderer CreateWorldLine(string name, Transform parent, Material material)
        {
            var lineObject = new GameObject(name);
            lineObject.transform.SetParent(parent, false);
            var line = lineObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.startWidth = 0.06f;
            line.endWidth = 0.06f;
            line.numCapVertices = 4;
            line.startColor = LineColor;
            line.endColor = LineColor;
            line.material = material;
            return line;
        }

        private static GameObject CreateWorldMarker(string name, Transform parent, Material material)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = name;
            marker.transform.SetParent(parent, false);
            marker.GetComponent<Renderer>().material = material;

            var collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                // Overlay markers are visual probes only. Removing colliders avoids changing
                // gameplay physics, raycasts, or any mod that scans nearby colliders.
                UnityObject.Destroy(collider);
            }

            return marker;
        }

        private static void Center(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
        }
    }
}

[HarmonyPatch(typeof(HUDManager), "Update")]
internal static class HudUpdatePatch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        // HUDManager.Update runs during normal gameplay and already has the current HUD context.
        // Driving the overlay here keeps the visualization in sync without adding another
        // MonoBehaviour object to manage.
        Overlay.Instance?.Tick();
    }
}

[HarmonyPatch(typeof(RedLocustBees), nameof(RedLocustBees.DoAIInterval))]
internal static class BeeLogPatch
{
    private const float LogIntervalSeconds = 1f;
    private static readonly Dictionary<int, float> NextLogTimes = new();

    [HarmonyPostfix]
    private static void Postfix(RedLocustBees __instance)
    {
        var now = Time.realtimeSinceStartup;
        var beeIndex = __instance.thisEnemyIndex;
        if (NextLogTimes.TryGetValue(beeIndex, out var nextLogTime) && now < nextLogTime)
        {
            // DoAIInterval can run frequently enough to flood BepInEx logs. Throttle per bee so the
            // log stays useful while still capturing behavior changes over time.
            return;
        }

        NextLogTimes[beeIndex] = now + LogIntervalSeconds;
        Log(__instance);
    }

    private static void Log(RedLocustBees bee)
    {
        if (bee.hive == null)
        {
            Plugin.Log?.LogInfo($"[bee:{bee.thisEnemyIndex}] no hive");
            return;
        }

        var hive = bee.hive.transform.position;
        var agentDestination = Vector3.zero;
        // The log follows the same strict source rule as the overlay and status text. If these
        // fields are present, they came from the live NavMeshAgent destination.
        var agentReadable = Overlay.TryGetAgentDestination(bee, out agentDestination);

        Plugin.Log?.LogInfo(
            $"[bee:{bee.thisEnemyIndex}] "
                + $"state={bee.currentBehaviourStateIndex} "
                + $"hiveHeld={bee.hive.isHeld} "
                + $"agentOnNavMesh={agentReadable} "
                + $"agentDestinationToHive={Fmt.Distance(agentDestination, hive, agentReadable)} "
                + $"bodyToAgentDestination={Fmt.Distance(bee.transform.position, agentDestination, agentReadable)} "
                + $"hive={Fmt.Vector(hive)} "
                + $"agentDestination={Fmt.Vector(agentDestination, agentReadable)}"
        );
    }
}

internal static class Fmt
{
    public static string Distance(Vector3 a, Vector3 b, bool enabled = true)
    {
        return enabled ? Vector3.Distance(a, b).ToString("F3") : "n/a";
    }

    public static string Vector(Vector3 vector, bool enabled = true)
    {
        return enabled ? $"({vector.x:F3},{vector.y:F3},{vector.z:F3})" : "n/a";
    }
}
