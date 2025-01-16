using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using static Toddlers.ToddlerPlayUtility;

namespace Toddlers
{
    [HarmonyPatch(typeof(ChildcareUtility), nameof(ChildcareUtility.MakeBabyPlayAsLongAsToilIsActive))]
    class MakeBabyPlayAsLongAsToilIsActive_Patch
    {
        static Toil Postfix(Toil toil, TargetIndex babyIndex)
        {
            toil.AddPreTickAction(delegate
            {
                CureLoneliness((Pawn)toil.actor.jobs.curJob.GetTarget(babyIndex).Thing);
            });
            return toil;
        }
    }
}
