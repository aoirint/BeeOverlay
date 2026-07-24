using BeeOverlay.Core.Handlers;
using BeeOverlay.Core.Ports;
using BeeOverlay.Core.UseCases;
using BeeOverlay.Interop;
using BeeOverlay.Interop.Game;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace BeeOverlay;

/// <summary>
/// Plugin-facing facade that wires game observation and Unity presentation to Core.
/// </summary>
internal sealed class PluginController
{
    private readonly FrameHandler frameHandler;
    private readonly BeeOverlayConfiguration configuration;

    private PluginController(FrameHandler frameHandler, BeeOverlayConfiguration configuration)
    {
        this.frameHandler = frameHandler;
        this.configuration = configuration;
    }

    public static PluginController Create(ManualLogSource logger, ConfigFile config)
    {
        BeeOverlayConfiguration configuration = BeeOverlayConfiguration.Bind(config);
        IOverlayObservationSource observationSource = new BeeObservationSource(logger);
        IOverlayPresenter presenter = new Overlay(logger);
        var frameHandler = new FrameHandler(
            observationSource: observationSource,
            presenter: presenter,
            buildOverlayFrameUseCase: new BuildOverlayFrameUseCase()
        );

        return new PluginController(frameHandler, configuration);
    }

    public void HandleFrame()
    {
        frameHandler.HandleFrame(configuration.Enabled, configuration.PresentationOptions);
    }
}
