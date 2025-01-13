using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Toddlers
{
    //replaces the Pawns property for the assign window
    //to allow both babies and toddlers to appear in it
    [HarmonyPatch(typeof(MainTabWindow_Assign), "Pawns", MethodType.Getter)]
    class MainTabWindow_Assign_Patch
    {
        static bool Prefix(ref IEnumerable<Pawn> __result)
        {
            __result = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists;

            return false;
        }
    }

    
}