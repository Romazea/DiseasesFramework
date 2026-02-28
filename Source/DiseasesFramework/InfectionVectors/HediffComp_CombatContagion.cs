using Verse;
using RimWorld;

namespace DiseasesFramework.InfectionVectors
{
    public class HediffCompProperties_CombatContagion : HediffCompProperties
    {
        public float infectionChance = 0.3f;
        public HediffDef hediffToApply;
        public bool onlyMelee = true;

        public bool sendNotification = false;
        public bool useLetterInsteadOfMessage = false;

        public HediffCompProperties_CombatContagion()
        {
            this.compClass = typeof(HediffComp_CombatContagion);
        }
    }

    public class HediffComp_CombatContagion : HediffComp
    {
        public HediffCompProperties_CombatContagion Props => (HediffCompProperties_CombatContagion)this.props;

        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);

            if (this.Pawn == null || !this.Pawn.Spawned)
            {
                return;
            }

            if (Props.hediffToApply == null)
            {
                Log.ErrorOnce("[Disease Framework] An XML is trying to use CombatContagion but is missing the <hediffToApply> tag.", this.Pawn.thingIDNumber ^ 84729);
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

                if (Rand.Chance(Props.infectionChance))
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