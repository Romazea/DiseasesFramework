using Verse;
using HarmonyLib;

namespace DiseasesFramework
{
    [StaticConstructorOnStartup]
    public static class DiseasesFramework_Init
    {
        static DiseasesFramework_Init()
        {
            Harmony harmony = new Harmony("com.romazea.diseasesframework");

            harmony.PatchAll();

            Log.Message("<color=cyan>[Disease Framework] Harmony patches applied successfully.</color>");
        }
    }
}