#nullable enable

extern alias UnityEngine;

using System.Collections.Generic;
using BeeOverlay.Core.Models;
using BeeOverlay.Core.Snapshots;
using UnityEngine::UnityEngine;

namespace BeeOverlay.Interop;

internal sealed partial class Overlay
{
    private void DrawBee(BeeDiagnostic bee, HashSet<int> seen)
    {
        var observation = bee.Observation;
        var hive = bee.Hive;
        if (hive == null)
        {
            return;
        }

        var view = GetView(observation.Identity);
        // Keep both probes even though they look similar in the scene. bee-hive is the pickup
        // position proxy for state 0 -> 1 reasoning, while lastKnownHive is the remembered-position
        // probe used by the state 0 -> 2 missing-hive path.
        view.SetSpatialGuides(
            ToUnityVector3(hive.Observation.Position) + Vector3.up * WorldYOffset,
            hive.Observation.DefenseDistance,
            ToUnityVector3(observation.EyePosition),
            bee.LocalPlayer != null
                ? ToUnityVector3(bee.LocalPlayer.SightTargetPosition)
                : (Vector3?)null,
            observation.CanSeeLocalPlayer,
            hive.Missing,
            hive.Sight
        );
        seen.Add(observation.Identity);
    }

    private static string GetBeeStatusLine(BeeDiagnostic bee)
    {
        var observation = bee.Observation;
        var hive = bee.Hive;
        if (hive == null)
        {
            return $"{Tag($"bee:{bee.DisplayNumber}", BeeColor)}  hive n/a";
        }

        var hiveSightProbe = hive.Sight;
        var hiveMissingProbe = hive.Missing;

        // HUD rows intentionally avoid transition-derived labels such as missProbe. In practice the
        // player needs quick distances and current game visibility booleans, not another copy of
        // the C# branch structure. The colored terms map to the same entity colors as the 3D
        // dots/lines so the player can glance between HUD and world probes.
        return string.Join(
            "  ",
            Tag($"bee:{bee.DisplayNumber}", BeeColor),
            Tag(
                $"bee-player={FmtDistance(bee.BeeToPlayerDistance)}/{SeenBlocked(observation.CanSeeLocalPlayer)}",
                PlayerColor
            ),
            Tag(
                $"hive-player={FmtDistance(hive.PlayerToHiveDistance)}/{InsideOutside(hive.PlayerToHiveDistance, hive.Observation.DefenseDistance)}",
                HiveColor
            ),
            Tag(
                $"bee-hive={hiveSightProbe.EyeToHiveDistance:F2}u/{SeenBlocked(hiveSightProbe.CanSeePickupProxy)}",
                PickupProxyColor
            ),
            Tag(
                $"bee-knownHive={hiveMissingProbe.EyeToLastKnownHiveDistance:F2}u/{SeenBlocked(!hiveMissingProbe.LinecastBlocked)}",
                LastKnownHiveColor
            )
        );
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

    private static Vector3 ToUnityVector3(Vector3Value value)
    {
        return new Vector3(x: value.X, y: value.Y, z: value.Z);
    }
}
