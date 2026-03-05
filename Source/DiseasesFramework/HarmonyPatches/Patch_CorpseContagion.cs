using HarmonyLib;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace DiseasesFramework.HarmonyPatches
{
    [HarmonyPatch(typeof(Pawn), "TickRare")]
    public static class Patch_CorpseContagion
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn __instance)
        {
            if (__instance == null || !__instance.Spawned || __instance.Dead || !__instance.RaceProps.Humanlike)
                return;

            if (__instance.IsHashIntervalTick(2500))
            {
                ScanForInfectedCorpses(__instance);
            }
        }

        private static void ScanForInfectedCorpses(Pawn healthyPawn)
        {
            float maxSearchRadius = 10f;

            IEnumerable<Thing> thingsInRadius = GenRadial.RadialDistinctThingsAround(healthyPawn.Position, healthyPawn.Map, maxSearchRadius, true);

            foreach (Thing thing in thingsInRadius)
            {
                Corpse corpse = thing as Corpse;
                if (corpse != null && corpse.InnerPawn != null && corpse.InnerPawn.health != null)
                {
                    foreach (Hediff hediff in corpse.InnerPawn.health.hediffSet.hediffs)
                    {
                        var comp = hediff.TryGetComp<InfectionVectors.HediffComp_CorpseContagion>();
                        if (comp != null)
                        {
                            var props = comp.Props;

                            if (healthyPawn.Position.DistanceTo(corpse.Position) <= props.radius)
                            {
                                if (props.requireLineOfSight && !GenSight.LineOfSight(healthyPawn.Position, corpse.Position, healthyPawn.Map, true))
                                {
                                    continue;
                                }

                                if (!healthyPawn.health.hediffSet.HasHediff(hediff.def) && Rand.Chance(props.infectionChance))
                                {
                                    InfectPawn(healthyPawn, hediff.def, props, corpse.InnerPawn.LabelShort);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void InfectPawn(Pawn pawn, HediffDef disease, InfectionVectors.HediffCompProperties_CorpseContagion props, string corpseName)
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