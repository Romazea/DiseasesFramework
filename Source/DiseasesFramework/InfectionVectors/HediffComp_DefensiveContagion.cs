using Verse;
using RimWorld;
using UnityEngine;

namespace DiseasesFramework.InfectionVectors
{
    public class HediffCompProperties_DefensiveContagion : HediffCompProperties
    {
        public float infectionChance = 0.3f;
        public HediffDef hediffToApply;
        public bool onlyMelee = true;

        public bool requiresDamageToPenetrate = true;
        public bool respectsToxicResistance = false;

        public bool sendNotification = false;
        public bool useLetterInsteadOfMessage = false;

        public HediffCompProperties_DefensiveContagion()
        {
            this.compClass = typeof(HediffComp_DefensiveContagion);
        }
    }

    public class HediffComp_DefensiveContagion : HediffComp
    {
        public HediffCompProperties_DefensiveContagion Props => (HediffCompProperties_DefensiveContagion)this.props;

        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);

            if (this.Pawn == null || !this.Pawn.Spawned)
            {
                return;
            }

            if (Props.hediffToApply == null)
            {
                Log.ErrorOnce("[Disease Framework] An XML is trying to use DefensiveContagion but is missing the <hediffToApply> tag.", this.Pawn.thingIDNumber ^ 84729);
                return;
            }

            if (Props.requiresDamageToPenetrate && totalDamageDealt <= 0f)
            {
                return;
            }

            Pawn attacker = dinfo.Instigator as Pawn;

            if (attacker != null && !attacker.Dead && attacker != this.Pawn)
            {
                if (Props.onlyMelee)
                {
                    bool isMeleeAttack = dinfo.Weapon == null || !dinfo.Weapon.IsRangedWeapon;

                    if (!isMeleeAttack)
                    {
                        return;
                    }
                }

                if (attacker.health.hediffSet.HasHediff(Props.hediffToApply))
                {
                    return;
                }

                float finalChance = Props.infectionChance;

                if (Props.respectsToxicResistance)
                {
                    float bioRes = attacker.GetStatValue(StatDefOf.ToxicResistance);

                    float envRes = attacker.GetStatValue(StatDefOf.ToxicEnvironmentResistance);

                    float bestProtection = Mathf.Max(bioRes, envRes);

                    finalChance *= Mathf.Clamp01(1f - bestProtection);
                }

                if (Rand.Chance(finalChance))
                {
                    attacker.health.AddHediff(Props.hediffToApply);

                    if (Props.sendNotification)
                    {
                        if (attacker.Faction == Faction.OfPlayer)
                        {
                            string text = attacker.LabelShort + " was infected by physical contact with contaminated blood/tissue.";

                            if (Props.useLetterInsteadOfMessage)
                            {
                                Find.LetterStack.ReceiveLetter("Combat Infection!", text, LetterDefOf.NegativeEvent, attacker);
                            }
                            else
                            {
                                Messages.Message(text, attacker, MessageTypeDefOf.NegativeEvent, true);
                            }
                        }
                    }
                }
            }
        }
    }
}