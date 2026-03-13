using HarmonyLib;
using Verse;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;

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

            // Further optimization: only run the radial scan once every 2500 ticks (approx. 1 in-game hour).
            if (__instance.IsHashIntervalTick(2500))
            {
                ScanForInfectedCorpses(__instance);
            }
        }

        /// <summary>
        /// Performs a radial search for corpses within range of the healthy pawn.
        /// Evaluates rot stages, line of sight, and toxic resistance before applying the infection.
        /// </summary>
        /// <param name="healthyPawn">The pawn to be tested for exposure.</param>
        private static void ScanForInfectedCorpses(Pawn healthyPawn)
        {
            // Maximum search distance for any potential corpse. 
            // Individual corpse contagion radii are capped by this value during the initial scan.
            float maxSearchRadius = 10f;
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
                        var comp = hediff.TryGetComp<HediffComp_CorpseContagion>();
                        if (comp != null)
                        {
                            var props = comp.Props;

                            // 1. Verify the putrefaction (rot) stage
                            CompRottable rotComp = corpse.TryGetComp<CompRottable>();
                            if (rotComp != null)
                            {
                                // Skip if the XML properties disable infection for the corpse's current state
                                if (!props.activeWhenFresh && rotComp.Stage == RotStage.Fresh) continue;
                                if (!props.activeWhenRotting && rotComp.Stage == RotStage.Rotting) continue;
                                if (!props.activeWhenDessicated && rotComp.Stage == RotStage.Dessicated) continue;
                            }
                            else
                            {
                                // If the corpse cannot rot (e.g., mechanoids or special races), assume it is fresh
                                if (!props.activeWhenFresh) continue;
                            }

                            // Check if the healthy pawn is within the specific contagion radius of this disease.
                            if (healthyPawn.Position.DistanceTo(corpse.Position) <= props.radius)
                            {
                                // Line of Sight check: walls/doors can block the spread if configured in XML.
                                if (props.requireLineOfSight && !GenSight.LineOfSight(healthyPawn.Position, corpse.Position, healthyPawn.Map, true))
                                {
                                    continue;
                                }

                                // Proceed only if the pawn isn't already sick with this specific disease.
                                if (!healthyPawn.health.hediffSet.HasHediff(hediff.def))
                                {
                                    float finalChance = props.infectionChance;

                                    // 2. Mitigation via toxic resistance / gas masks
                                    if (props.respectsToxicResistance)
                                    {
                                        float bioRes = healthyPawn.GetStatValue(StatDefOf.ToxicResistance);
                                        float envRes = healthyPawn.GetStatValue(StatDefOf.ToxicEnvironmentResistance);
                                        float bestProtection = Mathf.Max(bioRes, envRes);

                                        // Reduce infection chance based on the pawn's best toxic protection
                                        finalChance *= Mathf.Clamp01(1f - bestProtection);
                                    }

                                    // Perform the biological roll
                                    if (Rand.Chance(finalChance))
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
        }

        /// <summary>
        /// Final execution of the infection. Adds the hediff and handles player notifications.
        /// </summary>
        /// <param name="pawn">The pawn receiving the infection.</param>
        /// <param name="disease">The specific disease (HediffDef) to apply.</param>
        /// <param name="props">The component properties dictating notification settings.</param>
        /// <param name="corpseName">The name of the deceased pawn that transmitted the disease.</param>
        private static void InfectPawn(Pawn pawn, HediffDef disease, HediffCompProperties_CorpseContagion props, string corpseName)
        {
            pawn.health.AddHediff(disease);

            if (props.sendNotification && pawn.Faction == Faction.OfPlayer)
            {
                // Pass three arguments for translation: 
                // {0} = Target pawn name
                // {1} = Disease label (translated automatically)
                // {2} = Name of the deceased carrier
                string text = "DF_CorpseInfection_Message".Translate(pawn.LabelShort, disease.label, corpseName);
                string label = "DF_CorpseInfection_LetterLabel".Translate();

                if (props.useLetterInsteadOfMessage)
                {
                    Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NegativeEvent, pawn);
                }
                else
                {
                    Messages.Message(text, pawn, MessageTypeDefOf.NegativeEvent, true);
                }
            }
        }
    }
}