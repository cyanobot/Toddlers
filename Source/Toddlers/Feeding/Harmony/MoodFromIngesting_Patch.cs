using HarmonyLib;
using RimWorld;
using Verse;

namespace Toddlers
{
    //stops (Disliked food) from showing for toddlers considering baby food
    [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.MoodFromIngesting))]
    class MoodFromIngesting_Patch
    {
        static bool Prefix(ref float __result, Pawn ingester)
        {
            if (ingester.DevelopmentalStage == DevelopmentalStage.Baby)
            {
                __result = 0f;
                return false;
            }
            return true;
        }
    }

    
}