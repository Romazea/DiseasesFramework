using HarmonyLib;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace DiseasesFramework.InfectionVectors.DF_Corpses
{
    /// <summary>
    /// Harmony patch for the Corpse Contagion system.
    /// Periodically scans the area around living pawns for infected corpses that emit miasma/contagion.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "TickRare")]
    public static class Patch_CorpseContagion
    {
        /// <summary>
        /// Postfix executed during the Pawn's Rare Tick (every 250 game ticks).
        /// This provides a balance between real-time responsiveness and game performance.
        /// </summary>
        /// <param name="__instance">The living pawn potentially being exposed to contagion.</param>
        [HarmonyPostfix]
        public static void Postfix(Pawn __instance)
        {
            // Basic validation: only living, spawned, humanlike pawns are checked for infection.
            if (__instance == null || !__instance.Spawned || __instance.Dead || !__instance.RaceProps.Humanlike)
                return;

            // Further optimization: only run the scan once every 2500 ticks (approx. 1 in-game hour).
            if (__instance.IsHashIntervalTick(2500))
            {
                ScanForInfectedCorpses(__instance);
            }
        }

        /// <summary>
        /// Performs a radial search for corpses within range of the healthy pawn.
        /// If a corpse has a disease with the CorpseContagion component, it calculates the infection risk.
        /// </summary>
        /// <param name="healthyPawn">The pawn to be tested for exposure.</param>
        private static void ScanForInfectedCorpses(Pawn healthyPawn)
        {
            // Maximum search distance for any potential corpse. 
            // Individual corpse contagion radii are capped by this value during the initial scan.
            float maxSearchRadius = 10f;

            // Get all things (including corpses) in a 10-tile radius.
            IEnumerable<Thing> thingsInRadius = GenRadial.RadialDistinctThingsAround(healthyPawn.Position, healthyPawn.Map, maxSearchRadius, true);

            foreach (Thing thing in thingsInRadius)
            {
                Corpse corpse = thing as Corpse;

                // Verify if the thing is a corpse and has internal health data (InnerPawn).
                if (corpse != null && corpse.InnerPawn != null && corpse.InnerPawn.health != null)
                {
                    // Check every disease the corpse had at the time of death.
                    foreach (Hediff hediff in corpse.InnerPawn.health.hediffSet.hediffs)
                    {
                        var comp = hediff.TryGetComp<InfectionVectors.DF_Corpses.HediffComp_CorpseContagion>();
                        if (comp != null)
                        {
                            var props = comp.Props;

                            // Check if the healthy pawn is within the specific contagion radius of this disease.
                            if (healthyPawn.Position.DistanceTo(corpse.Position) <= props.radius)
                            {
                                // Line of Sight check: walls/doors can block the spread if configured in XML.
                                if (props.requireLineOfSight && !GenSight.LineOfSight(healthyPawn.Position, corpse.Position, healthyPawn.Map, true))
                                {
                                    continue;
                                }

                                // Apply infection if the pawn isn't already sick and the RNG roll succeeds.
                                if (!healthyPawn.health.hediffSet.HasHediff(hediff.def) && Rand.Chance(props.infectionChance))
                                {
                                    InfectPawn(healthyPawn, hediff.def, props, corpse.InnerPawn.LabelShort);
                                    return; // Stop scanning once an infection is applied to prevent multi-infection in one tick.
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Final execution of the infection. Adds the hediff and handles player notifications.
        /// </summary>
        private static void InfectPawn(Pawn pawn, HediffDef disease, InfectionVectors.DF_Corpses.HediffCompProperties_CorpseContagion props, string corpseName)
        {
            pawn.health.AddHediff(disease);

            if (props.sendNotification && pawn.Faction == Faction.OfPlayer)
            {
                string text = $"{pawn.LabelShort} has contracted {disease.label} from exposure to an infected corpse ({corpseName}).";

                if (props.useLetterInsteadOfMessage)
                {
                    Find.LetterStack.ReceiveLetter("Corpse Infection", text, LetterDefOf.NegativeEvent, pawn);
                }
                else
                {
                    Messages.Message(text, pawn, MessageTypeDefOf.NegativeEvent, true);
                }
            }
        }
    }
}