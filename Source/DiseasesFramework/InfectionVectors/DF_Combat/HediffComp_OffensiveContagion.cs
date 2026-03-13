using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace DiseasesFramework.InfectionVectors.DF_Combat
{
    /// <summary>
    /// XML properties for the Offensive Contagion component.
    /// Defines parameters for a disease that spreads from the carrier to the target when the carrier attacks.
    /// </summary>
    public class HediffCompProperties_OffensiveContagion : HediffCompProperties
    {
        /// <summary>Base probability (0.0 to 1.0) of infecting the target per successful hit.</summary>
        public float infectionChance = 0.5f;

        /// <summary>The HediffDef to apply to the victim.</summary>
        public HediffDef hediffToApply;

        /// <summary>If true, contagion only occurs via melee attacks (bites, scratches, etc.).</summary>
        public bool onlyMelee = true;

        /// <summary>If true, the attack must deal actual damage to trigger the infection check.</summary>
        public bool requiresDamageToPenetrate = true;

        /// <summary>If true, the infection will be applied to the specific body part that was hit. Allows for medical amputations.</summary>
        public bool transmitToHitPart = true;

        /// <summary>If true, the infection chance is multiplied by the attacker's current disease severity.</summary>
        public bool scaleWithSeverity = false;

        /// <summary>If true, the victim's Toxic Resistance stats will reduce the infection chance.</summary>
        public bool respectsToxicResistance = false;

        /// <summary>Whether to notify the player when a colonist is infected this way.</summary>
        public bool sendNotification = false;

        /// <summary>If true, sends a high-priority Letter; otherwise, a standard Message.</summary>
        public bool useLetterInsteadOfMessage = false;

        public HediffCompProperties_OffensiveContagion()
        {
            this.compClass = typeof(HediffComp_OffensiveContagion);
        }
    }

    /// <summary>
    /// Component that marks a disease as "infectious through attacks."
    /// Used by the Harmony Patch to identify infectious attackers.
    /// </summary>
    public class HediffComp_OffensiveContagion : HediffComp
    {
        public HediffCompProperties_OffensiveContagion Props => (HediffCompProperties_OffensiveContagion)this.props;
    }

    /// <summary>
    /// Harmony patch that intercepts any damage applied to a Pawn to check for contagion.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PostApplyDamage))]
    public static class Patch_Pawn_PostApplyDamage_OffensiveContagion
    {
        /// <summary>
        /// Postfix that runs after damage is dealt.
        /// </summary>
        public static void Postfix(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            // Valid victim and instigator check
            if (__instance == null || __instance.Dead || dinfo.Instigator == null) return;

            // Biological entity check (excludes mechs and special flesh types)
            if (__instance.RaceProps.IsMechanoid || __instance.RaceProps.FleshType != FleshTypeDefOf.Normal) return;

            Pawn attacker = dinfo.Instigator as Pawn;
            if (attacker == null || attacker == __instance) return;

            // Iterate through attacker's diseases to find infectious components
            List<Hediff> attackerHediffs = attacker.health.hediffSet.hediffs;
            for (int i = 0; i < attackerHediffs.Count; i++)
            {
                HediffComp_OffensiveContagion comp = attackerHediffs[i].TryGetComp<HediffComp_OffensiveContagion>();

                if (comp != null)
                {
                    var props = comp.Props;
                    if (props.hediffToApply == null) continue;

                    // Skip if damage was required but not dealt
                    if (props.requiresDamageToPenetrate && totalDamageDealt <= 0f) continue;

                    // Melee safety check: Filters out explosions and ranged projectiles
                    if (props.onlyMelee)
                    {
                        if (dinfo.Def.isExplosive || (dinfo.Weapon != null && dinfo.Weapon.IsRangedWeapon)) continue;
                        if (dinfo.WeaponBodyPartGroup == null && dinfo.Weapon == null) continue;
                    }

                    // Prevent duplicate infections of the same type
                    if (__instance.health.hediffSet.HasHediff(props.hediffToApply)) continue;

                    float finalChance = props.infectionChance;

                    // Severity Scaling: More advanced disease stages result in higher infectivity
                    if (props.scaleWithSeverity)
                    {
                        finalChance *= comp.parent.Severity;
                    }

                    // Toxic Resistance mitigation
                    if (props.respectsToxicResistance)
                    {
                        float bioRes = __instance.GetStatValue(StatDefOf.ToxicResistance);
                        float envRes = __instance.GetStatValue(StatDefOf.ToxicEnvironmentResistance);
                        float bestProtection = Mathf.Max(bioRes, envRes);
                        finalChance *= Mathf.Clamp01(1f - bestProtection);
                    }

                    // Perform the biological roll
                    if (Rand.Chance(finalChance))
                    {
                        // Target hit part logic: applies infection to the limb that was struck
                        if (props.transmitToHitPart && dinfo.HitPart != null)
                        {
                            __instance.health.AddHediff(props.hediffToApply, dinfo.HitPart);
                        }
                        else
                        {
                            __instance.health.AddHediff(props.hediffToApply); // Fallback to whole body
                        }

                        // Player notifications
                        if (props.sendNotification && __instance.Faction == Faction.OfPlayer)
                        {
                            string text = "DF_OffensiveInfection_Message".Translate(__instance.LabelShort, attacker.LabelShort);
                            string label = "DF_OffensiveInfection_LetterLabel".Translate();

                            if (props.useLetterInsteadOfMessage)
                            {
                                Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NegativeEvent, __instance);
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