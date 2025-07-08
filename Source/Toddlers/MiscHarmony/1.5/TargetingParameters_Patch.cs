using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

#if RW_1_5
namespace Toddlers
{
    [HarmonyPatch]
    class TargetingParameters_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return typeof(TargetingParameters).GetMethod(nameof(TargetingParameters.ForArrest));
            yield return typeof(TargetingParameters).GetMethod(nameof(TargetingParameters.ForCarryDeathresterToBed));
            yield return typeof(TargetingParameters).GetMethod(nameof(TargetingParameters.ForCarryToBiosculpterPod));
            yield return typeof(TargetingParameters).GetMethod(nameof(TargetingParameters.ForDraftedCarryBed));
            yield return typeof(TargetingParameters).GetMethod(nameof(TargetingParameters.ForDraftedCarryCryptosleepCasket));
            yield return typeof(TargetingParameters).GetMethod(nameof(TargetingParameters.ForRescue));
            yield return typeof(TargetingParameters).GetMethod(nameof(TargetingParameters.ForShuttle));
        }

        static void Postfix(object[] __args, MethodBase __originalMethod, ref TargetingParameters __result)
        {
            Pawn p;
            if (__originalMethod == typeof(TargetingParameters).GetMethod(nameof(TargetingParameters.ForDraftedCarryBed))) 
            {
                p = (Pawn)__args[1];
            }
            else
            {
                p = (Pawn)__args[0];
            }

            if (ToddlerUtility.IsToddler(p))
            {
                __result.canTargetPawns = false;
            }
            else if (__originalMethod == typeof(TargetingParameters).GetMethod(nameof(TargetingParameters.ForRescue)))
            {
                __result.onlyTargetIncapacitatedPawns = false;
                __result.validator = delegate (TargetInfo targ)
                {
                    if (!targ.HasThing || !(targ.Thing is Pawn pawn))
                    {
                        return false;
                    }
                    return pawn.Downed || ToddlerUtility.IsLiveToddler(pawn);
                };
            }
            else if (__originalMethod == typeof(TargetingParameters).GetMethod(nameof(TargetingParameters.ForShuttle)))
            {
                Predicate<TargetInfo> oldValidator = __result.validator;
                __result.validator = delegate (TargetInfo targ)
                {
                    if (oldValidator(targ))
                    {
                        return true;
                    }
                    if (!targ.HasThing || !(targ.Thing is Pawn pawn))
                    {
                        return false;
                    }
                    // Allow loading toddlers even if not downed.
                    return ToddlerUtility.IsLiveToddler(pawn);
                };
            }
        }
    }

    
}
#endif