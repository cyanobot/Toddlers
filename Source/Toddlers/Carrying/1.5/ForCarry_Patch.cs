using HarmonyLib;
using RimWorld;
using Verse;

namespace Toddlers
{
#if RW_1_5
    [HarmonyPatch(typeof(TargetingParameters), nameof(TargetingParameters.ForCarry))]
    class ForCarry_Patch
    {
        static TargetingParameters Postfix(TargetingParameters result, Pawn p)
        {
            result.onlyTargetIncapacitatedPawns = false;
            result.validator = delegate (TargetInfo targ)
            {
                if (!targ.HasThing) return false;                           //nothing there
                if (ToddlerUtility.IsLiveToddler(p)) return false;          //toddlers can't carry anyone
                Pawn toCarry = targ.Thing as Pawn; 
                if (toCarry == null) return false;                          //no pawn
                if (toCarry == p) return false;                             //can't carry self
                if (ToddlerUtility.IsLiveToddler(toCarry)) return true;     //can carry toddlers
                if (!toCarry.Downed) return false;                          //can't carry non-downed non-toddlers
                return true;
            };
            return result;
        }
    }
#endif
    
}