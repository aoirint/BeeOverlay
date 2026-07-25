#nullable enable

using BeeOverlay.Core.Models;
using BeeOverlay.Core.Presentation;

namespace BeeOverlay.Core.State;

/// <summary>
/// Retains the bee identity selected for world-guide presentation.
/// </summary>
internal sealed class WorldGuideSelection
{
    private int? selectedBeeIdentity;

    public void Reset()
    {
        selectedBeeIdentity = null;
    }

    public WorldGuideSelectionResult Update(OverlayFrame frame, bool cycleTriggered)
    {
        var selectedIndex = FindSelectedIndex(frame);
        if (selectedBeeIdentity.HasValue && selectedIndex < 0)
        {
            selectedBeeIdentity = null;
            return new WorldGuideSelectionResult(
                selectedBeeIdentity,
                HudTipMessage.SelectedBeeRemoved
            );
        }

        if (!cycleTriggered)
        {
            return new WorldGuideSelectionResult(selectedBeeIdentity, tipMessage: null);
        }

        if (frame.Bees.Count == 0)
        {
            return new WorldGuideSelectionResult(
                selectedBeeIdentity,
                HudTipMessage.SelectNoBee
            );
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

        return new WorldGuideSelectionResult(selectedBeeIdentity, tipMessage: null);
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
