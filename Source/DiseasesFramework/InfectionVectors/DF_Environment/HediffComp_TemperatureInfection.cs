using Verse;
using RimWorld;

namespace DiseasesFramework.InfectionVectors.DF_Environment
{
    /// <summary>
    /// XML properties for the Temperature Infection component.
    /// Defines how a condition (like Hypothermia) can trigger a secondary disease after reaching a certain severity.
    /// </summary>
    public class HediffCompProperties_TemperatureInfection : HediffCompProperties
    {
        /// <summary>The minimum severity the parent Hediff must reach before infection checks begin.</summary>
        public float minSeverity = 0.1f;

        /// <summary>The secondary disease (HediffDef) to be applied to the pawn.</summary>
        public HediffDef diseaseToApply;

        /// <summary>The probability (0.0 to 1.0) of contracting the disease during each check.</summary>
        public float chancePerCheck = 0.05f;

        /// <summary>Frequency (in ticks) of the infection checks. Default 2500 is approx. 1 game hour.</summary>
        public int checkInterval = 2500;

        /// <summary>Whether to notify the player if the secondary disease is contracted.</summary>
        public bool sendNotification = true;

        /// <summary>If true, sends a high-priority Letter; otherwise, a standard Message.</summary>
        public bool useLetterInsteadOfMessage = false;

        public HediffCompProperties_TemperatureInfection()
        {
            this.compClass = typeof(HediffComp_TemperatureInfection);
        }
    }

    /// <summary>
    /// Active component that monitors the severity of a condition and triggers a secondary infection.
    /// Useful for simulating opportunistic diseases that arise when the immune system is weakened by extreme temperatures.
    /// </summary>
    public class HediffComp_TemperatureInfection : HediffComp
    {
        public HediffCompProperties_TemperatureInfection Props => (HediffCompProperties_TemperatureInfection)this.props;

        /// <summary>
        /// Hook called every tick to manage the check interval logic.
        /// </summary>
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Pawn == null || Pawn.Dead) return;

            // Executes the check only at the specified interval to preserve performance.
            if (Pawn.IsHashIntervalTick(Props.checkInterval))
            {
                CheckForInfection();
            }
        }

        /// <summary>
        /// Evaluates current severity and rolls for the secondary infection.
        /// </summary>
        private void CheckForInfection()
        {
            if (parent == null) return;

            // Condition 1: Must exceed the minimum severity threshold (e.g., Hypothermia must be 'Serious').
            if (parent.Severity < Props.minSeverity) return;

            // Condition 2: Ensure the pawn doesn't already have the secondary disease.
            if (Pawn.health.hediffSet.HasHediff(Props.diseaseToApply)) return;

            // Final probability roll.
            if (Rand.Chance(Props.chancePerCheck))
            {
                Pawn.health.AddHediff(Props.diseaseToApply);

                if (Props.sendNotification && Pawn.Faction == Faction.OfPlayer)
                {
                    string text = $"{Pawn.LabelShort} has contracted {Props.diseaseToApply.label} due to severe exposure.";

                    if (Props.useLetterInsteadOfMessage)
                    {
                        Find.LetterStack.ReceiveLetter("Disease Risk", text, LetterDefOf.NegativeEvent, Pawn);
                    }
                    else
                    {
                        Messages.Message(text, Pawn, MessageTypeDefOf.NegativeEvent, true);
                    }
                }
            }
        }
    }
}