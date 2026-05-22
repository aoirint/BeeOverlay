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

    // Four Unity units is the investigation threshold from the observed bee return behavior.
    // Keep it near the overlay colors because it drives both the status text and the warning line.
    private const float DestinationToHiveThresholdDistance = 4f;

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
        statusRect.sizeDelta = new Vector2(760f, 240f);

        statusText = statusObject.GetComponent<UiText>();
        statusText.font = font;
        statusText.fontSize = 16;
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

        if (!TryGetAgentDestination(bee, out var destination))
        {
            // No fallback is intentional. The overlay exists to inspect the real NavMeshAgent
            // destination, and drawing another field in the same red "destination" role would make
            // a bad or missing agent look like valid evidence. Leaving the bee undrawn makes the
            // missing prerequisite obvious through the top-left n/a status row.
            return;
        }

        var view = GetView(bee.thisEnemyIndex);
        var beePosition = bee.transform.position;
        var hive = bee.hive.transform.position;
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
        seen.Add(bee.thisEnemyIndex);
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
            destinationMaterial
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
            return "bee:n/a  agentDest-hive=n/a  >=4 n/a";
        }

        if (bee.hive == null)
        {
            return $"bee:{bee.thisEnemyIndex}  agentDest-hive=n/a  >=4 n/a  no hive";
        }

        if (!TryGetAgentDestination(bee, out var agentDestination))
        {
            // Keep the status row strict for the same reason as the overlay: every numeric distance
            // must come from NavMeshAgent.destination, otherwise the >=4 answer can look precise
            // while measuring the wrong piece of game AI state.
            return $"bee:{bee.thisEnemyIndex}  agentDest-hive=n/a  >=4 n/a  no navmesh agent";
        }

        var distance = Vector3.Distance(agentDestination, bee.hive.transform.position);
        var overThreshold = distance >= DestinationToHiveThresholdDistance ? "YES" : "NO";
        return $"bee:{bee.thisEnemyIndex}  agentDest-hive={distance:F2}u  >=4 {overThreshold}";
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
        private readonly GameObject beeMarker;
        private readonly GameObject hiveMarker;
        private readonly GameObject destinationMarker;

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
            GameObject beeMarker,
            GameObject hiveMarker,
            GameObject destinationMarker
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
            this.beeMarker = beeMarker;
            this.hiveMarker = hiveMarker;
            this.destinationMarker = destinationMarker;
        }

        public static BeeView Create(
            int beeIndex,
            RectTransform hudParent,
            Font? font,
            Material lineMaterial,
            Material beeMaterial,
            Material hiveMaterial,
            Material destinationMaterial
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
            var beeMarker = CreateWorldMarker("BeeWorldMarker", worldRoot.transform, beeMaterial);
            var hiveMarker = CreateWorldMarker("HiveWorldMarker", worldRoot.transform, hiveMaterial);
            var destinationMarker = CreateWorldMarker("DestinationWorldMarker", worldRoot.transform, destinationMaterial);

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
                beeMarker,
                hiveMarker,
                destinationMarker
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

        public void SetWorld(Vector3 bee, Vector3 destination, Vector3 hive, float markerDistance, Color destinationToHiveColor)
        {
            if (worldRoot == null)
            {
                return;
            }

            worldRoot.SetActive(true);

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
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startColor = color;
            line.endColor = color;
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
