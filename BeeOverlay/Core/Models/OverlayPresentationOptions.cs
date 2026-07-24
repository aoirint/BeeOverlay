namespace BeeOverlay.Core.Models;

/// <summary>
/// Selects the independently visible parts of an enabled overlay update.
/// </summary>
internal readonly struct OverlayPresentationOptions
{
    public bool HudEnabled { get; }

    public bool BeeMarkerEnabled { get; }

    public bool HiveMarkerEnabled { get; }

    public bool KnownHiveMarkerEnabled { get; }

    public bool PlayerMarkerEnabled { get; }

    public bool PlayerSightLineEnabled { get; }

    public bool BeeSightRangeSphereEnabled { get; }

    public bool HiveDefenseSphereEnabled { get; }

    public bool KnownHiveNearSphereEnabled { get; }

    public bool KnownHiveLineOfSightSphereEnabled { get; }

    public bool KnownHiveProbeLineEnabled { get; }

    public bool HivePickupSightLineEnabled { get; }

    public bool HasWorldGuides =>
        BeeMarkerEnabled ||
        HiveMarkerEnabled ||
        KnownHiveMarkerEnabled ||
        PlayerMarkerEnabled ||
        PlayerSightLineEnabled ||
        BeeSightRangeSphereEnabled ||
        HiveDefenseSphereEnabled ||
        KnownHiveNearSphereEnabled ||
        KnownHiveLineOfSightSphereEnabled ||
        KnownHiveProbeLineEnabled ||
        HivePickupSightLineEnabled;

    public bool HasVisibleElement => HudEnabled || HasWorldGuides;

    public OverlayPresentationOptions(
        bool hudEnabled,
        bool beeMarkerEnabled,
        bool hiveMarkerEnabled,
        bool knownHiveMarkerEnabled,
        bool playerMarkerEnabled,
        bool playerSightLineEnabled,
        bool beeSightRangeSphereEnabled,
        bool hiveDefenseSphereEnabled,
        bool knownHiveNearSphereEnabled,
        bool knownHiveLineOfSightSphereEnabled,
        bool knownHiveProbeLineEnabled,
        bool hivePickupSightLineEnabled)
    {
        HudEnabled = hudEnabled;
        BeeMarkerEnabled = beeMarkerEnabled;
        HiveMarkerEnabled = hiveMarkerEnabled;
        KnownHiveMarkerEnabled = knownHiveMarkerEnabled;
        PlayerMarkerEnabled = playerMarkerEnabled;
        PlayerSightLineEnabled = playerSightLineEnabled;
        BeeSightRangeSphereEnabled = beeSightRangeSphereEnabled;
        HiveDefenseSphereEnabled = hiveDefenseSphereEnabled;
        KnownHiveNearSphereEnabled = knownHiveNearSphereEnabled;
        KnownHiveLineOfSightSphereEnabled = knownHiveLineOfSightSphereEnabled;
        KnownHiveProbeLineEnabled = knownHiveProbeLineEnabled;
        HivePickupSightLineEnabled = hivePickupSightLineEnabled;
    }
}
