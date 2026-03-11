using Verse;
using RimWorld;

namespace DiseasesFramework.InfectionVectors.DF_Surgery
{
    /// <summary>
    /// XML properties for contaminated body parts.
    /// This component allows harvested organs and limbs to carry pathogens from their original host.
    /// </summary>
    public class CompProperties_ContaminatedBodyPart : CompProperties
    {
        public CompProperties_ContaminatedBodyPart()
        {
            this.compClass = typeof(CompContaminatedBodyPart);
        }
    }

    /// <summary>
    /// Component that tracks disease data on a physical body part item (e.g., a harvested Heart or Liver).
    /// Used to transmit infections during organ transplants.
    /// </summary>
    public class CompContaminatedBodyPart : ThingComp
    {
        private HediffDef activeDisease;

        /// <summary>Returns the disease currently linked to this body part.</summary>
        public HediffDef ActiveDisease => activeDisease;

        /// <summary>
        /// Manually links a disease to this body part.
        /// Usually called when the organ is harvested from an infected pawn.
        /// </summary>
        /// <param name="disease">The pathogen to store.</param>
        public void Contaminate(HediffDef disease)
        {
            activeDisease = disease;
        }

        /// <summary>Quick check to see if the part is carrying any pathogen.</summary>
        public bool IsContaminated() => activeDisease != null;

        /// <summary>
        /// Ensures the contamination status is preserved through save/load cycles.
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look(ref activeDisease, "activeDisease");
        }

        /// <summary>
        /// Adds a custom line to the object's inspection panel (bottom-left UI).
        /// This warns the player that the organ is contaminated before they try to transplant it.
        /// </summary>
        /// <returns>A string with the disease name or null if clean.</returns>
        public override string CompInspectStringExtra()
        {
            if (IsContaminated())
            {
                // We use the key from the XML and pass the disease label as {0}
                return "DF_Surgery_ContaminatedStatus".Translate(activeDisease.label);
            }
            return null;
        }
    }
}