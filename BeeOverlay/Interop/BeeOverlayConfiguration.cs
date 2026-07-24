using BeeOverlay.Core.Models;
using BepInEx.Configuration;

namespace BeeOverlay.Interop;

/// <summary>
/// Binds live BepInEx settings for the overlay's global and presentation switches.
/// </summary>
internal sealed class BeeOverlayConfiguration
{
    private readonly ConfigEntry<bool> enabled;
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
        var enabled = Bind(config, "Enabled", "Set to false to disable BeeOverlay. Changes made through BepInEx configuration APIs apply on the next HUD update.");
        var hudEnabled = Bind(config, "HudEnabled", "Set to false to hide BeeOverlay's HUD text while keeping enabled world guides available.");
        var beeMarkerEnabled = Bind(config, "BeeMarkerEnabled", "Set to false to hide bee markers.");
        var hiveMarkerEnabled = Bind(config, "HiveMarkerEnabled", "Set to false to hide hive markers.");
        var knownHiveMarkerEnabled = Bind(config, "KnownHiveMarkerEnabled", "Set to false to hide remembered-hive markers.");
        var playerMarkerEnabled = Bind(config, "PlayerMarkerEnabled", "Set to false to hide local-player markers.");
        var playerSightLineEnabled = Bind(config, "PlayerSightLineEnabled", "Set to false to hide bee-to-player sight lines.");
        var beeSightRangeSphereEnabled = Bind(config, "BeeSightRangeSphereEnabled", "Set to false to hide bee 16-unit sight-range spheres.");
        var hiveDefenseSphereEnabled = Bind(config, "HiveDefenseSphereEnabled", "Set to false to hide hive defense-range spheres.");
        var knownHiveNearSphereEnabled = Bind(config, "KnownHiveNearSphereEnabled", "Set to false to hide remembered-hive 4-unit spheres.");
        var knownHiveLineOfSightSphereEnabled = Bind(config, "KnownHiveLineOfSightSphereEnabled", "Set to false to hide remembered-hive 8-unit line-of-sight spheres.");
        var knownHiveProbeLineEnabled = Bind(config, "KnownHiveProbeLineEnabled", "Set to false to hide bee-to-remembered-hive probe lines.");
        var hivePickupSightLineEnabled = Bind(config, "HivePickupSightLineEnabled", "Set to false to hide bee-to-hive pickup-proxy sight lines.");

        return new BeeOverlayConfiguration(
            enabled,
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

    private static ConfigEntry<bool> Bind(ConfigFile config, string key, string description)
    {
        return config.Bind("General", key, true, description);
    }
}
