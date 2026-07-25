using BeeOverlay.Core.Models;
using BepInEx.Configuration;

namespace BeeOverlay.Interop;

/// <summary>
/// Binds live BepInEx settings for the overlay's global and presentation switches.
/// </summary>
internal sealed class BeeOverlayConfiguration
{
    private readonly ConfigEntry<bool> enabled;
    private readonly ConfigEntry<bool> guestEnabled;
    private readonly ConfigEntry<bool> overlayEnabled;
    private readonly ConfigEntry<bool> hudEnabled;
    private readonly ConfigEntry<bool> beeMarkerEnabled;
    private readonly ConfigEntry<bool> hiveMarkerEnabled;
    private readonly ConfigEntry<bool> knownHiveMarkerEnabled;
    private readonly ConfigEntry<bool> playerMarkerEnabled;
    private readonly ConfigEntry<bool> playerSightLineEnabled;
    private readonly ConfigEntry<bool> beeSightRangeSphereEnabled;
    private readonly ConfigEntry<bool> hiveDefenseSphereEnabled;
    private readonly ConfigEntry<bool> knownHiveNearSphereEnabled;
    private readonly ConfigEntry<bool> knownHiveLineOfSightSphereEnabled;
    private readonly ConfigEntry<bool> knownHiveProbeLineEnabled;
    private readonly ConfigEntry<bool> hivePickupSightLineEnabled;

    private BeeOverlayConfiguration(
        ConfigEntry<bool> enabled,
        ConfigEntry<bool> guestEnabled,
        ConfigEntry<bool> overlayEnabled,
        ConfigEntry<bool> hudEnabled,
        ConfigEntry<bool> beeMarkerEnabled,
        ConfigEntry<bool> hiveMarkerEnabled,
        ConfigEntry<bool> knownHiveMarkerEnabled,
        ConfigEntry<bool> playerMarkerEnabled,
        ConfigEntry<bool> playerSightLineEnabled,
        ConfigEntry<bool> beeSightRangeSphereEnabled,
        ConfigEntry<bool> hiveDefenseSphereEnabled,
        ConfigEntry<bool> knownHiveNearSphereEnabled,
        ConfigEntry<bool> knownHiveLineOfSightSphereEnabled,
        ConfigEntry<bool> knownHiveProbeLineEnabled,
        ConfigEntry<bool> hivePickupSightLineEnabled)
    {
        this.enabled = enabled;
        this.guestEnabled = guestEnabled;
        this.overlayEnabled = overlayEnabled;
        this.hudEnabled = hudEnabled;
        this.beeMarkerEnabled = beeMarkerEnabled;
        this.hiveMarkerEnabled = hiveMarkerEnabled;
        this.knownHiveMarkerEnabled = knownHiveMarkerEnabled;
        this.playerMarkerEnabled = playerMarkerEnabled;
        this.playerSightLineEnabled = playerSightLineEnabled;
        this.beeSightRangeSphereEnabled = beeSightRangeSphereEnabled;
        this.hiveDefenseSphereEnabled = hiveDefenseSphereEnabled;
        this.knownHiveNearSphereEnabled = knownHiveNearSphereEnabled;
        this.knownHiveLineOfSightSphereEnabled = knownHiveLineOfSightSphereEnabled;
        this.knownHiveProbeLineEnabled = knownHiveProbeLineEnabled;
        this.hivePickupSightLineEnabled = hivePickupSightLineEnabled;
    }

    public bool Enabled => enabled.Value;

    public bool GuestEnabled => guestEnabled.Value;

    public bool OverlayEnabled => overlayEnabled.Value;

    public OverlayPresentationOptions PresentationOptions => new(
        hudEnabled.Value,
        beeMarkerEnabled.Value,
        hiveMarkerEnabled.Value,
        knownHiveMarkerEnabled.Value,
        playerMarkerEnabled.Value,
        playerSightLineEnabled.Value,
        beeSightRangeSphereEnabled.Value,
        hiveDefenseSphereEnabled.Value,
        knownHiveNearSphereEnabled.Value,
        knownHiveLineOfSightSphereEnabled.Value,
        knownHiveProbeLineEnabled.Value,
        hivePickupSightLineEnabled.Value);

    public static BeeOverlayConfiguration Bind(ConfigFile config)
    {
        var enabled = BindGeneral(config, "Enabled", "Set to false to disable all BeeOverlay functionality.");
        var guestEnabled = BindGeneral(config, "GuestEnabled", "Set to false to disallow non-host players from using BeeOverlay when this player hosts the lobby.");
        var overlayEnabled = BindOverlay(config, "Enabled", "Set to false to hide every BeeOverlay element while keeping general functionality enabled.");
        var hudEnabled = BindOverlay(config, "HudEnabled", "Set to false to hide BeeOverlay's HUD text while keeping enabled world guides available.");
        var beeMarkerEnabled = BindOverlay(config, "BeeMarkerEnabled", "Set to false to hide bee markers.");
        var hiveMarkerEnabled = BindOverlay(config, "HiveMarkerEnabled", "Set to false to hide hive markers.");
        var knownHiveMarkerEnabled = BindOverlay(config, "KnownHiveMarkerEnabled", "Set to false to hide remembered-hive markers.");
        var playerMarkerEnabled = BindOverlay(config, "PlayerMarkerEnabled", "Set to false to hide local-player markers.");
        var playerSightLineEnabled = BindOverlay(config, "PlayerSightLineEnabled", "Set to false to hide bee-to-player sight lines.");
        var beeSightRangeSphereEnabled = BindOverlay(config, "BeeSightRangeSphereEnabled", "Set to false to hide bee 16-unit sight-range spheres.");
        var hiveDefenseSphereEnabled = BindOverlay(config, "HiveDefenseSphereEnabled", "Set to false to hide hive defense-range spheres.");
        var knownHiveNearSphereEnabled = BindOverlay(config, "KnownHiveNearSphereEnabled", "Set to false to hide remembered-hive 4-unit spheres.");
        var knownHiveLineOfSightSphereEnabled = BindOverlay(config, "KnownHiveLineOfSightSphereEnabled", "Set to false to hide remembered-hive 8-unit line-of-sight spheres.");
        var knownHiveProbeLineEnabled = BindOverlay(config, "KnownHiveProbeLineEnabled", "Set to false to hide bee-to-remembered-hive probe lines.");
        var hivePickupSightLineEnabled = BindOverlay(config, "HivePickupSightLineEnabled", "Set to false to hide bee-to-hive pickup-proxy sight lines.");

        return new BeeOverlayConfiguration(
            enabled,
            guestEnabled,
            overlayEnabled,
            hudEnabled,
            beeMarkerEnabled,
            hiveMarkerEnabled,
            knownHiveMarkerEnabled,
            playerMarkerEnabled,
            playerSightLineEnabled,
            beeSightRangeSphereEnabled,
            hiveDefenseSphereEnabled,
            knownHiveNearSphereEnabled,
            knownHiveLineOfSightSphereEnabled,
            knownHiveProbeLineEnabled,
            hivePickupSightLineEnabled);
    }

    private static ConfigEntry<bool> BindGeneral(
        ConfigFile config,
        string key,
        string description)
    {
        return BindGeneral(config, key, true, description);
    }

    private static ConfigEntry<bool> BindGeneral(
        ConfigFile config,
        string key,
        bool defaultValue,
        string description)
    {
        return config.Bind("General", key, defaultValue, description);
    }

    private static ConfigEntry<bool> BindOverlay(ConfigFile config, string key, string description)
    {
        return config.Bind("Overlay", key, true, description);
    }
}
