using HarmonyLib;
using Verse;
using static Toddlers.ToddlerUtility;

namespace Toddlers
{
    //toddlers should never count as threats
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.ThreatDisabled))]
    class ThreatDisabled_Patch
    {
        static bool Postfix(bool result, Pawn __instance)
        {
            if (result) return true;
            if (IsToddler(__instance)) return true;
            return false;
        }
    }

    
}