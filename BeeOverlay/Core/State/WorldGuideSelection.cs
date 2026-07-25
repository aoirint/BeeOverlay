#nullable enable

using BeeOverlay.Core.Models;

namespace BeeOverlay.Core.State;

/// <summary>
/// Retains the bee identity selected for world-guide presentation.
/// </summary>
internal sealed class WorldGuideSelection
{
    private int? selectedBeeIdentity;

    public int? Update(OverlayFrame frame, bool cycleTriggered)
    {
        var selectedIndex = FindSelectedIndex(frame);
        if (selectedBeeIdentity.HasValue && selectedIndex < 0)
        {
            selectedBeeIdentity = null;
        }

        if (!cycleTriggered || frame.Bees.Count == 0)
        {
            return selectedBeeIdentity;
        }

        if (!selectedBeeIdentity.HasValue)
        {
            selectedBeeIdentity = frame.Bees[0].Observation.Identity;
        }
        else if (selectedIndex + 1 < frame.Bees.Count)
        {
            selectedBeeIdentity = frame.Bees[selectedIndex + 1].Observation.Identity;
        }
        else
        {
            selectedBeeIdentity = null;
        }

        return selectedBeeIdentity;
    }

    private int FindSelectedIndex(OverlayFrame frame)
    {
        if (!selectedBeeIdentity.HasValue)
        {
            return -1;
        }

        for (var i = 0; i < frame.Bees.Count; i++)
        {
            if (frame.Bees[i].Observation.Identity == selectedBeeIdentity.Value)
            {
                return i;
            }
        }

        return -1;
    }
}
