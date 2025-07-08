using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Toddlers
{
    //don't remove babies/toddlers from bed to play with them if they should be resting for medical reasons
    //and if they're downed for medical reasons but there's no bed, still don't put them on the floor to play that's weird
    [HarmonyPatch()]
    class BabyPlayGiver_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return typeof(BabyPlayGiver_PlayStatic).GetMethod(nameof(BabyPlayGiver_PlayStatic.CanDo));
            yield return typeof(BabyPlayGiver_PlayToys).GetMethod(nameof(BabyPlayGiver_PlayToys.CanDo));
        }

        static void Postfix(ref bool __result, Pawn __1)
        {
            if (__result && HealthAIUtility.ShouldSeekMedicalRest(__1)
                && (__1.InBed() || __1.Downed))
            {
                __result = false;
            }
        }
    }
}
