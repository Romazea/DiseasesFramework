using Verse;
using RimWorld;
using HarmonyLib;
using System.Collections.Generic;

namespace DiseasesFramework.InfectionVectors
{
    public class HediffCompProperties_OffensiveContagion : HediffCompProperties
    {
        public float infectionChance = 0.5f;
        public HediffDef hediffToApply;
        public bool onlyMelee = true;

        public bool sendNotification = false;
        public bool useLetterInsteadOfMessage = false;

        public HediffCompProperties_OffensiveContagion()
        {
            this.compClass = typeof(HediffComp_OffensiveContagion);
        }
    }

    public class HediffComp_OffensiveContagion : HediffComp
    {
        public HediffCompProperties_OffensiveContagion Props => (HediffCompProperties_OffensiveContagion)this.props;
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PostApplyDamage))]
    public static class Patch_Pawn_PostApplyDamage_OffensiveContagion
    {
        public static void Postfix(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {

            if (__instance == null || __instance.Dead || dinfo.Instigator == null) return;

            Pawn attacker = dinfo.Instigator as Pawn;
            if (attacker == null || attacker == __instance) return;

            List<Hediff> attackerHediffs = attacker.health.hediffSet.hediffs;
            for (int i = 0; i < attackerHediffs.Count; i++)
            {
                HediffComp_OffensiveContagion comp = attackerHediffs[i].TryGetComp<HediffComp_OffensiveContagion>();

                if (comp != null)
                {
                    var props = comp.Props;
                    if (props.hediffToApply == null) continue;

                    if (props.onlyMelee)
                    {
                        bool isMeleeAttack = dinfo.Weapon == null || !dinfo.Weapon.IsRangedWeapon;
                        if (!isMeleeAttack) continue;
                    }

                    if (__instance.health.hediffSet.HasHediff(props.hediffToApply)) continue;

                    if (Rand.Chance(props.infectionChance))
                    {
                        __instance.health.AddHediff(props.hediffToApply);

                        if (props.sendNotification && __instance.Faction == Faction.OfPlayer)
                        {
                            string text = __instance.LabelShort + " was infected by an attack from " + attacker.LabelShort + ".";

                            if (props.useLetterInsteadOfMessage)
                            {
                                Find.LetterStack.ReceiveLetter("Offensive Infection!", text, LetterDefOf.NegativeEvent, __instance);
                            }
                            else
                            {
                                Messages.Message(text, __instance, MessageTypeDefOf.NegativeEvent, true);
                            }
                        }
                    }
                }
            }
        }
    }
}