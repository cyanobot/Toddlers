using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using static Toddlers.ToddlerUtility;

namespace Toddlers
{
    [HarmonyPatch(typeof(PawnBioAndNameGenerator), "GetBackstoryCategoryFiltersFor")]
    class GetBackstoryCategoryFiltersFor_Patch
    {
        public static BackstoryCategoryFilter ToddlerCategoryGroup = new BackstoryCategoryFilter
        {
            categories = new List<string> { "Toddler" },
            commonality = 1f
        };

        static List<BackstoryCategoryFilter> Postfix(List<BackstoryCategoryFilter> __result, Pawn pawn)
        {
            if (IsToddler(pawn))
            {
                return new List<BackstoryCategoryFilter> { ToddlerCategoryGroup };
            }
            return __result;
        }
    }

    
}