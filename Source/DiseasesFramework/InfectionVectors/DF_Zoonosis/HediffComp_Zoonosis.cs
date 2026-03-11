using Verse;
using RimWorld;

namespace DiseasesFramework.InfectionVectors.DF_Zoonosis
{
    /// <summary>
    /// XML properties for the Zoonosis component.
    /// Defines the infection probabilities based on the type of interaction with an animal.
    /// </summary>
    public class HediffCompProperties_Zoonosis : HediffCompProperties
    {
        /// <summary>The HediffDef to apply to the human. If null, it defaults to the animal's parent disease.</summary>
        public HediffDef hediffToApply;

        /// <summary>Chance (0.0 to 1.0) to infect during taming, training, milking, or shearing.</summary>
        public float handlingInfectionChance = 0.05f;

        /// <summary>Chance (0.0 to 1.0) to infect during medical tending or rescuing.</summary>
        public float tendingInfectionChance = 0.15f;

        /// <summary>Chance (0.0 to 1.0) to infect the butcher when processing the animal's carcass.</summary>
        public float butcheringInfectionChance = 0.25f;

        /// <summary>Whether to notify the player when a zoonotic infection occurs.</summary>
        public bool sendNotification = true;

        /// <summary>If true, sends a high-priority Letter; otherwise, a standard Message.</summary>
        public bool useLetterInsteadOfMessage = false;

        public HediffCompProperties_Zoonosis()
        {
            this.compClass = typeof(HediffComp_Zoonosis);
        }
    }

    /// <summary>
    /// Active component that manages disease transmission from animals to humans.
    /// This component is triggered by Harmony patches during various interactions.
    /// </summary>
    public class HediffComp_Zoonosis : HediffComp
    {
        public HediffCompProperties_Zoonosis Props => (HediffCompProperties_Zoonosis)this.props;

        /// <summary>
        /// Core logic for checking and applying zoonotic infections.
        /// </summary>
        /// <param name="human">The human pawn interacting with the animal.</param>
        /// <param name="isTending">True if the interaction is medical (tending/carrying).</param>
        /// <param name="isButchering">True if the animal is being butchered.</param>
        public void CheckAndTryInfect(Pawn human, bool isTending = false, bool isButchering = false)
        {
            // Validates that the target is a living, humanlike pawn.
            if (human == null || !human.Spawned || human.Dead || !human.RaceProps.Humanlike)
                return;

            // BUGFIXED LOGIC: Correctly assigns chances based on the interaction type.
            float chance = Props.handlingInfectionChance;
            if (isTending) chance = Props.tendingInfectionChance;
            if (isButchering) chance = Props.butcheringInfectionChance;

            if (Rand.Chance(chance))
            {
                // Fallback: if no specific hediff is defined in XML, use the animal's current disease.
                HediffDef diseaseToGive = Props.hediffToApply ?? this.parent.def;

                // Prevents redundant infection if the human already has the disease.
                if (!human.health.hediffSet.HasHediff(diseaseToGive))
                {
                    human.health.AddHediff(diseaseToGive);

                    if (Props.sendNotification && human.Faction == Faction.OfPlayer)
                    {
                        // {0} = Human Pawn name
                        // {1} = Disease label
                        // {2} = Animal name
                        string text = "DF_ZoonosisInfection_Message".Translate(human.LabelShort, diseaseToGive.label, this.Pawn.LabelShort);
                        string label = "DF_ZoonosisInfection_LetterLabel".Translate();

                        if (Props.useLetterInsteadOfMessage)
                        {
                            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NegativeEvent, human);
                        }
                        else
                        {
                            Messages.Message(text, human, MessageTypeDefOf.NegativeEvent, true);
                        }
                    }
                }
            }
        }
    }
}