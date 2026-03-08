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
    /// Harmony patches to handle disease transmission during surgical procedures and organ harvesting.
    /// Manages both the contamination of newly spawned parts and the infection of patients during transplants.
    /// </summary>
    public static class Patch_OrganSurgeryContagion
    {
        // --- INSTALLATION PATCHES ---

        /// <summary>
        /// Patch for installing artificial body parts. Checks if the part is contaminated before installation.
        /// </summary>
        [HarmonyPatch(typeof(Recipe_InstallArtificialBodyPart), "ApplyOnPawn")]
        public static class Patch_InstallArtificial_Manual
        {
            [HarmonyPrefix]
            public static void Prefix(Pawn pawn, List<Thing> ingredients)
            {
                ApplySurgicalInfection(pawn, ingredients);
            }
        }

        /// <summary>
        /// Patch for installing implants. Ensures the patient is infected if the implant is contaminated.
        /// </summary>
        [HarmonyPatch(typeof(Recipe_InstallImplant), "ApplyOnPawn")]
        public static class Patch_InstallImplant_Manual
        {
            [HarmonyPrefix]
            public static void Prefix(Pawn pawn, List<Thing> ingredients)
            {
                ApplySurgicalInfection(pawn, ingredients);
            }
        }

        /// <summary>
        /// Internal logic to transfer a pathogen from an ingredient (organ/part) to the patient.
        /// </summary>
        private static void ApplySurgicalInfection(Pawn pawn, List<Thing> ingredients)
        {
            if (pawn?.health == null || ingredients == null) return;

            foreach (Thing ingredient in ingredients)
            {
                var comp = ingredient.TryGetComp<CompContaminatedBodyPart>();
                if (comp != null && comp.IsContaminated())
                {
                    HediffDef disease = comp.ActiveDisease;
                    pawn.health.AddHediff(disease);

                    // Handle player feedback based on the disease's specific surgical properties.
                    var props = disease.CompProps<HediffCompProperties_OrganContagion>();
                    if (props != null && props.sendNotification)
                    {
                        string text = $"{pawn.LabelShort} has been infected with {disease.label} during surgery.";
                        string title = "Surgical Infection";

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

        // --- HARVESTING / SPAWNING PATCH ---

        /// <summary>
        /// Global patch for GenSpawn. Captures the moment an organ is harvested (spawned) 
        /// to check if it should be contaminated by the original host.
        /// </summary>
        [HarmonyPatch(typeof(GenSpawn), "Spawn", new Type[] { typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool), typeof(bool) })]
        public static class Patch_GlobalContagion_Spawn
        {
            /// <summary>
            /// Postfix that checks the area around a newly spawned item for a "source" pawn (the host).
            /// </summary>
            [HarmonyPostfix]
            public static void Postfix(Thing newThing, IntVec3 loc, Map map)
            {
                var comp = newThing?.TryGetComp<CompContaminatedBodyPart>();
                if (comp == null || comp.IsContaminated() || map == null) return;

                // Scan adjacent cells to find the pawn from which the part was likely harvested.
                foreach (IntVec3 adjCell in GenAdj.OccupiedRect(loc, Rot4.North, new IntVec2(1, 1)).ExpandedBy(1))
                {
                    Pawn pawn = adjCell.GetFirstPawn(map);
                    if (pawn?.health == null) continue;

                    foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                    {
                        var organComp = hediff.TryGetComp<HediffComp_OrganContagion>();
                        if (organComp != null && Rand.Chance(organComp.Props.infectionChance))
                        {
                            // Logic to determine if the part is natural or artificial, and if it can be infected.
                            bool isNatural = newThing.def.thingCategories?.Any(c => c.defName == "BodyPartsNatural") ?? false;

                            if (isNatural || organComp.Props.canInfectArtificialPart)
                            {
                                comp.Contaminate(hediff.def);
                                return; // Infection applied, stop searching.
                            }
                        }
                    }
                }
            }
        }
    }
}