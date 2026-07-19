using BeeOverlay.Core.Snapshots;

namespace BeeOverlay.Core.Ports;

/// <summary>
/// Captures the game values needed by one overlay frame without exposing game objects.
/// </summary>
internal interface IOverlayObservationSource
{
    OverlayFrameObservation Capture();
}
