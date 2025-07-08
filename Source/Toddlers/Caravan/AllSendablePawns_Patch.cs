using HarmonyLib;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace Toddlers
{
    //Can take toddlers on caravans even if they are in a mental state
    [HarmonyPatch(typeof(CaravanFormingUtility), nameof(CaravanFormingUtility.AllSendablePawns))]
    class AllSendablePawns_Patch
    {
        static List<Pawn> Postfix(List<Pawn> result, Map map, bool allowEvenIfInMentalState)
        {
            //if allowEvenIfInMentalState was true, we don't need to meddle
            if (allowEvenIfInMentalState) return result;

            IReadOnlyList<Pawn> allPawnsSpawned = map.mapPawns.AllPawnsSpawned;
            foreach (Pawn pawn in allPawnsSpawned)
            {
                if (pawn.InMentalState && ToddlerUtility.IsLiveToddler(pawn))
                    result.Add(pawn);
            }
            return result;
        }
    }

    
}