using HarmonyLib;
using RimWorld;
using System;
using Verse;
using Verse.AI;

namespace Toddlers
{
    // Make toddlers create mess filth when being fed.
    // 1.7: FeedBabyFoodFromInventory became private - nameof() no longer compiles across assemblies.
#if RW_1_7
    [HarmonyPatch(typeof(JobDriver_BottleFeedBaby), "FeedBabyFoodFromInventory")]
#else
    [HarmonyPatch(typeof(JobDriver_BottleFeedBaby), nameof(JobDriver_BottleFeedBaby.FeedBabyFoodFromInventory))]
#endif
    class JobDriver_BottleFeedBaby_Patch
    {
        static Toil Postfix(Toil result, JobDriver_BottleFeedBaby __instance)
        {
            Pawn feeder = __instance.pawn;
#if RW_1_7
            // 1.7: the Baby property was removed; targetA of the bottlefeed job is the baby.
            Pawn baby = (Pawn)__instance.job.GetTarget(TargetIndex.A).Thing;
#else
            Pawn baby = __instance.Baby;
#endif
            if (ToddlerUtility.IsToddler(baby) && feeder.Map != null)
            {
                result.AddPreTickAction(() => FeedingUtility.TryMakeMess(feeder, baby));
            }
                
            return result;
        }
    }
}