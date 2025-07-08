using Verse;
using RimWorld;
using HarmonyLib;
using static Toddlers.BabyMoveUtility;

namespace Toddlers
{
    [HarmonyPatch(typeof(ChildcareUtility),nameof(ChildcareUtility.SafePlaceForBaby))]
    public static class SafePlaceForBaby_Patch
    {
        public static bool Prefix(Pawn baby, Pawn hauler, bool ignoreOtherReservations, ref LocalTargetInfo __result)
        {
            __result = BestPlaceForBaby(baby, hauler, ignoreOtherReservations);
            return false;
        }
    }


}
