using BeeOverlay.Core.Models;
using BeeOverlay.Core.Presentation;

namespace BeeOverlay.Core.Ports;

/// <summary>
/// Presents one Core frame through the current HUD and owned world objects.
/// </summary>
internal interface IOverlayPresenter
{
    bool TryPrepare(bool hudEnabled);

    void Present(
        OverlayFrame frame,
        OverlayPresentationOptions options,
        int? selectedBeeIdentity
    );

    void DisplayTip(HudTipMessage message);

    void HideAll();

    void LogWaitingForHud();
}
