using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Toddlers
{
    //replaces the Pawns property for the schedule window
    //to allow babies and toddlers to appear in it
    [HarmonyPatch(typeof(MainTabWindow_Schedule),"Pawns",MethodType.Getter)]
    class MainTabWindow_Schedule_Patch
    {
        static bool Prefix(ref IEnumerable<Pawn> __result)
        {
            __result = Find.CurrentMap.mapPawns.FreeColonists;

            return false;
        }
    }

    
}