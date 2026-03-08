using Verse;
using RimWorld;

namespace DiseasesFramework.InfectionVectors.DF_Fomites
{
 
    public class CompProperties_Fomite : CompProperties
    {
        public float daysToDecay = 2f;

        public CompProperties_Fomite()
        {
            this.compClass = typeof(CompFomite);
        }
    }

    public class CompFomite : ThingComp
    {
        public CompProperties_Fomite Props => (CompProperties_Fomite)this.props;

        private HediffDef activeDisease = null;
        private int tickContaminated = -1;

        public HediffDef ActiveDisease => activeDisease;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look(ref activeDisease, "activeDisease");
            Scribe_Values.Look(ref tickContaminated, "tickContaminated", -1);
        }

        public void Contaminate(HediffDef disease)
        {
            activeDisease = disease;
            tickContaminated = Find.TickManager.TicksGame;
        }

        public void Cleanse()
        {
            activeDisease = null;
            tickContaminated = -1;
        }

        public bool IsContaminated()
        {
            if (activeDisease == null || tickContaminated == -1) return false;

            float daysPassed = (Find.TickManager.TicksGame - tickContaminated) / 60000f;

            if (daysPassed > Props.daysToDecay)
            {
                Cleanse();
                return false;
            }

            return true;
        }
    }
}