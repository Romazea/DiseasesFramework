using Verse;
using HarmonyLib;
using System.Reflection;

namespace DiseasesFramework.Core
{
    [StaticConstructorOnStartup]
    public static class DiseasesFramework_Init
    {
        static DiseasesFramework_Init()
        {
            Harmony harmony = new Harmony("com.romazea.diseasesframework");

            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log.Message("<color=cyan>[Disease Framework] Harmony patches applied successfully.</color>");
        }
    }
}