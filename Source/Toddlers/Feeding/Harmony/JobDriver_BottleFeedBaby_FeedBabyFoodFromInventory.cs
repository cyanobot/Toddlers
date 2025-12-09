using HarmonyLib;
using RimWorld;
using System;
using Verse;
using Verse.AI;

namespace Toddlers
{
    // Make toddlers create mess filth when being fed.
    [HarmonyPatch(typeof(JobDriver_BottleFeedBaby), nameof(JobDriver_BottleFeedBaby.FeedBabyFoodFromInventory))]
    class JobDriver_BottleFeedBaby_Patch
    {
        static Toil Postfix(Toil result, JobDriver_BottleFeedBaby __instance)
        {
            Pawn feeder = __instance.pawn;
            Pawn baby = __instance.Baby;
            if (ToddlerUtility.IsToddler(baby) && feeder.Map != null)
            {
                result.AddPreTickAction(() => FeedingUtility.TryMakeMess(feeder, baby));
            }
                
            return result;
        }
    }
}