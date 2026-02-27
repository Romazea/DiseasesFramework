using Verse;
using RimWorld;
using System.Collections.Generic;

namespace DiseasesFramework.InfectionVectors
{
    public class HediffCompProperties_AuraContagion : HediffCompProperties
    {
        public float radius = 3f;
        public int tickInterval = 2500;
        public float infectionChance = 0.5f;
        public HediffDef hediffToApply;
        public bool requireLineOfSight = true;

        public bool sendNotification = false;
        public bool useLetterInsteadOfMessage = false;

        public HediffCompProperties_AuraContagion()
        {
            this.compClass = typeof(HediffComp_AuraContagion);
        }
    }

    public class HediffComp_AuraContagion : HediffComp
    {
        public HediffCompProperties_AuraContagion Props => (HediffCompProperties_AuraContagion)this.props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (this.Pawn.IsHashIntervalTick(Props.tickInterval))
            {
                TryInfectOthers();
            }
        }

        private void TryInfectOthers()
        {
            if (this.Pawn == null || !this.Pawn.Spawned || this.Pawn.Map == null || this.Pawn.Dead)
            {
                return;
            }

            if (Props.hediffToApply == null)
            {
                Log.ErrorOnce("[Disease Framework] An XML is trying to use AuraContagion but is missing the <hediffToApply> tag.", this.Pawn.thingIDNumber);
                return;
            }

            IEnumerable<Thing> thingsInRadius = GenRadial.RadialDistinctThingsAround(this.Pawn.Position, this.Pawn.Map, Props.radius, true);

            foreach (Thing thing in thingsInRadius)
            {
                Pawn targetPawn = thing as Pawn;

                if (targetPawn != null && !targetPawn.Dead && targetPawn != this.Pawn)
                {
                    if (Props.requireLineOfSight && !GenSight.LineOfSight(this.Pawn.Position, targetPawn.Position, this.Pawn.Map, true))
                    {
                        continue;
                    }

                    if (targetPawn.health.hediffSet.HasHediff(Props.hediffToApply))
                    {
                        continue;
                    }

                    if (Rand.Chance(Props.infectionChance))
                    {
                        targetPawn.health.AddHediff(Props.hediffToApply);

                        if (Props.sendNotification)
                        {
                            if (targetPawn.Faction == Faction.OfPlayer)
                            {
                                string text = targetPawn.LabelShort + " has caught " + Props.hediffToApply.label + " through proximity.";

                                if (Props.useLetterInsteadOfMessage)
                                {
                                    Find.LetterStack.ReceiveLetter("New Infection!", text, LetterDefOf.NegativeEvent, targetPawn);
                                }
                                else
                                {
                                    Messages.Message(text, targetPawn, MessageTypeDefOf.NegativeEvent, true);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}