using HarmonyLib;
using RimWorld;
using Verse;

namespace Toddlers
{
    //without this toddlers won't eat baby food because it's desperateonly
    [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.TryFindBestFoodSourceFor))]
    class BestFoodSource_Patch
    {
        static void Prefix(Pawn eater, ref FoodPreferability minPrefOverride)
        {
            if (ToddlerUtility.IsToddler(eater)) minPrefOverride = FoodPreferability.DesperateOnly;
        }
    }

    
}