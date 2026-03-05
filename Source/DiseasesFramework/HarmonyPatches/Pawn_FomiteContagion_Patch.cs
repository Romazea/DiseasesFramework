using HarmonyLib;
using Verse;
using RimWorld;

namespace DiseasesFramework.InfectionVectors
{
    [HarmonyPatch(typeof(Pawn), "TickRare")]
    public static class Pawn_FomiteContagion_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn __instance)
        {
            if (__instance == null || !__instance.Spawned || __instance.Dead || !__instance.RaceProps.Humanlike)
                return;

            if (__instance.IsHashIntervalTick(2500))
            {
                CheckFomiteExposure(__instance);
            }
        }

        private static void CheckFomiteExposure(Pawn pawn)
        {
            if (pawn.apparel != null)
            {
                foreach (Apparel apparel in pawn.apparel.WornApparel)
                {
                    CompFomite comp = apparel.TryGetComp<CompFomite>();

                    if (comp != null && comp.IsContaminated() && !pawn.health.hediffSet.HasHediff(comp.ActiveDisease))
                    {
                        TryInfect(pawn, comp.ActiveDisease, apparel.LabelShort);
                    }
                }
            }

            if (pawn.InBed())
            {
                Building_Bed bed = pawn.CurrentBed();
                if (bed != null)
                {
                    CompFomite comp = bed.TryGetComp<CompFomite>();

                    if (comp != null && comp.IsContaminated() && !pawn.health.hediffSet.HasHediff(comp.ActiveDisease))
                    {
                        TryInfect(pawn, comp.ActiveDisease, bed.LabelShort);
                    }
                }
            }
        }

        private static void TryInfect(Pawn pawn, HediffDef disease, string sourceName)
        {
            float infectionChance = 0.05f;

            if (Rand.Chance(infectionChance))
            {
                pawn.health.AddHediff(disease);

                if (pawn.Faction == Faction.OfPlayer)
                {
                    var props = disease.CompProps<HediffCompProperties_FomiteContagion>();

                    bool sendNotif = props != null ? props.sendNotification : true;
                    bool useLetter = props != null ? props.useLetterInsteadOfMessage : false;

                    if (sendNotif)
                    {
                        string title = "Contact infection";
                        string text = $"{pawn.LabelShort} has contracted {disease.label} through contact with a contaminated object ({sourceName}).";
                        
                        if (useLetter)
                        {
                            Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.NegativeEvent, pawn);
                        }
                        else 
                        {
                            Messages.Message(text, pawn, MessageTypeDefOf.NegativeEvent, true);
                        }
                    }
                }
            }
        }
    }
}