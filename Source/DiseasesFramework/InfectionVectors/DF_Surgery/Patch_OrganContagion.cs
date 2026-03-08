using HarmonyLib;
using Verse;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using DiseasesFramework.InfectionVectors.DF_Surgery;

namespace DiseasesFramework.HarmonyPatches
{
    public static class Patch_NaturalOrganContagion
    {
        [HarmonyPatch(typeof(Recipe_InstallNaturalBodyPart), "ApplyOnPawn")]
        public static class Patch_InstallNaturalOrgan_Manual
        {
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

                        pawn.health.AddHediff(disease);

                        if (props != null && props.sendNotification)
                        {
                            string text = $"{pawn.LabelShort} has been infected with {disease.label} during organ transplantation.";
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
        }

        [HarmonyPatch(typeof(GenSpawn), "Spawn", new Type[] { typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool), typeof(bool) })]
        public static class Patch_OrganContagion
        {
            [HarmonyPostfix]
            public static void Postfix(Thing newThing, IntVec3 loc, Map map)
            {
                var comp = newThing?.TryGetComp<CompContaminatedBodyPart>();
                if (comp == null || comp.IsContaminated() || map == null) return;

                foreach (IntVec3 adjCell in GenAdj.OccupiedRect(loc, Rot4.North, new IntVec2(1, 1)).ExpandedBy(1))
                {
                    Pawn pawn = adjCell.GetFirstPawn(map);
                    if (pawn?.health == null) continue;

                    foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                    {
                        var organComp = hediff.TryGetComp<HediffComp_OrganContagion>();

                        if (organComp != null && Rand.Chance(organComp.Props.infectionChance))
                        {
                            bool isNatural = newThing.def.thingCategories?.Any(c => c.defName == "BodyPartsNatural") ?? false;

                            if (isNatural || organComp.Props.canInfectArtificialPart)
                            {
                                comp.Contaminate(hediff.def);

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