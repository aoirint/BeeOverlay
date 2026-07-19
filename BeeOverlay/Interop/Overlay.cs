#nullable enable

extern alias LethalCompany;
extern alias UnityEngine;

using System.Collections.Generic;
using System.Text;
using BeeOverlay.Core.Models;
using BeeOverlay.Core.Ports;
using BepInEx.Logging;
using LethalCompany;
using UnityEngine::UnityEngine;
using UiText = LethalCompany::UnityEngine.UI.Text;
using UnityObject = UnityEngine::UnityEngine.Object;

namespace BeeOverlay.Interop;

internal sealed partial class Overlay : IOverlayPresenter
{
    // Keep one lifecycle owner for HUD and per-bee views. The partial files separate sampling and
    // rendering details without creating independent mutable services that could drift per frame.
    // The state transition overlay uses entity colors first, then grey only for blocked/inactive
    // checks. That keeps dots, lines, and wireframes readable as "which object is this about?" instead
    // of mixing separate colors for every condition.
    private static readonly Color HudTextColor = new(1f, 0.85f, 0.1f, 0.95f);
    private static readonly Color BeeColor = new(1f, 0.85f, 0.1f, 0.95f);
    private static readonly Color HiveColor = new(0.25f, 1f, 0.35f, 0.95f);
    private static readonly Color LastKnownHiveColor = new(0.05f, 0.32f, 1f, 0.95f);
    private static readonly Color LastKnownHiveNearSphereColor = new(0.15f, 0.55f, 1f, 0.7f);
    private static readonly Color LastKnownHiveLineOfSightSphereColor = new(0.25f, 0.6f, 1f, 0.3f);
    private static readonly Color PlayerColor = new(1f, 0.15f, 0.1f, 0.95f);
    private static readonly Color PickupProxyColor = new(1f, 1f, 1f, 0.95f);
    private static readonly Color InactiveLineColor = new(0.18f, 0.18f, 0.18f, 0.58f);

    // Keep the important thresholds named at the overlay boundary. The goal is not to invent new
    // gameplay rules here; each visual should point back to one specific base-game gate that can
    // move RedLocustBees out of state 0.
    private const float VisiblePlayerSightLineRenderYOffset = -0.35f;
    // Six 48-segment loops produce almost the same vertex count as the previous three 96-segment
    // great circles, while latitude and longitude cues make the guide read as a sphere at a glance.
    private const int WireframeSphereSegments = 48;
    private const float WireframeLatitudeOffsetFactor = 0.5f;
    private const float WireframeLatitudeRadiusFactor = 0.8660254f;

    // Markers are lifted a little above the sampled positions so they are not hidden by terrain,
    // hive meshes, or the bee body while still representing the same horizontal point.
    private const float WorldYOffset = 0.35f;

    private readonly Dictionary<int, BeeView> views = new();
    private readonly Font? font = Resources.GetBuiltinResource<Font>("Arial.ttf");
    private readonly ManualLogSource logger;

    // LineRenderer vertex colors are updated per segment every frame. A white material avoids
    // tinting those runtime colors and lets the normal/warning colors match the HUD lines.
    private readonly Material worldLineMaterial = CreateMaterial(Color.white);
    private readonly Material beeMaterial = CreateMaterial(BeeColor);
    private readonly Material hiveMaterial = CreateMaterial(HiveColor);
    private readonly Material lastKnownHiveMaterial = CreateMaterial(LastKnownHiveColor);
    private readonly Material playerMaterial = CreateMaterial(PlayerColor);

    private RectTransform? hudRoot;
    private UiText? statusText;
    private Transform? attachedHudContainer;
    private float nextWaitingLogTime;

    public Overlay(ManualLogSource logger)
    {
        this.logger = logger;
    }

    public bool TryPrepare()
    {
        return TryEnsureHudRoot();
    }

    public void Present(OverlayFrame frame)
    {
        var seen = new HashSet<int>();
        var statusBuilder = new StringBuilder();
        statusBuilder.Append($"Bee Overlay | bees={frame.Bees.Count}");
        foreach (var bee in frame.Bees)
        {
            DrawBee(bee, seen);
            statusBuilder.AppendLine();
            // HUD numbers are compact per-frame ordinals after sorting, while the view dictionary
            // still uses the stable identity below. That keeps the overlay readable without giving
            // up the identity Unity exposes for hiding old per-bee world objects.
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

        logger.LogInfo($"Overlay attached to HUDContainer='{hudContainer.name}'.");
        return true;
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

    private void SetStatus(string text)
    {
        if (statusText != null)
        {
            // The HUD string is already rich-text colored. Assigning one block keeps Unity's text
            // rebuild cheap and avoids per-row objects that would have to be recreated with bees.
            statusText.text = text;
        }
    }

    public void HideAll()
    {
        foreach (var view in views.Values)
        {
            view.SetVisible(false);
        }
    }

    public void LogWaitingForHud()
    {
        if (Time.realtimeSinceStartup < nextWaitingLogTime)
        {
            return;
        }

        nextWaitingLogTime = Time.realtimeSinceStartup + 5f;
        logger.LogInfo("Overlay waiting for HUDManager.HUDContainer.");
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

}
