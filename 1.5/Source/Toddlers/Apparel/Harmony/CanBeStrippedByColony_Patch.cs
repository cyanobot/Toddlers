using HarmonyLib;
using Verse;

namespace Toddlers
{
    [HarmonyPatch(typeof(StrippableUtility), nameof(StrippableUtility.CanBeStrippedByColony))]
    class CanBeStrippedByColony_Patch
    {
        static bool Postfix(bool result, Thing th)
        {
            if (th is Pawn pawn && ToddlerUtility.IsToddler(pawn)) return true;
            else return result;
        }
    }
}
