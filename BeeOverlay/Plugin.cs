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

    private static PluginController? controller;

    internal static PluginController Controller => controller!;

    private Harmony? harmony;

    private void Awake()
    {
        // Keep the loader entry point small. The controller composition root wires Core to game
        // observation and Unity presentation before the first Harmony callback can run.
        Log = Logger;
        controller = PluginController.Create(Logger);
        harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll(typeof(Plugin).Assembly);
        Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} loaded.");
    }
}
