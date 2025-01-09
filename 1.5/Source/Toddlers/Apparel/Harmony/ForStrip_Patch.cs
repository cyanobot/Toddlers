using HarmonyLib;
using RimWorld;
using Verse;

namespace Toddlers
{
    [HarmonyPatch(typeof(TargetingParameters), nameof(TargetingParameters.ForStrip))]
    class ForStrip_Patch
    {
        static void Postfix(ref TargetingParameters __result, Pawn p)
        {
            if (!ToddlerUtility.CanDressSelf(p))
            {
                __result.canTargetPawns = false;
                __result.canTargetItems = false;
            }
        }
    }
}
