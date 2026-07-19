#nullable enable

using BeeOverlay.Interop;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace BeeOverlay;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("Lethal Company.exe")]
public sealed class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource? Log { get; private set; }

    private Harmony? harmony;

    private void Awake()
    {
        // Keep the loader entry point as the composition root. Game sampling, HUD lifecycle, and
        // rendering stay behind the Interop boundary so plugin startup does not accumulate policy.
        Log = Logger;
        Overlay.Instance = new Overlay();
        harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll(typeof(Plugin).Assembly);
        Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} loaded.");
    }
}
