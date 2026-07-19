#nullable enable

extern alias UnityEngine;

using BeeOverlay.Core;
using BeeOverlay.Core.Models;
using UnityEngine::UnityEngine;
using UnityObject = UnityEngine::UnityEngine.Object;

namespace BeeOverlay.Interop;

internal sealed partial class Overlay
{
    private sealed class BeeView
    {
        private readonly GameObject worldRoot;
        private readonly WireframeSphere beeSightRangeSphere;
        private readonly WireframeSphere defenseDistanceSphere;
        private readonly LineRenderer visiblePlayerSightLine;
        private readonly WireframeSphere lastKnownHiveNearSphere;
        private readonly WireframeSphere lastKnownHiveLineOfSightSphere;
        private readonly LineRenderer beeEyeToLastKnownHiveLine;
        private readonly LineRenderer beeEyeToHiveLine;
        private readonly GameObject beeMarker;
        private readonly GameObject hiveMarker;
        private readonly GameObject lastKnownHiveMarker;
        private readonly GameObject playerMarker;

        private BeeView(
            GameObject worldRoot,
            WireframeSphere beeSightRangeSphere,
            WireframeSphere defenseDistanceSphere,
            LineRenderer visiblePlayerSightLine,
            WireframeSphere lastKnownHiveNearSphere,
            WireframeSphere lastKnownHiveLineOfSightSphere,
            LineRenderer beeEyeToLastKnownHiveLine,
            LineRenderer beeEyeToHiveLine,
            GameObject beeMarker,
            GameObject hiveMarker,
            GameObject lastKnownHiveMarker,
            GameObject playerMarker
        )
        {
            this.worldRoot = worldRoot;
            this.beeSightRangeSphere = beeSightRangeSphere;
            this.defenseDistanceSphere = defenseDistanceSphere;
            this.visiblePlayerSightLine = visiblePlayerSightLine;
            this.lastKnownHiveNearSphere = lastKnownHiveNearSphere;
            this.lastKnownHiveLineOfSightSphere = lastKnownHiveLineOfSightSphere;
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

            var beeSightRangeSphere = WireframeSphere.Create("BeeSightRangeSphere", worldRoot.transform, lineMaterial);
            var defenseDistanceSphere = WireframeSphere.Create("DefenseDistanceSphere", worldRoot.transform, lineMaterial);
            var visiblePlayerSightLine = CreateWorldLine("VisiblePlayerSightLine", worldRoot.transform, lineMaterial);
            var lastKnownHiveNearSphere = WireframeSphere.Create("LastKnownHiveNearSphere", worldRoot.transform, lineMaterial);
            var lastKnownHiveLineOfSightSphere = WireframeSphere.Create("LastKnownHiveLineOfSightSphere", worldRoot.transform, lineMaterial);
            var beeEyeToLastKnownHiveLine = CreateWorldLine("BeeEyeToLastKnownHiveLine", worldRoot.transform, lineMaterial);
            var beeEyeToHiveLine = CreateWorldLine("BeeEyeToHiveLine", worldRoot.transform, lineMaterial);
            var beeMarker = CreateWorldMarker("BeeEyeWorldMarker", worldRoot.transform, beeMaterial);
            var hiveMarker = CreateWorldMarker("HiveWorldMarker", worldRoot.transform, hiveMaterial);
            var lastKnownHiveMarker = CreateWorldMarker("LastKnownHiveWorldMarker", worldRoot.transform, lastKnownHiveMaterial);
            var playerMarker = CreateWorldMarker("LocalPlayerWorldMarker", worldRoot.transform, playerMaterial);

            // These guides are conditional frame-by-frame. Start hidden so a newly allocated view
            // never flashes stale geometry before the first real sample is written.
            beeSightRangeSphere.SetVisible(false);
            defenseDistanceSphere.SetVisible(false);
            visiblePlayerSightLine.gameObject.SetActive(false);
            lastKnownHiveNearSphere.SetVisible(false);
            lastKnownHiveLineOfSightSphere.SetVisible(false);
            beeEyeToLastKnownHiveLine.gameObject.SetActive(false);
            beeEyeToHiveLine.gameObject.SetActive(false);
            beeMarker.SetActive(false);
            hiveMarker.SetActive(false);
            lastKnownHiveMarker.SetActive(false);
            playerMarker.SetActive(false);

            return new BeeView(
                worldRoot,
                beeSightRangeSphere,
                defenseDistanceSphere,
                visiblePlayerSightLine,
                lastKnownHiveNearSphere,
                lastKnownHiveLineOfSightSphere,
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

            // This yellow sphere is the spatial version of the 16u player sight range used by the
            // base game's CheckLineOfSightForPlayer call. It is centered on the bee eye because
            // both the real player check and the hive pickup proxy start their visibility test
            // from that point.
            beeSightRangeSphere.Set(beeEye, OverlayRules.PlayerLineOfSightDistance, BeeColor);

            // RedLocustBees stores defenseDistance as an integer radius around the hive. A sphere
            // makes the full three-dimensional trigger range visible before it is crossed.
            defenseDistanceSphere.Set(hive, defenseDistance, HiveColor);
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
            var hiveTarget = ToUnityVector3(probe.HivePosition) + Vector3.up * WorldYOffset;
            var lineColor = probe.CanSeePickupProxy ? PickupProxyColor : InactiveLineColor;
            SetWorldLine(beeEyeToHiveLine, beeEye, hiveTarget, lineColor);
            beeEyeToHiveLine.gameObject.SetActive(true);
        }

        private void SetHiveMissingProbe(Vector3 beeEye, HiveMissingProbe probe)
        {
            var lastKnownHive =
                ToUnityVector3(probe.LastKnownHivePosition) + Vector3.up * WorldYOffset;
            lastKnownHiveMarker.SetActive(true);
            lastKnownHiveMarker.transform.position = lastKnownHive;

            // Both spheres use the last-known-hive blue family, but not the exact same shade: the
            // 4u close-range trigger is darker and more urgent, while the 8u line-of-sight gate is
            // lighter so it reads as the outer context instead of competing with the inner ring.
            lastKnownHiveNearSphere.Set(
                lastKnownHive,
                OverlayRules.HiveMissingNearDistance,
                LastKnownHiveNearSphereColor
            );
            lastKnownHiveLineOfSightSphere.Set(
                lastKnownHive,
                OverlayRules.HiveMissingLineOfSightDistance,
                LastKnownHiveLineOfSightSphereColor
            );

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
            // Lines are recreated in place each frame instead of pooled per condition. The overlay
            // has a fixed small number of diagnostics per bee, so clarity beats a more abstract
            // renderer registry here.
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startColor = color;
            line.endColor = color;
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

        private sealed class WireframeSphere
        {
            private readonly LineRenderer equator;
            private readonly LineRenderer northLatitude;
            private readonly LineRenderer southLatitude;
            private readonly LineRenderer meridian0;
            private readonly LineRenderer meridian60;
            private readonly LineRenderer meridian120;

            private WireframeSphere(
                LineRenderer equator,
                LineRenderer northLatitude,
                LineRenderer southLatitude,
                LineRenderer meridian0,
                LineRenderer meridian60,
                LineRenderer meridian120
            )
            {
                this.equator = equator;
                this.northLatitude = northLatitude;
                this.southLatitude = southLatitude;
                this.meridian0 = meridian0;
                this.meridian60 = meridian60;
                this.meridian120 = meridian120;
            }

            public static WireframeSphere Create(string name, Transform parent, Material material)
            {
                return new WireframeSphere(
                    CreateWorldLine($"{name}Equator", parent, material),
                    CreateWorldLine($"{name}NorthLatitude", parent, material),
                    CreateWorldLine($"{name}SouthLatitude", parent, material),
                    CreateWorldLine($"{name}Meridian0", parent, material),
                    CreateWorldLine($"{name}Meridian60", parent, material),
                    CreateWorldLine($"{name}Meridian120", parent, material)
                );
            }

            public void Set(Vector3 center, float radius, Color color)
            {
                if (radius <= 0f)
                {
                    // A zero/negative radius means the base-game field is not useful for spatial
                    // guidance. Hide the guide instead of drawing a degenerate point that could
                    // be mistaken for a real marker.
                    SetVisible(false);
                    return;
                }

                SetVisible(true);
                SetLatitude(equator, center, radius, 0f, color);
                SetLatitude(northLatitude, center, radius, WireframeLatitudeOffsetFactor, color);
                SetLatitude(southLatitude, center, radius, -WireframeLatitudeOffsetFactor, color);
                SetMeridian(meridian0, center, radius, 0f, color);
                SetMeridian(meridian60, center, radius, Mathf.PI / 3f, color);
                SetMeridian(meridian120, center, radius, Mathf.PI * 2f / 3f, color);
            }

            public void SetVisible(bool visible)
            {
                equator.gameObject.SetActive(visible);
                northLatitude.gameObject.SetActive(visible);
                southLatitude.gameObject.SetActive(visible);
                meridian0.gameObject.SetActive(visible);
                meridian60.gameObject.SetActive(visible);
                meridian120.gameObject.SetActive(visible);
            }

            private static void SetLatitude(
                LineRenderer line,
                Vector3 center,
                float radius,
                float heightFactor,
                Color color
            )
            {
                line.positionCount = WireframeSphereSegments + 1;
                line.startColor = color;
                line.endColor = color;

                var circleRadius = radius * (
                    heightFactor == 0f ? 1f : WireframeLatitudeRadiusFactor
                );
                var height = radius * heightFactor;

                for (var i = 0; i <= WireframeSphereSegments; i++)
                {
                    var radians = Mathf.PI * 2f * i / WireframeSphereSegments;
                    var offset = new Vector3(
                        Mathf.Cos(radians) * circleRadius,
                        height,
                        Mathf.Sin(radians) * circleRadius
                    );
                    line.SetPosition(i, center + offset);
                }
            }

            private static void SetMeridian(
                LineRenderer line,
                Vector3 center,
                float radius,
                float longitude,
                Color color
            )
            {
                line.positionCount = WireframeSphereSegments + 1;
                line.startColor = color;
                line.endColor = color;

                var horizontalX = Mathf.Cos(longitude);
                var horizontalZ = Mathf.Sin(longitude);

                for (var i = 0; i <= WireframeSphereSegments; i++)
                {
                    var radians = Mathf.PI * 2f * i / WireframeSphereSegments;
                    var horizontalRadius = Mathf.Cos(radians) * radius;
                    var offset = new Vector3(
                        horizontalX * horizontalRadius,
                        Mathf.Sin(radians) * radius,
                        horizontalZ * horizontalRadius
                    );
                    line.SetPosition(i, center + offset);
                }
            }
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
