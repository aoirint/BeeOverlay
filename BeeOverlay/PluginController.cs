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
    private readonly ConfigEntry<bool> enabled;

    private PluginController(FrameHandler frameHandler, ConfigEntry<bool> enabled)
    {
        this.frameHandler = frameHandler;
        this.enabled = enabled;
    }

    public static PluginController Create(ManualLogSource logger, ConfigFile config)
    {
        ConfigEntry<bool> enabled = config.Bind(
            "General",
            "Enabled",
            true,
            "Set to false to disable BeeOverlay. Changes made through BepInEx configuration APIs apply on the next HUD update.");
        IOverlayObservationSource observationSource = new BeeObservationSource(logger);
        IOverlayPresenter presenter = new Overlay(logger);
        var frameHandler = new FrameHandler(
            observationSource: observationSource,
            presenter: presenter,
            buildOverlayFrameUseCase: new BuildOverlayFrameUseCase()
        );

        return new PluginController(frameHandler, enabled);
    }

    public void HandleFrame()
    {
        frameHandler.HandleFrame(enabled.Value);
    }
}
