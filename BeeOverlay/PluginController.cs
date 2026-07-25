extern alias LethalCompany;

using BeeOverlay.Core.Handlers;
using BeeOverlay.Core.Ports;
using BeeOverlay.Core.UseCases;
using BeeOverlay.Interop;
using BeeOverlay.Interop.Game;
using BepInEx.Configuration;
using BepInEx.Logging;
using LethalCompany;

namespace BeeOverlay;

/// <summary>
/// Plugin-facing facade that wires game observation and Unity presentation to Core.
/// </summary>
internal sealed class PluginController
{
    private readonly FrameHandler frameHandler;
    private readonly BeeOverlayConfiguration configuration;
    private readonly HostModPresenceGate hostModPresenceGate;

    private PluginController(
        FrameHandler frameHandler,
        BeeOverlayConfiguration configuration,
        HostModPresenceGate hostModPresenceGate
    )
    {
        this.frameHandler = frameHandler;
        this.configuration = configuration;
        this.hostModPresenceGate = hostModPresenceGate;
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

        return new PluginController(frameHandler, configuration, new HostModPresenceGate());
    }

    public void AttachHostModPresence(HUDManager hudManager)
    {
        hostModPresenceGate.Attach(hudManager);
    }

    public void ConfirmHostModPresence()
    {
        hostModPresenceGate.ConfirmHostPresence();
    }

    public void BeginHostModPresenceCheck(HostModPresenceBehaviour bridge)
    {
        hostModPresenceGate.BeginHostPresenceCheck(bridge);
    }

    public HostPresenceRequestResult TryRequestHostModPresence()
    {
        return hostModPresenceGate.TryRequestHostPresence();
    }

    public void ResetHostModPresence()
    {
        hostModPresenceGate.Reset();
    }

    public void HandleFrame()
    {
        frameHandler.HandleFrame(
            configuration.Enabled && hostModPresenceGate.IsOverlayAllowed,
            configuration.PresentationOptions
        );
    }
}
