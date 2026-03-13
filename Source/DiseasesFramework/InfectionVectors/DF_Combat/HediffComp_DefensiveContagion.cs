using Verse;
using RimWorld;
using UnityEngine;

namespace DiseasesFramework.InfectionVectors.DF_Combat
{
    /// <summary>
    /// XML properties for the Defensive Contagion component.
    /// Defines the parameters for a disease that infects an attacker when the host takes physical damage.
    /// </summary>
    public class HediffCompProperties_DefensiveContagion : HediffCompProperties
    {
        /// <summary>Base probability (0.0 to 1.0) of infecting the attacker per hit.</summary>
        public float infectionChance = 0.3f;

        /// <summary>The disease (HediffDef) that will be applied to the attacker.</summary>
        public HediffDef hediffToApply;

        /// <summary>If true, only melee attacks trigger the contagion. Ranged attacks, explosions, and environmental damage are ignored.</summary>
        public bool onlyMelee = true;

        /// <summary>If true, the attack must deal actual damage (> 0) to trigger contagion. Deflected blows will not infect.</summary>
        public bool requiresDamageToPenetrate = true;

        /// <summary>If true, the attacker's Toxic Resistance stats will mitigate the infection chance.</summary>
        public bool respectsToxicResistance = false;

        /// <summary>If true, the player receives a notification upon successful infection.</summary>
        public bool sendNotification = false;

        /// <summary>If true, sends a high-priority Letter instead of a subtle Message.</summary>
        public bool useLetterInsteadOfMessage = false;

        public HediffCompProperties_DefensiveContagion()
        {
            this.compClass = typeof(HediffComp_DefensiveContagion);
        }
    }

    /// <summary>
    /// Active component that monitors incoming damage on the host pawn.
    /// Attempts to infect the instigator of the damage based on the configured XML properties.
    /// </summary>
    public class HediffComp_DefensiveContagion : HediffComp
    {
        /// <summary>Typed access to the component's XML properties.</summary>
        public HediffCompProperties_DefensiveContagion Props => (HediffCompProperties_DefensiveContagion)this.props;

        /// <summary>
        /// Triggered automatically after the pawn takes damage.
        /// Evaluates the attacker and applies infection logic if all conditions are met.
        /// </summary>
        /// <param name="dinfo">Details regarding the damage source and instigator.</param>
        /// <param name="totalDamageDealt">Final damage amount applied after armor and resilience calculations.</param>
        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);

            // Ensure the host is valid and spawned
            if (this.Pawn == null || !this.Pawn.Spawned) return;

            // Failsafe: Ensures a Hediff is defined in the XML to avoid null reference errors
            if (Props.hediffToApply == null)
            {
                Log.ErrorOnce("[Disease Framework] DefensiveContagion is missing <hediffToApply> in XML.", this.Pawn.thingIDNumber ^ 84729);
                return;
            }

            // Check if damage penetration is required for infection
            if (Props.requiresDamageToPenetrate && totalDamageDealt <= 0f) return;

            // Ensure we have a valid biological attacker (not the host itself, not dead)
            if (!(dinfo.Instigator is Pawn attacker) || attacker.Dead || attacker == this.Pawn) return;

            // Ignore mechanoids and non-standard biological entities (like anomalies or dryads depending on flesh type)
            if (attacker.RaceProps.IsMechanoid || attacker.RaceProps.FleshType != FleshTypeDefOf.Normal) return;

            // 1. IMPROVED MELEE CHECK: Ignore environmental damage, explosions, and ranged weapons
            if (Props.onlyMelee)
            {
                // If it is an explosion, a trap, or a ranged weapon attack, abort.
                if (dinfo.Def.isExplosive || (dinfo.Weapon != null && dinfo.Weapon.IsRangedWeapon)) return;

                // If the damage did not come from a pawn's body part or a melee weapon, abort.
                if (dinfo.WeaponBodyPartGroup == null && dinfo.Weapon == null) return;
            }

            // Prevent redundant infections if the attacker is already carrying the disease
            if (attacker.health.hediffSet.HasHediff(Props.hediffToApply)) return;

            float finalChance = Props.infectionChance;

            // 2. TACTICAL IMMERSION: Bites are significantly more contagious than punches (50% bonus)
            if (dinfo.WeaponBodyPartGroup != null && dinfo.WeaponBodyPartGroup.defName == "Teeth")
            {
                finalChance *= 1.5f;
            }

            // Apply Toxic Resistance mitigation if enabled
            if (Props.respectsToxicResistance)
            {
                float bioRes = attacker.GetStatValue(StatDefOf.ToxicResistance);
                float envRes = attacker.GetStatValue(StatDefOf.ToxicEnvironmentResistance);
                float bestProtection = Mathf.Max(bioRes, envRes);

                finalChance *= Mathf.Clamp01(1f - bestProtection);
            }

            // Roll the biological roulette
            if (Rand.Chance(finalChance))
            {
                // 3. LOCALIZED INFECTION: Try to apply the disease to the specific part that touched the host.
                BodyPartRecord infectedPart = null;

                // If the attacker used their own body (fists, teeth), find that specific part
                if (dinfo.WeaponBodyPartGroup != null)
                {
                    // Highly optimized loop to avoid LINQ/GenCollection type inference errors
                    foreach (BodyPartRecord part in attacker.health.hediffSet.GetNotMissingParts())
                    {
                        if (part.groups.Contains(dinfo.WeaponBodyPartGroup))
                        {
                            infectedPart = part;
                            break;
                        }
                    }
                }

                // Apply the Hediff (Fallback to Whole Body if infectedPart is null, e.g., using a sword)
                attacker.health.AddHediff(Props.hediffToApply, infectedPart);

                // Handle player notifications
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