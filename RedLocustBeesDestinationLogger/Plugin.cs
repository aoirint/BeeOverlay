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
using PlayerControllerB = LethalCompany::GameNetcodeStuff.PlayerControllerB;
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
    // The state transition overlay uses entity colors first, then grey only for blocked/inactive
    // checks. That keeps dots, lines, and rings readable as "which object is this about?" instead
    // of mixing separate colors for every condition.
    private static readonly Color HudTextColor = new(1f, 0.85f, 0.1f, 0.95f);
    private static readonly Color BeeColor = new(1f, 0.85f, 0.1f, 0.95f);
    private static readonly Color HiveColor = new(0.25f, 1f, 0.35f, 0.95f);
    private static readonly Color LastKnownHiveColor = new(0.15f, 0.55f, 1f, 0.95f);
    private static readonly Color PlayerColor = new(1f, 0.15f, 0.1f, 0.95f);
    private static readonly Color InactiveLineColor = new(0.45f, 0.5f, 0.52f, 0.55f);

    private const float PlayerLineOfSightDistance = 16f;
    private const float VisiblePlayerSightLineRenderYOffset = -0.35f;
    private const float HiveMissingNearDistance = 4f;
    private const float HiveMissingLineOfSightDistance = 8f;
    private const int DefenseDistanceCircleSegments = 96;

    // Markers are lifted a little above the sampled positions so they are not hidden by terrain,
    // hive meshes, or the bee body while still representing the same horizontal point.
    private const float WorldYOffset = 0.35f;

    private readonly Dictionary<int, BeeView> views = new();
    private readonly Font? font = Resources.GetBuiltinResource<Font>("Arial.ttf");

    // LineRenderer vertex colors are updated per segment every frame. A white material avoids
    // tinting those runtime colors and lets the normal/warning colors match the HUD lines.
    private readonly Material worldLineMaterial = CreateMaterial(Color.white);
    private readonly Material beeMaterial = CreateMaterial(BeeColor);
    private readonly Material hiveMaterial = CreateMaterial(HiveColor);
    private readonly Material lastKnownHiveMaterial = CreateMaterial(LastKnownHiveColor);
    private readonly Material playerMaterial = CreateMaterial(PlayerColor);

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

        var bees = UnityObject.FindObjectsOfType<RedLocustBees>();

        // Stable ordering makes the top-left status useful for screenshots and frame-to-frame
        // comparison. Unity's object enumeration order is not a good identity signal by itself.
        Array.Sort(bees, static (left, right) => left.thisEnemyIndex.CompareTo(right.thisEnemyIndex));

        var seen = new HashSet<int>();
        var statusBuilder = new StringBuilder();
        statusBuilder.Append($"Bee state transition overlay | bees={bees.Length}");
        var localPlayer = GameNetworkManager.Instance != null ? GameNetworkManager.Instance.localPlayerController : null;
        var localPlayerPosition = GetPlayerBodyPosition(localPlayer);
        foreach (var bee in bees)
        {
            DrawBee(bee, seen);
            statusBuilder.AppendLine();
            statusBuilder.Append(GetBeeStatusLine(bee, localPlayer, localPlayerPosition));
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

        var rootObject = new GameObject("RedLocustBeesState0Overlay", typeof(RectTransform));
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
        statusText.color = HudTextColor;
        statusText.supportRichText = true;
        statusText.raycastTarget = false;
        statusText.horizontalOverflow = HorizontalWrapMode.Overflow;
        statusText.verticalOverflow = VerticalWrapMode.Overflow;

        Plugin.Log?.LogInfo($"Overlay attached to HUDContainer='{hudContainer.name}'.");
        return true;
    }

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
        return player != null ? player.transform.position : null;
    }

    private BeeView GetView(int beeIndex)
    {
        if (views.TryGetValue(beeIndex, out var view))
        {
            return view;
        }

        view = BeeView.Create(
            beeIndex,
            worldLineMaterial,
            beeMaterial,
            hiveMaterial,
            lastKnownHiveMaterial,
            playerMaterial
        );
        views.Add(beeIndex, view);
        return view;
    }

    private static string GetBeeStatusLine(
        RedLocustBees bee,
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
            return $"{Tag($"bee:{bee.thisEnemyIndex}", BeeColor)}  hive n/a";
        }

        var beeEyePosition = bee.eye != null ? bee.eye.position : bee.transform.position + Vector3.up * WorldYOffset;
        var hivePosition = bee.hive.transform.position;
        var visiblePlayer = bee.CheckLineOfSightForPlayer(360f, (int)PlayerLineOfSightDistance, 1);
        var canSeeLocalPlayer = localPlayer != null && visiblePlayer != null && visiblePlayer == localPlayer;
        var playerToHiveDistance = localPlayerPosition.HasValue
            ? Vector3.Distance(localPlayerPosition.Value, hivePosition)
            : (float?)null;
        var hiveMissingProbe = GetHiveMissingProbe(bee, beeEyePosition);
        var hiveSightProbe = GetHiveSightProbe(beeEyePosition, hivePosition);

        // HUD rows intentionally avoid transition-derived labels such as missProbe. In practice the
        // player needs quick distances and current game visibility booleans; the colored terms map
        // to the same entity colors as the 3D dots/lines.
        return string.Join(
            "  ",
            Tag($"bee:{bee.thisEnemyIndex}", BeeColor),
            Tag($"player-hive={FmtDistance(playerToHiveDistance)}", PlayerColor),
            Tag($"bee-hive={hiveSightProbe.EyeToHiveDistance:F2}u", HiveColor),
            Tag($"bee-lkh={hiveMissingProbe.EyeToLastKnownHiveDistance:F2}u", LastKnownHiveColor),
            Tag($"playerLOS={YesNo(canSeeLocalPlayer)}", PlayerColor),
            Tag($"hiveLOS={YesNo(!hiveSightProbe.LinecastBlocked)}", HiveColor),
            Tag($"lkhLOS={YesNo(!hiveMissingProbe.LinecastBlocked)}", LastKnownHiveColor)
        );
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

    private static string FmtDistance(float? distance)
    {
        return distance.HasValue ? $"{distance.Value:F2}u" : "n/a";
    }

    private static string YesNo(bool value)
    {
        return value ? "YES" : "NO";
    }

    private static string Tag(string text, Color color)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>";
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
        private readonly GameObject worldRoot;
        private readonly LineRenderer defenseDistanceCircle;
        private readonly LineRenderer visiblePlayerSightLine;
        private readonly LineRenderer lastKnownHiveNearCircle;
        private readonly LineRenderer beeEyeToLastKnownHiveLine;
        private readonly LineRenderer beeEyeToHiveLine;
        private readonly GameObject beeMarker;
        private readonly GameObject hiveMarker;
        private readonly GameObject lastKnownHiveMarker;
        private readonly GameObject playerMarker;

        private BeeView(
            GameObject worldRoot,
            LineRenderer defenseDistanceCircle,
            LineRenderer visiblePlayerSightLine,
            LineRenderer lastKnownHiveNearCircle,
            LineRenderer beeEyeToLastKnownHiveLine,
            LineRenderer beeEyeToHiveLine,
            GameObject beeMarker,
            GameObject hiveMarker,
            GameObject lastKnownHiveMarker,
            GameObject playerMarker
        )
        {
            this.worldRoot = worldRoot;
            this.defenseDistanceCircle = defenseDistanceCircle;
            this.visiblePlayerSightLine = visiblePlayerSightLine;
            this.lastKnownHiveNearCircle = lastKnownHiveNearCircle;
            this.beeEyeToLastKnownHiveLine = beeEyeToLastKnownHiveLine;
            this.beeEyeToHiveLine = beeEyeToHiveLine;
            this.beeMarker = beeMarker;
            this.hiveMarker = hiveMarker;
            this.lastKnownHiveMarker = lastKnownHiveMarker;
            this.playerMarker = playerMarker;
        }

        public static BeeView Create(
            int beeIndex,
            Material lineMaterial,
            Material beeMaterial,
            Material hiveMaterial,
            Material lastKnownHiveMaterial,
            Material playerMaterial
        )
        {
            var worldRoot = new GameObject($"BeeWorldOverlay_{beeIndex}");
            UnityObject.DontDestroyOnLoad(worldRoot);

            var defenseDistanceCircle = CreateWorldLine("DefenseDistanceCircle", worldRoot.transform, lineMaterial);
            var visiblePlayerSightLine = CreateWorldLine("VisiblePlayerSightLine", worldRoot.transform, lineMaterial);
            var lastKnownHiveNearCircle = CreateWorldLine("LastKnownHiveNearCircle", worldRoot.transform, lineMaterial);
            var beeEyeToLastKnownHiveLine = CreateWorldLine("BeeEyeToLastKnownHiveLine", worldRoot.transform, lineMaterial);
            var beeEyeToHiveLine = CreateWorldLine("BeeEyeToHiveLine", worldRoot.transform, lineMaterial);
            var beeMarker = CreateWorldMarker("BeeEyeWorldMarker", worldRoot.transform, beeMaterial);
            var hiveMarker = CreateWorldMarker("HiveWorldMarker", worldRoot.transform, hiveMaterial);
            var lastKnownHiveMarker = CreateWorldMarker("LastKnownHiveWorldMarker", worldRoot.transform, lastKnownHiveMaterial);
            var playerMarker = CreateWorldMarker("LocalPlayerWorldMarker", worldRoot.transform, playerMaterial);

            // These guides are conditional frame-by-frame. Start hidden so a newly allocated view
            // never flashes stale geometry before the first real sample is written.
            defenseDistanceCircle.gameObject.SetActive(false);
            visiblePlayerSightLine.gameObject.SetActive(false);
            lastKnownHiveNearCircle.gameObject.SetActive(false);
            beeEyeToLastKnownHiveLine.gameObject.SetActive(false);
            beeEyeToHiveLine.gameObject.SetActive(false);
            beeMarker.SetActive(false);
            hiveMarker.SetActive(false);
            lastKnownHiveMarker.SetActive(false);
            playerMarker.SetActive(false);

            return new BeeView(
                worldRoot,
                defenseDistanceCircle,
                visiblePlayerSightLine,
                lastKnownHiveNearCircle,
                beeEyeToLastKnownHiveLine,
                beeEyeToHiveLine,
                beeMarker,
                hiveMarker,
                lastKnownHiveMarker,
                playerMarker
            );
        }

        public void SetSpatialGuides(
            Vector3 hive,
            int defenseDistance,
            Vector3 beeEye,
            Vector3? localPlayer,
            bool canSeeLocalPlayer,
            HiveMissingProbe hiveMissingProbe,
            HiveSightProbe hiveSightProbe
        )
        {
            if (worldRoot == null)
            {
                return;
            }

            worldRoot.SetActive(true);
            SetMarker(beeMarker, beeEye, 0.16f);
            SetMarker(hiveMarker, hive, 0.18f);

            // RedLocustBees stores defenseDistance as an integer radius around the hive. Drawing it
            // as a horizontal ring lets the player judge "near hive" before crossing the trigger
            // region, which is more useful than another late true/false row in the HUD.
            SetWorldCircle(defenseDistanceCircle, hive, defenseDistance, HiveColor);
            SetHiveMissingProbe(beeEye, hiveMissingProbe);
            SetHiveSightProbe(beeEye, hiveSightProbe);

            // The player line follows the same "always draw, change color" convention as the
            // hive line. It targets the local player's real camera/body position, while the color
            // comes only from the game's CheckLineOfSightForPlayer result for that same player.
            // This keeps blocked sight readable without inventing our own line-of-sight fallback.
            if (localPlayer.HasValue)
            {
                // This offset is rendering-only and applies only to the player end of the line.
                // Keeping the bee-eye end exact preserves the visual meaning of "the bee is
                // looking from here", while lowering the player end keeps rapid visibility flicker
                // from flashing across the player's exact camera point.
                var playerRenderOffset = Vector3.up * VisiblePlayerSightLineRenderYOffset;
                var displayedPlayer = localPlayer.Value + playerRenderOffset;
                var lineColor = canSeeLocalPlayer ? PlayerColor : InactiveLineColor;
                SetMarker(playerMarker, displayedPlayer, 0.16f);
                visiblePlayerSightLine.gameObject.SetActive(true);
                SetWorldLine(visiblePlayerSightLine, beeEye, displayedPlayer, lineColor);
            }
            else
            {
                visiblePlayerSightLine.gameObject.SetActive(false);
                playerMarker.SetActive(false);
            }
        }

        private void SetHiveSightProbe(Vector3 beeEye, HiveSightProbe probe)
        {
            // This is a predictive helper for the pickup moment: if the player is effectively at
            // the hive, the bee-to-hive ray is the closest stable proxy for whether the bee could
            // see that pickup position before the player collider is actually there.
            var hiveTarget = probe.HivePosition + Vector3.up * WorldYOffset;
            var lineColor = probe.LinecastBlocked ? InactiveLineColor : HiveColor;
            SetWorldLine(beeEyeToHiveLine, beeEye, hiveTarget, lineColor);
            beeEyeToHiveLine.gameObject.SetActive(true);
        }

        private void SetHiveMissingProbe(Vector3 beeEye, HiveMissingProbe probe)
        {
            var lastKnownHive = probe.LastKnownHivePosition + Vector3.up * WorldYOffset;
            lastKnownHiveMarker.SetActive(true);
            lastKnownHiveMarker.transform.position = lastKnownHive;

            // The 4u ring marks the close-range IsHiveMissing() trigger directly. The 8u
            // line-of-sight trigger is intentionally left as text plus the bee-to-memory line so
            // it does not compete with the more actionable pickup sightline guides.
            SetWorldCircle(lastKnownHiveNearCircle, lastKnownHive, HiveMissingNearDistance, LastKnownHiveColor);

            var probeLineColor = probe.CanEvaluateMissing
                ? LastKnownHiveColor
                : InactiveLineColor;
            SetWorldLine(beeEyeToLastKnownHiveLine, beeEye, lastKnownHive, probeLineColor);
            beeEyeToLastKnownHiveLine.gameObject.SetActive(true);

            // Keep the remembered hive marker slightly smaller than the three primary state points
            // so it reads as diagnostic context instead of a fourth object competing with hive.
            var markerScale = Mathf.Clamp(probe.EyeToLastKnownHiveDistance * 0.03f, 0.14f, 0.32f);
            lastKnownHiveMarker.transform.localScale = Vector3.one * markerScale;
        }

        public void SetVisible(bool visible)
        {
            if (worldRoot != null)
            {
                worldRoot.SetActive(visible);
            }
        }

        private static void SetMarker(GameObject marker, Vector3 position, float scale)
        {
            marker.SetActive(true);
            marker.transform.position = position;
            marker.transform.localScale = Vector3.one * scale;
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
            line.startColor = HudTextColor;
            line.endColor = HudTextColor;
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

        Plugin.Log?.LogInfo(
            $"[bee:{bee.thisEnemyIndex}] "
                + $"state={bee.currentBehaviourStateIndex} "
                + $"hiveHeld={bee.hive.isHeld} "
                + $"hive={Fmt.Vector(bee.hive.transform.position)} "
                + $"lastKnownHive={Fmt.Vector(bee.lastKnownHivePosition)}"
        );
    }
}

internal static class Fmt
{
    public static string Vector(Vector3 vector, bool enabled = true)
    {
        return enabled ? $"({vector.x:F3},{vector.y:F3},{vector.z:F3})" : "n/a";
    }
}
