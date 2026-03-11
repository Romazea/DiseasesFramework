using Verse;
using RimWorld;
using UnityEngine;

namespace DiseasesFramework.InfectionVectors.DF_Combat
{
    /// <summary>
    /// XML properties for the Defensive Contagion component.
    /// Defines parameters for a disease that infects the attacker when the host takes physical damage.
    /// </summary>
    public class HediffCompProperties_DefensiveContagion : HediffCompProperties
    {
        /// <summary>Base probability (0.0 to 1.0) of infecting the attacker per hit.</summary>
        public float infectionChance = 0.3f;

        /// <summary>The disease (HediffDef) that will be applied to the attacker.</summary>
        public HediffDef hediffToApply;

        /// <summary>If true, only melee attacks trigger the contagion. Ranged attacks are safe.</summary>
        public bool onlyMelee = true;

        /// <summary>If true, the attack must deal actual damage (> 0) to trigger contagion. Deflected blows won't infect.</summary>
        public bool requiresDamageToPenetrate = true;

        /// <summary>If true, the attacker's Toxic Resistance stats will reduce the final infection chance.</summary>
        public bool respectsToxicResistance = false;

        /// <summary>If true, the player receives a notification upon infection.</summary>
        public bool sendNotification = false;

        /// <summary>If true, sends a high-priority Letter instead of a subtle Message.</summary>
        public bool useLetterInsteadOfMessage = false;

        public HediffCompProperties_DefensiveContagion()
        {
            this.compClass = typeof(HediffComp_DefensiveContagion);
        }
    }

    /// <summary>
    /// Active component that monitors incoming damage on the host.
    /// Attempts to infect the instigator of the damage based on configured XML properties.
    /// </summary>
    public class HediffComp_DefensiveContagion : HediffComp
    {
        public HediffCompProperties_DefensiveContagion Props => (HediffCompProperties_DefensiveContagion)this.props;

        /// <summary>
        /// Triggered automatically after the pawn takes damage.
        /// Evaluates the attacker and applies infection logic if conditions are met.
        /// </summary>
        /// <param name="dinfo">Details regarding the damage source and instigator.</param>
        /// <param name="totalDamageDealt">Final damage amount applied after armor/resilience.</param>
        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);

            if (this.Pawn == null || !this.Pawn.Spawned) return;

            // Failsafe: Ensures a Hediff is defined in XML to avoid null reference errors.
            if (Props.hediffToApply == null)
            {
                Log.ErrorOnce("[Disease Framework] DefensiveContagion is missing <hediffToApply> in XML.", this.Pawn.thingIDNumber ^ 84729);
                return;
            }

            // Checks if damage penetration is required for infection.
            if (Props.requiresDamageToPenetrate && totalDamageDealt <= 0f) return;

            Pawn attacker = dinfo.Instigator as Pawn;

            if (attacker != null && !attacker.Dead && attacker != this.Pawn)
            {
                // REVISED LOGIC: Aborts if the attacker is a machine OR NOT a standard biological entity (FleshType != Normal).
                // This ensures standard humans and animals are susceptible while protecting special entities.
                if (attacker.RaceProps.IsMechanoid || attacker.RaceProps.FleshType != FleshTypeDefOf.Normal)
                {
                    return;
                }

                if (Props.onlyMelee)
                {
                    bool isMeleeAttack = dinfo.Weapon == null || !dinfo.Weapon.IsRangedWeapon;
                    if (!isMeleeAttack) return;
                }

                // Prevents redundant infections if the attacker is already carrying the disease.
                if (attacker.health.hediffSet.HasHediff(Props.hediffToApply)) return;

                float finalChance = Props.infectionChance;

                // MITIGATION: Scales chance based on the attacker's toxic resistance stats.
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

                    if (Props.sendNotification && attacker.Faction == Faction.OfPlayer)
                    {
                        string text = "DF_DefensiveInfection_Message".Translate(attacker.LabelShort);
                        string label = "DF_DefensiveInfection_LetterLabel".Translate();

                        if (Props.useLetterInsteadOfMessage)
                        {
                            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NegativeEvent, attacker);
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