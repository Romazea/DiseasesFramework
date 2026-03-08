using Verse;
using RimWorld;

namespace DiseasesFramework.InfectionVectors.DF_Ingestion
{
    /// <summary>
    /// XML properties for the Ingestion Contagion component.
    /// Configures how a disease carried by a pawn can be transferred to its meat or products.
    /// </summary>
    public class HediffCompProperties_IngestionContagion : HediffCompProperties
    {
        /// <summary>Probability (0.0 to 1.0) that the food produced from this pawn will be contaminated.</summary>
        public float contaminationChance = 1.0f;

        /// <summary>
        /// The specific disease to apply to the food. 
        /// If null, the framework should default to the parent disease.
        /// </summary>
        public HediffDef hediffToApply = null;

        public HediffCompProperties_IngestionContagion()
        {
            this.compClass = typeof(HediffComp_IngestionContagion);
        }
    }

    /// <summary>
    /// Active component that marks a disease as "transmissible via ingestion."
    /// Used by butcher and gathering patches to identify if the resulting food should carry a pathogen.
    /// </summary>
    public class HediffComp_IngestionContagion : HediffComp
    {
        /// <summary>
        /// Provides access to the ingestion-specific settings defined in XML.
        /// </summary>
        public HediffCompProperties_IngestionContagion Props => (HediffCompProperties_IngestionContagion)this.props;
    }
}