using Verse;
using RimWorld;

namespace DiseasesFramework.InfectionVectors.DF_Fomites
{
    public class HediffCompProperties_FomiteContagion : HediffCompProperties
    {
        public int contaminateInterval = 2500;
        public bool sendNotification = true;
        public bool useLetterInsteadOfMessage = false;

        public HediffCompProperties_FomiteContagion()
        {
            this.compClass = typeof(HediffComp_FomiteContagion);
        }
    }

    public class HediffComp_FomiteContagion : HediffComp
    {
        public HediffCompProperties_FomiteContagion Props => (HediffCompProperties_FomiteContagion)this.props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Pawn == null || !Pawn.Spawned || Pawn.Dead) return;

            if (Pawn.IsHashIntervalTick(Props.contaminateInterval))
            {
                ContaminateSurroundings();
            }
        }

        private void ContaminateSurroundings()
        {
            if (Pawn.apparel != null)
            {
                foreach (Apparel apparel in Pawn.apparel.WornApparel)
                {
                    CompFomite fomiteComp = apparel.TryGetComp<CompFomite>();
                    if (fomiteComp != null)
                    {
                        fomiteComp.Contaminate(this.parent.def);
                    }
                }
            }

            if (Pawn.InBed())
            {
                Building_Bed bed = Pawn.CurrentBed();
                if (bed != null)
                {
                    CompFomite fomiteComp = bed.TryGetComp<CompFomite>();
                    if (fomiteComp != null)
                    {
                        fomiteComp.Contaminate(this.parent.def);
                    }
                }
            }
        }
    }
}