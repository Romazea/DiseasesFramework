using Verse;
using RimWorld;

namespace DiseasesFramework.InfectionVectors.DF_Corpses
{
    /// <summary>
    /// XML properties for the Corpse Contagion component.
    /// Defines how a disease spreads from a corpse to nearby living pawns via environmental exposure.
    /// </summary>
    public class HediffCompProperties_CorpseContagion : HediffCompProperties
    {
        /// <summary>The distance (in tiles) within which the corpse can infect nearby pawns.</summary>
        public float radius = 4f;

        /// <summary>Base probability (0.0 to 1.0) of infecting a pawn inside the radius per pulse/tick check.</summary>
        public float infectionChance = 0.5f;

        /// <summary>If true, the infection cannot pass through walls or solid objects.</summary>
        public bool requireLineOfSight = true;

        /// <summary>Can the corpse infect others immediately after death?</summary>
        public bool activeWhenFresh = true;

        /// <summary>Can the corpse infect others while rotting and emitting miasma?</summary>
        public bool activeWhenRotting = true;

        /// <summary>Can the corpse infect others when reduced to a skeleton?</summary>
        public bool activeWhenDessicated = false;

        /// <summary>If true, gas masks and toxic resistance stats reduce the infection chance.</summary>
        public bool respectsToxicResistance = true;

        /// <summary>Whether to notify the player when a colonist contracts a disease from a corpse.</summary>
        public bool sendNotification = true;

        /// <summary>If true, sends a high-priority Letter; otherwise, uses a standard top-left Message.</summary>
        public bool useLetterInsteadOfMessage = false;

        public HediffCompProperties_CorpseContagion()
        {
            this.compClass = typeof(HediffComp_CorpseContagion);
        }
    }

    /// <summary>
    /// Active component that handles infection logic emanating from a corpse.
    /// Used by the framework to identify which corpses are hazardous and their respective infection parameters.
    /// </summary>
    public class HediffComp_CorpseContagion : HediffComp
    {
        /// <summary>
        /// Provides easy access to the XML-defined properties for this component.
        /// </summary>
        public HediffCompProperties_CorpseContagion Props => (HediffCompProperties_CorpseContagion)this.props;
    }
}