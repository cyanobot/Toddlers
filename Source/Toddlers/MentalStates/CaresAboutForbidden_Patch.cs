using HarmonyLib;
using RimWorld;
using Verse;

namespace Toddlers
{
    //crying/giggling toddlers still respect forbiddances
    [HarmonyPatch(typeof(ForbidUtility),nameof(ForbidUtility.CaresAboutForbidden))]
    class CaresAboutForbidden_Patch
    {
        static bool Postfix(bool result, Pawn pawn, bool cellTarget, bool bypassDraftedCheck)
        {
            //if they already care about forbidden, nothing need to change
            if (result == true) return result;

            //only care about babies/toddlers in mental states
            if (pawn.DevelopmentalStage == DevelopmentalStage.Baby && pawn.InMentalState)
            {
                //if they don't already care and they're in a mental state
                //check there's no other reason they don't care
                //using vanilla checks
                
                if (pawn.Drafted)
                {
#if RW_1_5
                    return false;
#else
                    if (!bypassDraftedCheck) return false;
#endif
                }

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