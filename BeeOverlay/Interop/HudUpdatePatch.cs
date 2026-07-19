#nullable enable

extern alias LethalCompany;

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
        Overlay.Instance?.Tick();
    }
}
