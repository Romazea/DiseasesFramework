using Verse;
using HarmonyLib;
using System.Reflection;

namespace DiseasesFramework.Core
{
    /// <summary>
    /// Main initialization class for the framework.
    /// The [StaticConstructorOnStartup] attribute tells RimWorld to execute this code 
    /// automatically during the loading screen, right after processing all XML defs.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class DiseasesFramework_Init
    {
        /// <summary>
        /// Static constructor executed upon game startup. 
        /// Responsible for instantiating the Harmony library and applying all the mod's patches.
        /// </summary>
        static DiseasesFramework_Init()
        {
            // 1. Harmony Initialization: 
            // Creates a unique instance using the mod's package ID. 
            // It is vital that this ID is unique to prevent conflicts with other community mods.
            Harmony harmony = new Harmony("com.romazea.diseasesframework");

            // 2. Patch Execution:
            // Instructs Harmony to scan this entire assembly (.dll) for classes 
            // marked with [HarmonyPatch] and inject them into the base game's code.
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // 3. Visual Confirmation:
            // Prints a message to the debug console indicating the patching engine started successfully.
            // Using <color> tags helps modders spot our mod in a sea of logs.
            Log.Message("<color=cyan>[Disease Framework] Harmony patches applied successfully.</color>");
        }
    }
}