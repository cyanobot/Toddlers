using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Toddlers
{
    //can always kidnap toddlers
    [HarmonyPatch(typeof(KidnapAIUtility),nameof(KidnapAIUtility.TryFindGoodKidnapVictim))]
    class TryFindGoodKidnapVictim_Patch
    {
        //very slightly different from vanilla
        private static bool KidnapValidator(Thing t, Pawn kidnapper, List<Thing> disallowed = null)
        {
            Pawn pawn = t as Pawn;
            if (!pawn.RaceProps.Humanlike)
            {
                return false;
            }
            if (!pawn.Downed && !ToddlerUtility.IsToddler(pawn))
            {
                return false;
            }
            if (pawn.Faction != Faction.OfPlayer)
            {
                return false;
            }
            if (!pawn.Faction.HostileTo(kidnapper.Faction))
            {
                return false;
            }
            if (!kidnapper.CanReserve(pawn))
            {
                return false;
            }
            return (disallowed == null || !disallowed.Contains(pawn)) ? true : false;
        }

        //same as vanilla except uses adjusted validator above
        static bool Prefix(ref bool __result, Pawn kidnapper, float maxDist, out Pawn victim, List<Thing> disallowed = null)
        {
            if (!kidnapper.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) || !kidnapper.Map.reachability.CanReachMapEdge(kidnapper.Position, TraverseParms.For(kidnapper, Danger.Some)))
            {
                victim = null;
                __result = false;
                return false;
            }

            victim = (Pawn)GenClosest.ClosestThingReachable(kidnapper.Position, kidnapper.Map, 
                ThingRequest.ForGroup(ThingRequestGroup.Pawn), PathEndMode.OnCell, 
                TraverseParms.For(TraverseMode.NoPassClosedDoors, Danger.Some), maxDist, 
                t => KidnapValidator(t,kidnapper,disallowed));
            __result = victim != null;

            return false;
        }
    }

    
}