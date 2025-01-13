using HarmonyLib;
using RimWorld;
using Verse;

namespace Toddlers
{
    //crying/giggling toddlers still respect forbiddances
    [HarmonyPatch(typeof(ForbidUtility),nameof(ForbidUtility.CaresAboutForbidden))]
    class CaresAboutForbidden_Patch
    {
        static bool Postfix(bool result, Pawn pawn, bool cellTarget)
        {
            //only care about babies/toddlers in mental states
            if (pawn.DevelopmentalStage == DevelopmentalStage.Baby)
            {
                if (pawn.HostFaction != null 
                    && (pawn.HostFaction != Faction.OfPlayer || !pawn.Spawned 
                    || pawn.Map.IsPlayerHome 
                    || (pawn.GetRoom() != null && pawn.GetRoom().IsPrisonCell) 
                    || (pawn.IsPrisoner && !pawn.guest.PrisonerIsSecure)))
                {
                    return false;
                }
                if (SlaveRebellionUtility.IsRebelling(pawn))
                {
                    return false;
                }
                if (cellTarget && ThinkNode_ConditionalShouldFollowMaster.ShouldFollowMaster(pawn))
                {
                    return false;
                }
                return true;
            }
            return result;
        }
    }

    
}