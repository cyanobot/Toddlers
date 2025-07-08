using HarmonyLib;
using Verse;
using static Toddlers.ToddlerUtility;

namespace Toddlers
{
    [HarmonyPatch(typeof(StrippableUtility), nameof(StrippableUtility.CanBeStrippedByColony))]
    class CanBeStrippedByColony_Patch
    {
        static bool Postfix(bool __result, Thing th)
        {
            if (__result) return true;

            if (th is Pawn pawn && IsToddler(pawn))
            {
                if (!(pawn as IStrippable).AnythingToStrip()) return false;

                else return true;
            }

            return __result;
        }
    }
}
