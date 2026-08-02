using HarmonyLib;
using Verse;

namespace SpaMod
{
    [StaticConstructorOnStartup]
    public static class SpaModInit
    {
        static SpaModInit()
        {
            var harmony = new Harmony("johnbrimstone.spa");
            harmony.PatchAll();
            Log.Message("[Spa & Sauna] Loaded.");
        }
    }
}
