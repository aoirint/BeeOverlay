using BeeOverlay.Core.Models;

namespace BeeOverlay.Core.Ports;

/// <summary>
/// Presents one Core frame through the current HUD and owned world objects.
/// </summary>
internal interface IOverlayPresenter
{
    bool TryPrepare();

    void Present(OverlayFrame frame);

    void HideAll();

    void LogWaitingForHud();
}
