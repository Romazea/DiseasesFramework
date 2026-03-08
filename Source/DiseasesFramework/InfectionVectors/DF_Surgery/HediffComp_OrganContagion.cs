using Verse;
using RimWorld;

namespace DiseasesFramework.InfectionVectors.DF_Surgery
{
    /// <summary>
    /// XML properties for the Organ Contagion component.
    /// Defines how a disease behaves when the host's body parts are harvested or transplanted.
    /// </summary>
    public class HediffCompProperties_OrganContagion : HediffCompProperties
    {
        /// <summary>Probability (0.0 to 1.0) that the harvested part will become contaminated.</summary>
        public float infectionChance = 1.0f;

        /// <summary>If false, artificial parts (bionics, prosthetics) harvested from the host will remain clean.</summary>
        public bool canInfectArtificialPart = false;

        /// <summary>Whether to notify the player when a patient is infected via a contaminated transplant.</summary>
        public bool sendNotification = true;

        /// <summary>If true, uses a Letter; otherwise, a standard Message for notifications.</summary>
        public bool useLetterInsteadOfMessage = false;

        public HediffCompProperties_OrganContagion()
        {
            this.compClass = typeof(HediffComp_OrganContagion);
        }
    }

    /// <summary>
    /// Active component that marks a disease as capable of contaminating body parts.
    /// This component is scanned by Harmony patches during surgical operations.
    /// </summary>
    public class HediffComp_OrganContagion : HediffComp
    {
        /// <summary>
        /// Provides access to the surgery-specific settings defined in XML.
        /// </summary>
        public HediffCompProperties_OrganContagion Props => (HediffCompProperties_OrganContagion)this.props;
    }
}