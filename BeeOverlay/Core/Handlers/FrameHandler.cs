using BeeOverlay.Core.Ports;
using BeeOverlay.Core.UseCases;

namespace BeeOverlay.Core.Handlers;

/// <summary>
/// Coordinates one coherent observation and presentation for each HUD update.
/// </summary>
internal sealed class FrameHandler
{
    private readonly IOverlayObservationSource observationSource;
    private readonly IOverlayPresenter presenter;
    private readonly BuildOverlayFrameUseCase buildOverlayFrameUseCase;

    public FrameHandler(
        IOverlayObservationSource observationSource,
        IOverlayPresenter presenter,
        BuildOverlayFrameUseCase buildOverlayFrameUseCase
    )
    {
        this.observationSource = observationSource;
        this.presenter = presenter;
        this.buildOverlayFrameUseCase = buildOverlayFrameUseCase;
    }

    public void HandleFrame()
    {
        // Treat the overlay as disposable scene UI. If the vanilla HUD is not ready, hiding world
        // probes is safer than leaving old markers in the scene with no matching status text.
        if (!presenter.TryPrepare())
        {
            presenter.HideAll();
            presenter.LogWaitingForHud();
            return;
        }

        var observation = observationSource.Capture();
        var frame = buildOverlayFrameUseCase.Execute(observation);
        presenter.Present(frame);
    }
}
