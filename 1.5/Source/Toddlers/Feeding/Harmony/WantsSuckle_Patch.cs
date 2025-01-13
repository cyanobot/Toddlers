using HarmonyLib;
using RimWorld;
using Verse;
using static Toddlers.ToddlerUtility;
using static Toddlers.FeedingUtility;
using static Toddlers.ToddlerLearningUtility;

namespace Toddlers
{
    //
    [HarmonyPatch(typeof(ChildcareUtility),nameof(ChildcareUtility.WantsSuckle))]
    class WantsSuckle_Patch
    {
        static bool Postfix(bool result, Pawn baby, ref ChildcareUtility.BreastfeedFailReason? reason)
        {
            if (!result) return false;
            if (!IsToddler(baby)) return result;
            if (IsToddlerEatingUrgently(baby)) return false;
            if (!Toddlers_Settings.feedCapableToddlers && CanFeedSelf(baby) && FoodUtility.TryFindBestFoodSourceFor(baby, baby, false, out var _, out var _)) return false;
            return result;
        }

    }

    
}