using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI;

namespace Toddlers
{
    //don't  show mental break  warnings for toddlers
    //or for that matter any other pawn who can't do random mental breaks
    [HarmonyPatch(typeof(MentalBreaker))]
    class BreakExtremeIsImminent_Patch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return typeof(MentalBreaker).GetProperty(nameof(MentalBreaker.BreakExtremeIsImminent), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(false);
            yield return typeof(MentalBreaker).GetProperty(nameof(MentalBreaker.BreakMajorIsImminent), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(false);
            yield return typeof(MentalBreaker).GetProperty(nameof(MentalBreaker.BreakMinorIsImminent), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(false);
            yield return typeof(MentalBreaker).GetProperty(nameof(MentalBreaker.BreakExtremeIsApproaching), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(false);
        }

        static bool Postfix(bool result, Pawn ___pawn)
        {
            if (result && !___pawn.mindState.mentalBreaker.CanDoRandomMentalBreaks)
                return false;
            return result;
        }
    }

    
}