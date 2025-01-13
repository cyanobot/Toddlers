using HarmonyLib;
using RimWorld;
using Verse;

namespace Toddlers
{
    [HarmonyPatch(typeof(Hediff_VatLearning), nameof(Hediff_VatLearning.PostTick))]
    class VatLearning_Patch
    {
        static void Postfix(Pawn ___pawn)
        {
            Hediff_ToddlerLearning learningHediff_walk = (Hediff_ToddlerLearning)___pawn.health.hediffSet.GetFirstHediffOfDef(Toddlers_DefOf.LearningToWalk);
            Hediff_ToddlerLearning learningHediff_manipulation = (Hediff_ToddlerLearning)___pawn.health.hediffSet.GetFirstHediffOfDef(Toddlers_DefOf.LearningManipulation);

            //0.6 = factor so that the growth vat is less efficient than learning by doing
            float factor = (float)Building_GrowthVat.AgeTicksPerTickInGrowthVat * 0.6f;

            if (learningHediff_walk != null)
            {
                learningHediff_walk.InnerTick(factor);
                if (learningHediff_walk.Severity >= 1f) ___pawn.health.RemoveHediff(learningHediff_walk);
            }
            if (learningHediff_manipulation != null)
            {
                learningHediff_manipulation.InnerTick(factor);
                if (learningHediff_manipulation.Severity >= 1f) ___pawn.health.RemoveHediff(learningHediff_manipulation);
            }
        }
    }

    
}