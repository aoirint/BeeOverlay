#nullable enable

namespace BeeOverlay.Core.Ports;

/// <summary>
/// Provides one-frame overlay input intentions without exposing the input framework.
/// </summary>
internal interface IOverlayInput
{
    bool CycleWorldGuideTargetTriggered { get; }
}
