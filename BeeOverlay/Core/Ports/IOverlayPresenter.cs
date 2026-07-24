using BeeOverlay.Core.Models;

namespace BeeOverlay.Core.Ports;

/// <summary>
/// Presents one Core frame through the current HUD and owned world objects.
/// </summary>
internal interface IOverlayPresenter
{
    bool TryPrepare(bool hudEnabled);

    void Present(OverlayFrame frame, OverlayPresentationOptions options);

    void HideAll();

    void LogWaitingForHud();
}
