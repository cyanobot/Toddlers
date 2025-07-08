using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Toddlers
{
#if RW_1_5
    [HarmonyPatch(typeof(JobDriver_CarryDownedPawn), "MakeNewToils")]
    class CarryDownedPawn_Patch
    {

        static IEnumerable<Toil> Postfix(IEnumerable<Toil> result, JobDriver_CarryDownedPawn __instance)
        {
            Pawn toCarry = (Pawn)__instance.job.GetTarget(TargetIndex.A).Thing;
            if (!toCarry.DestroyedOrNull() && ToddlerUtility.IsLiveToddler(toCarry))
            {
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.A)
                    .FailOnSomeonePhysicallyInteracting(TargetIndex.A);
                yield return Toils_Haul.StartCarryThing(TargetIndex.A);
            }
            else
            {
                foreach (Toil toil in result) yield return toil;
            }
        }
    }
#endif
    
}