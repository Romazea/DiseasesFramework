using Verse;
using RimWorld;
using UnityEngine;

namespace DiseasesFramework.InfectionVectors.DF_Ingestion
{
    /// <summary>
    /// XML properties for food contamination.
    /// Defines how the player is notified and how the infection probability scales.
    /// </summary>
    public class CompProperties_ContaminatedFood : CompProperties
    {
        /// <summary>Base chance multiplier. 1.0 means 100% chance per full serving. Scales with amount eaten.</summary>
        public float baseInfectionWeight = 1.0f;

        /// <summary>Whether to notify the player when a pawn contracts a disease from this food.</summary>
        public bool sendNotification = true;

        /// <summary>If true, sends a Letter; otherwise, a standard Message.</summary>
        public bool useLetterInsteadOfMessage = false;

        public CompProperties_ContaminatedFood()
        {
            this.compClass = typeof(CompContaminatedFood);
        }
    }

    /// <summary>
    /// Component that turns a food item into a disease vector.
    /// Pathogens are stored within the food and transmitted upon ingestion.
    /// </summary>
    public class CompContaminatedFood : ThingComp
    {
        /// <summary>The specific disease currently hiding in this food item.</summary>
        public HediffDef linkedDisease;

        public CompProperties_ContaminatedFood Props => (CompProperties_ContaminatedFood)this.props;

        /// <summary>
        /// Saves and loads the linked disease data.
        /// Essential for keeping food dangerous after a game save/load.
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look(ref linkedDisease, "linkedDisease");
        }

        /// <summary>
        /// Logic to handle item stacking/splitting.
        /// Ensures that when a stack of food is split, the new stack inherits the same contamination.
        /// </summary>
        public override void PostSplitOff(Thing piece)
        {
            base.PostSplitOff(piece);
            CompContaminatedFood otherComp = piece.TryGetComp<CompContaminatedFood>();

            if (otherComp != null)
            {
                otherComp.linkedDisease = this.linkedDisease;
            }
        }

        /// <summary>
        /// Triggered when a pawn finishes eating the item.
        /// Now applies infection probability based on the 'baseInfectionWeight' property.
        /// </summary>
        /// <param name="ingester">The pawn that consumed the food.</param>
        public override void PostIngested(Pawn ingester)
        {
            base.PostIngested(ingester);

            if (linkedDisease == null || ingester == null) return;

            // Only biological (flesh) entities can be infected by food.
            if (!ingester.RaceProps.IsFlesh) return;

            // Avoid reapplying if the pawn already has the disease.
            if (ingester.health.hediffSet.HasHediff(linkedDisease)) return;

            // CALCULATION: Apply infection chance roll. 
            // We use Mathf.Clamp01 to ensure the chance is always valid between 0 and 1.
            if (Rand.Chance(Mathf.Clamp01(Props.baseInfectionWeight)))
            {
                ingester.health.AddHediff(linkedDisease);

                if (Props.sendNotification && ingester.Faction == Faction.OfPlayer)
                {
                    // {0} = Ingester pawn name, {1} = Linked disease label
                    string text = "DF_FoodInfection_Message".Translate(ingester.LabelShort, linkedDisease.label);
                    string label = "DF_FoodInfection_LetterLabel".Translate();

                    if (Props.useLetterInsteadOfMessage)
                    {
                        Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NegativeEvent, ingester);
                    }
                    else
                    {
                        Messages.Message(text, ingester, MessageTypeDefOf.NegativeEvent, true);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a custom debug button in Dev Mode.
        /// Allows developers to manually infect food for testing purposes.
        /// </summary>
        public override System.Collections.Generic.IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Infect Food (FoodPoisoning)",
                    defaultDesc = "Debug tool for Blaxer Studios: Manually link Food Poisoning to this item.",
                    icon = TexCommand.Attack,
                    action = () =>
                    {
                        this.linkedDisease = HediffDefOf.FoodPoisoning;
                        Messages.Message("Dev: Food manually infected with Food Poisoning", MessageTypeDefOf.NeutralEvent);
                    }
                };
            }
        }
    }
}