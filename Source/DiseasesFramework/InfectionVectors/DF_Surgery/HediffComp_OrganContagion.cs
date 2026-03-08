using Verse;
using RimWorld;

namespace DiseasesFramework.InfectionVectors.DF_Surgery
{
    public class HediffCompProperties_OrganContagion : HediffCompProperties
    {
        public float infectionChance = 1.0f;
        public bool canInfectArtificialPart = false;
        public bool sendNotification = true;
        public bool useLetterInsteadOfMessage = false;

        public HediffCompProperties_OrganContagion()
        {
            this.compClass = typeof(HediffComp_OrganContagion);
        }
    }

    public class HediffComp_OrganContagion : HediffComp
    {
        public HediffCompProperties_OrganContagion Props => (HediffCompProperties_OrganContagion)this.props;
    }
}