using HarmonyLib;
using RimWorld;
using Verse;

namespace Toddlers
{
    [HarmonyPatch(typeof(Building_Door), nameof(Building_Door.PawnCanOpen))]
    class Door_Patch
    {
        static bool Prefix(ref bool __result, Pawn p)
        {
            //Log.Message("Firing Door_Patch");
            if (ToddlerLearningUtility.IsCrawler(p))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    
}