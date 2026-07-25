#nullable enable

using BeeOverlay.Core.Presentation;

namespace BeeOverlay.Core.State;

/// <summary>
/// Carries the selected identity and any transient feedback from one selection update.
/// </summary>
internal sealed class WorldGuideSelectionResult
{
    public WorldGuideSelectionResult(int? selectedBeeIdentity, HudTipMessage? tipMessage)
    {
        SelectedBeeIdentity = selectedBeeIdentity;
        TipMessage = tipMessage;
    }

    public int? SelectedBeeIdentity { get; }

    public HudTipMessage? TipMessage { get; }
}
