using HarmonyLib;
using Verse;
using RimWorld;

namespace DiseasesFramework.InfectionVectors.DF_Zoonosis
{
    /// <summary>
    /// Harmony patch for TendUtility.DoTend.
    /// Triggers a zoonotic check when a doctor tends to an animal's wounds or illnesses.
    /// </summary>
    [HarmonyPatch(typeof(TendUtility), "DoTend")]
    public static class Patch_Zoonosis_Tend
    {
        [HarmonyPostfix]
        public static void PostFix(Pawn doctor, Pawn patient)
        {
            if (doctor == null || patient == null || !patient.RaceProps.Animal || !doctor.RaceProps.Humanlike)
                return;

            foreach (Hediff hediff in patient.health.hediffSet.hediffs)
            {
                var comp = hediff.TryGetComp<HediffComp_Zoonosis>();
                if (comp != null)
                {
                    // Uses the 'tendingInfectionChance' due to direct medical contact.
                    comp.CheckAndTryInfect(doctor, isTending: true);
                }
            }
        }
    }

    /// <summary>
    /// Harmony patch for Pawn_InteractionsTracker.TryInteractWith.
    /// Handles infections during social interactions like taming, training, or nuzzling.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_InteractionsTracker), "TryInteractWith")]
    public static class Patch_Zoonosis_Interact
    {
        /// <summary>
        /// Postfix that captures interactions between two pawns.
        /// </summary>
        /// <param name="__result">True if the interaction was successful.</param>
        /// <param name="recipient">The pawn receiving the interaction.</param>
        /// <param name="___pawn">The pawn initiating the interaction (private field).</param>
        [HarmonyPostfix]
        public static void PostFix(bool __result, Pawn recipient, Pawn ___pawn)
        {
            if (!__result || recipient == null || ___pawn == null) return;

            // Case A: Human initiates interaction with an animal (e.g., Taming/Training)
            if (___pawn.RaceProps.Humanlike && recipient.RaceProps.Animal)
            {
                foreach (Hediff hediff in recipient.health.hediffSet.hediffs)
                {
                    var comp = hediff.TryGetComp<HediffComp_Zoonosis>();
                    if (comp != null) comp.CheckAndTryInfect(___pawn, isTending: false);
                }
            }
            // Case B: Animal initiates interaction with a human (e.g., Nuzzling)
            else if (___pawn.RaceProps.Animal && recipient.RaceProps.Humanlike)
            {
                foreach (Hediff hediff in ___pawn.health.hediffSet.hediffs)
                {
                    var comp = hediff.TryGetComp<HediffComp_Zoonosis>();
                    if (comp != null) comp.CheckAndTryInfect(recipient, isTending: false);
                }
            }
        }
    }

    /// <summary>
    /// Harmony patch for CompHasGatherableBodyResource.Gathered.
    /// Handles infection risks during milking or shearing activities.
    /// </summary>
    [HarmonyPatch(typeof(CompHasGatherableBodyResource), "Gathered")]
    public static class Patch_Zoonosis_Gather
    {
        [HarmonyPostfix]
        public static void Postfix(CompHasGatherableBodyResource __instance, Pawn doer)
        {
            if (doer == null || !doer.RaceProps.Humanlike) return;

            if (__instance.parent is Pawn animal && animal.RaceProps.Animal)
            {
                foreach (Hediff hediff in animal.health.hediffSet.hediffs)
                {
                    var comp = hediff.TryGetComp<HediffComp_Zoonosis>();
                    if (comp != null) comp.CheckAndTryInfect(doer, isTending: false);
                }
            }
        }
    }

    /// <summary>
    /// Harmony patch for Pawn_CarryTracker.TryStartCarry.
    /// Triggers infection checks when a pawn is physically carried (e.g., Rescuing or transporting).
    /// </summary>
    [HarmonyPatch(typeof(Pawn_CarryTracker), "TryStartCarry", new System.Type[] { typeof(Thing), typeof(int), typeof(bool) })]
    public static class Patch_Zoonosis_Carry
    {
        [HarmonyPostfix]
        public static void Postfix(int __result, Thing item, Pawn ___pawn)
        {
            // Verify that an animal was successfully picked up by a human carrier.
            if (__result <= 0 || ___pawn == null || !___pawn.RaceProps.Humanlike || !(item is Pawn animal) || !animal.RaceProps.Animal)
                return;

            foreach (Hediff hediff in animal.health.hediffSet.hediffs)
            {
                var comp = hediff.TryGetComp<HediffComp_Zoonosis>();
                if (comp != null)
                {
                    // Carrying involves intense physical contact, treated as a 'tending' risk level.
                    comp.CheckAndTryInfect(___pawn, isTending: true);
                }
            }
        }
    }

    /// <summary>
    /// Harmony patch for Corpse.ButcherProducts.
    /// Handles infection risks when a butcher processes an infected animal's carcass.
    /// </summary>
    [HarmonyPatch(typeof(Verse.Corpse), "ButcherProducts")]
    public static class Patch_Zoonosis_Butcher
    {
        [HarmonyPrefix]
        public static void Prefix(Verse.Corpse __instance, Pawn butcher)
        {
            if (butcher == null || !butcher.RaceProps.Humanlike) return;

            // Extract the original pawn data from the corpse.
            Pawn animal = __instance.InnerPawn;

            if (animal != null && animal.RaceProps.Animal)
            {
                // Checks diseases the animal carried at the time of death.
                foreach (Hediff hediff in animal.health.hediffSet.hediffs)
                {
                    var comp = hediff.TryGetComp<HediffComp_Zoonosis>();
                    if (comp != null)
                    {
                        // Pass 'true' to the isButchering parameter for the highest risk level.
                        comp.CheckAndTryInfect(butcher, isTending: false, isButchering: true);
                    }
                }
            }
        }
    }
}