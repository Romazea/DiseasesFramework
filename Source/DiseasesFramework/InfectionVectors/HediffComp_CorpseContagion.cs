using Verse;
using RimWorld;

namespace DiseasesFramework.InfectionVectors
{
    public class HediffCompProperties_CorpseContagion : HediffCompProperties
    {
        public float radius = 4f;
        public float infectionChance = 0.5f;
        public bool requireLineOfSight = true;
        public bool sendNotification = true;
        public bool useLetterInsteadOfMessage = false;

        public HediffCompProperties_CorpseContagion()
        {
            this.compClass = typeof(HediffComp_CorpseContagion);
        }
    }

    public class HediffComp_CorpseContagion : HediffComp
    {
        public HediffCompProperties_CorpseContagion Props => (HediffCompProperties_CorpseContagion)this.props;
    }
}