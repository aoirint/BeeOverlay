#nullable enable

extern alias LethalCompany;

using System;
using HarmonyLib;
using LethalCompany;

namespace BeeOverlay.Interop;

[HarmonyPatch(typeof(HUDManager))]
internal static class HudUpdatePatch
{
    [HarmonyPatch(nameof(HUDManager.Awake))]
    [HarmonyPostfix]
    private static void AwakePostfix(HUDManager __instance)
    {
        try
        {
            Plugin.Controller.AttachHostModPresence(__instance);
        }
        catch (Exception error)
        {
            LogCallbackFailure("Host-presence setup", error);
        }
    }

    [HarmonyPatch(nameof(HUDManager.Update))]
    [HarmonyPostfix]
    private static void UpdatePostfix()
    {
        // HUDManager.Update runs during normal gameplay and already has the current HUD context.
        // Driving the overlay here keeps the visualization in sync without adding another
        // MonoBehaviour object to manage.
        try
        {
            Plugin.Controller.HandleFrame();
        }
        catch (Exception error)
        {
            // The overlay is diagnostic-only. A failed observation or presentation must not break
            // HUDManager.Update, and a logger failure must not escape the callback either.
            LogCallbackFailure("Overlay update", error);
        }
    }

    private static void LogCallbackFailure(string operation, Exception error)
    {
        try
        {
            Plugin.Log?.LogError($"{operation} failed: {error}");
        }
        catch
        {
            // Logging cannot safely report its own failure at this Harmony boundary.
        }
    }
}
