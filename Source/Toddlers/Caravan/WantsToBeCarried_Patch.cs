using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace Toddlers
{
    //toddlers should be carried on caravans if possible
    [HarmonyPatch(typeof(Caravan_CarryTracker),"WantsToBeCarried")]
    class WantsToBeCarried_Patch
    {
        static bool Prefix(ref bool __result, Pawn p)
        {
            if (ToddlerUtility.IsToddler(p))
            {
                __result = true;
                return false;
            }

            return true;
        }
    }

    
}