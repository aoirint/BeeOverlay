#nullable enable

extern alias LethalCompany;

using System;
using HarmonyLib;
using LethalCompany;

namespace BeeOverlay.Interop;

[HarmonyPatch(typeof(HUDManager), "Update")]
internal static class HudUpdatePatch
{
    [HarmonyPostfix]
    private static void Postfix()
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
            try
            {
                Plugin.Log?.LogError($"Overlay update failed: {error}");
            }
            catch
            {
                // Logging cannot safely report its own failure at this Harmony boundary.
            }
        }
    }
}
