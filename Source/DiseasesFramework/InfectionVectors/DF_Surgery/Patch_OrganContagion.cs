using HarmonyLib;
using Verse;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using DiseasesFramework.InfectionVectors.DF_Surgery;

namespace DiseasesFramework.HarmonyPatches
{
    /// <summary>
    /// Harmony patches specifically for natural organ operations.
    /// Handles the infection risk when transplanting organic parts from one pawn to another.
    /// </summary>
    public static class Patch_NaturalOrganContagion
    {
        /// <summary>
        /// Patch for 'Recipe_InstallNaturalBodyPart'. 
        /// Ensures that if a player installs a harvested natural organ that is contaminated, 
        /// the recipient contracts the disease.
        /// </summary>
        [HarmonyPatch(typeof(Recipe_InstallNaturalBodyPart), "ApplyOnPawn")]
        public static class Patch_InstallNaturalOrgan_Manual
        {
            /// <summary>
            /// Prefix logic to check organ ingredients before the surgery is finalized.
            /// </summary>
            [HarmonyPrefix]
            public static void Prefix(Pawn pawn, List<Thing> ingredients)
            {
                if (pawn?.health == null || ingredients == null) return;

                foreach (Thing ingredient in ingredients)
                {
                    var comp = ingredient.TryGetComp<CompContaminatedBodyPart>();
                    if (comp != null && comp.IsContaminated())
                    {
                        HediffDef disease = comp.ActiveDisease;
                        var props = disease.CompProps<HediffCompProperties_OrganContagion>();

                        // Apply the disease to the patient.
                        pawn.health.AddHediff(disease);

                        // Feedback logic based on XML configuration.
                        if (props != null && props.sendNotification)
                        {
                            // {0} = Patient name
                            // {1} = Disease label
                            string text = "DF_NaturalOrganInfection_Message".Translate(pawn.LabelShort, disease.label);
                            string title = "DF_NaturalOrganInfection_LetterLabel".Translate();

                            if (props.useLetterInsteadOfMessage)
                            {
                                Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.NegativeEvent, pawn);
                            }
                            else
                            {
                                Messages.Message(text, pawn, MessageTypeDefOf.NegativeEvent);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Global Spawn patch to detect natural organs appearing in the world (harvesting).
        /// If the source pawn has an infectious disease, the organ is marked as contaminated.
        /// </summary>
        [HarmonyPatch(typeof(GenSpawn), "Spawn", new Type[] { typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool), typeof(bool) })]
        public static class Patch_OrganContagion
        {
            /// <summary>
            /// Postfix to catch the newly created organ and look for nearby "donor" pawns.
            /// </summary>
            [HarmonyPostfix]
            public static void Postfix(Thing newThing, IntVec3 loc, Map map)
            {
                var comp = newThing?.TryGetComp<CompContaminatedBodyPart>();
                if (comp == null || comp.IsContaminated() || map == null) return;

                // Scans adjacent cells for the pawn being operated on.
                foreach (IntVec3 adjCell in GenAdj.OccupiedRect(loc, Rot4.North, new IntVec2(1, 1)).ExpandedBy(1))
                {
                    Pawn pawn = adjCell.GetFirstPawn(map);
                    if (pawn?.health == null) continue;

                    foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                    {
                        var organComp = hediff.TryGetComp<HediffComp_OrganContagion>();

                        if (organComp != null && Rand.Chance(organComp.Props.infectionChance))
                        {
                            // Validates if the part belongs to the natural category.
                            bool isNatural = newThing.def.thingCategories?.Any(c => c.defName == "BodyPartsNatural") ?? false;

                            if (isNatural || organComp.Props.canInfectArtificialPart)
                            {
                                comp.Contaminate(hediff.def);

                                // Professional logging for framework developers.
                                if (Prefs.DevMode)
                                    Log.Message($"[Blaxer Studios] Natural organ {newThing.Label} contaminated with {hediff.def.label}.");

                                return;
                            }
                        }
                    }
                }
            }
        }
    }
}