using BeeOverlay.Core.Models;
using BeeOverlay.Core.Ports;
using BeeOverlay.Core.Presentation;
using BeeOverlay.Core.State;
using BeeOverlay.Core.UseCases;

namespace BeeOverlay.Core.Handlers;

/// <summary>
/// Coordinates one coherent observation and presentation for each HUD update.
/// </summary>
internal sealed class FrameHandler
{
    private readonly IOverlayInput overlayInput;
    private readonly IOverlayObservationSource observationSource;
    private readonly IOverlayPresenter presenter;
    private readonly BuildOverlayFrameUseCase buildOverlayFrameUseCase;
    private readonly WorldGuideSelection worldGuideSelection;

    public FrameHandler(
        IOverlayInput overlayInput,
        IOverlayObservationSource observationSource,
        IOverlayPresenter presenter,
        BuildOverlayFrameUseCase buildOverlayFrameUseCase,
        WorldGuideSelection worldGuideSelection
    )
    {
        this.overlayInput = overlayInput;
        this.observationSource = observationSource;
        this.presenter = presenter;
        this.buildOverlayFrameUseCase = buildOverlayFrameUseCase;
        this.worldGuideSelection = worldGuideSelection;
    }

    public void HandleFrame(
        bool enabled,
        bool targetSelectionAllowed,
        OverlayPresentationOptions options
    )
    {
        if (!enabled || !options.HasVisibleElement)
        {
            // Hiding the complete owned presentation makes disabling immediate even when the HUD
            // survives a scene transition. No game state is sampled while the diagnostic is off.
            presenter.HideAll();
            return;
        }

        if (!targetSelectionAllowed)
        {
            // Report the rejected input without observing bees, so a denied client cannot infer
            // whether a selectable target exists.
            presenter.HideAll();
            if (overlayInput.CycleWorldGuideTargetTriggered)
            {
                presenter.DisplayTip(HudTipMessage.TargetSelectionNotPermitted);
            }

            return;
        }

        // Treat the overlay as disposable scene UI. If the vanilla HUD is not ready, hiding world
        // probes is safer than leaving old markers in the scene with no matching status text.
        if (!presenter.TryPrepare(options.HudEnabled))
        {
            presenter.HideAll();
            presenter.LogWaitingForHud();
            return;
        }

        var observation = observationSource.Capture();
        var frame = buildOverlayFrameUseCase.Execute(observation);
        var selection = worldGuideSelection.Update(
            frame,
            overlayInput.CycleWorldGuideTargetTriggered
        );
        if (selection.TipMessage != null)
        {
            presenter.DisplayTip(selection.TipMessage);
        }

        presenter.Present(frame, options, selection.SelectedBeeIdentity);
    }
}
