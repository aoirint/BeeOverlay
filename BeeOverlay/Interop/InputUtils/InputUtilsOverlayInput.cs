#nullable enable

using BeeOverlay.Core.Ports;

namespace BeeOverlay.Interop.InputUtils;

/// <summary>
/// Adapts InputUtils actions to Core overlay input.
/// </summary>
internal sealed class InputUtilsOverlayInput : IOverlayInput
{
    private readonly InputUtilsActions inputActions;

    public InputUtilsOverlayInput(InputUtilsActions inputActions)
    {
        this.inputActions = inputActions;
    }

    public bool CycleWorldGuideTargetTriggered =>
        inputActions.CycleWorldGuideTargetKey?.triggered ?? false;
}
