using Verse;

namespace DF
{
    /// <summary>
    /// The main Mod class loaded by RimWorld's mod manager.
    /// Inheriting from Verse.Mod allows the framework to handle custom mod settings 
    /// and execute logic as soon as the mod's content pack is loaded into the game.
    /// </summary>
    public class DiseasesFrameworkCore : Mod
    {
        /// <summary>
        /// Constructor invoked automatically by RimWorld when the mod is initialized.
        /// </summary>
        /// <param name="content">The content pack representing this mod's files, XMLs, and metadata.</param>
        public DiseasesFrameworkCore(ModContentPack content) : base(content)
        {
            // Prints a green success message to the debug log to confirm the core mod class initialized correctly.
            // This happens very early in the loading process, before Harmony patches are applied.
            Log.Message("<color=green>[Disease Framework] Successfully loaded</color>");
        }
    }
}