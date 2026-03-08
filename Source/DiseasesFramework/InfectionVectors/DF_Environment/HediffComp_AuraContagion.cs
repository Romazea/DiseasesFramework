using Verse;
using RimWorld;
using System.Collections.Generic;

namespace DiseasesFramework.InfectionVectors.DF_Environment
{
    /// <summary>
    /// XML properties for the Aura Contagion component.
    /// Defines how a disease spreads from a living carrier to other nearby pawns through proximity.
    /// </summary>
    public class HediffCompProperties_AuraContagion : HediffCompProperties
    {
        /// <summary>The range (in tiles) of the infectious aura.</summary>
        public float radius = 3f;

        /// <summary>How often (in ticks) the aura pulses to check for new victims. Default 2500 is roughly 1 game hour.</summary>
        public int tickInterval = 2500;

        /// <summary>Base probability (0.0 to 1.0) of infecting someone within the aura per pulse.</summary>
        public float infectionChance = 0.5f;

        /// <summary>The HediffDef to be spread to others.</summary>
        public HediffDef hediffToApply;

        /// <summary>If true, walls and doors block the infectious aura.</summary>
        public bool requireLineOfSight = true;

        /// <summary>Whether to notify the player when a new infection occurs via aura.</summary>
        public bool sendNotification = false;

        /// <summary>If true, uses a high-priority Letter instead of a Message.</summary>
        public bool useLetterInsteadOfMessage = false;

        public HediffCompProperties_AuraContagion()
        {
            this.compClass = typeof(HediffComp_AuraContagion);
        }
    }

    /// <summary>
    /// Active component for infectious auras.
    /// Periodically scans the area around the carrier and attempts to infect nearby biological pawns.
    /// </summary>
    public class HediffComp_AuraContagion : HediffComp
    {
        public HediffCompProperties_AuraContagion Props => (HediffCompProperties_AuraContagion)this.props;

        /// <summary>
        /// Called every tick. Uses IsHashIntervalTick to execute logic based on the configured tickInterval.
        /// </summary>
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (this.Pawn.IsHashIntervalTick(Props.tickInterval))
            {
                TryInfectOthers();
            }
        }

        /// <summary>
        /// Scans the radius for potential victims and applies infection logic based on distance and resistance.
        /// </summary>
        private void TryInfectOthers()
        {
            if (this.Pawn == null || !this.Pawn.Spawned || this.Pawn.Map == null || this.Pawn.Dead)
            {
                return;
            }

            // Failsafe check for XML configuration.
            if (Props.hediffToApply == null)
            {
                Log.ErrorOnce("[Disease Framework] AuraContagion is missing <hediffToApply> in XML.", this.Pawn.thingIDNumber);
                return;
            }

            IEnumerable<Thing> thingsInRadius = GenRadial.RadialDistinctThingsAround(this.Pawn.Position, this.Pawn.Map, Props.radius, true);

            foreach (Thing thing in thingsInRadius)
            {
                Pawn targetPawn = thing as Pawn;

                // Ensure the target is a living pawn and not the carrier themselves.
                if (targetPawn != null && !targetPawn.Dead && targetPawn != this.Pawn)
                {
                    // BUGFIX: Changed '==' to '!=' to ensure standard Humans (FleshType.Normal) are susceptible.
                    if (targetPawn.RaceProps.IsMechanoid || targetPawn.RaceProps.FleshType != FleshTypeDefOf.Normal)
                    {
                        continue;
                    }

                    // Check for Line of Sight if required.
                    if (Props.requireLineOfSight && !GenSight.LineOfSight(this.Pawn.Position, targetPawn.Position, this.Pawn.Map, true))
                    {
                        continue;
                    }

                    // Avoid duplicate infections.
                    if (targetPawn.health.hediffSet.HasHediff(Props.hediffToApply))
                    {
                        continue;
                    }

                    // CALCULATING MITIGATION:
                    // This uses ToxicEnvironmentResistance (e.g., from Gas Masks or closed Apparel).
                    float protection = targetPawn.GetStatValue(StatDefOf.ToxicEnvironmentResistance);
                    float adjustedChance;

                    if (protection >= 0.8f) // High protection (Full Hazmat)
                    {
                        adjustedChance = 0f;
                    }
                    else if (protection >= 0.5f) // Partial protection (Masks)
                    {
                        adjustedChance = Props.infectionChance * 0.1f;
                    }
                    else // Low/No protection
                    {
                        adjustedChance = Props.infectionChance * (1f - protection);
                    }

                    // Final infection roll.
                    if (adjustedChance > 0f && Rand.Chance(adjustedChance))
                    {
                        targetPawn.health.AddHediff(Props.hediffToApply);

                        if (Props.sendNotification && targetPawn.Faction == Faction.OfPlayer)
                        {
                            string text = $"{targetPawn.LabelShort} has caught {Props.hediffToApply.label} through proximity.";

                            if (Props.useLetterInsteadOfMessage)
                                Find.LetterStack.ReceiveLetter("New Infection!", text, LetterDefOf.NegativeEvent, targetPawn);
                            else
                                Messages.Message(text, targetPawn, MessageTypeDefOf.NegativeEvent, true);
                        }
                    }
                }
            }
        }
    }
}