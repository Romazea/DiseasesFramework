using Verse;
using RimWorld;

namespace DiseasesFramework.InfectionVectors
{
    public class CompProperties_ContaminatedBodyPart : CompProperties
    {
        public CompProperties_ContaminatedBodyPart()
        {
            this.compClass = typeof(CompContaminatedBodyPart);
        }
    }

    public class CompContaminatedBodyPart : ThingComp
    {
        private HediffDef activeDisease;

        public HediffDef ActiveDisease => activeDisease;

        public void Contaminate(HediffDef disease)
        {
            activeDisease = disease;
        }

        public bool IsContaminated() => activeDisease != null;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look(ref activeDisease, "activeDisease");
        }

        public override string CompInspectStringExtra()
        {
            if (IsContaminated())
            {
                return "Contaminated: " + activeDisease.label;
            }
            return null;
        }
    }
}