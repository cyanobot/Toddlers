using HarmonyLib;
using RimWorld;
using Verse;

namespace Toddlers
{
    //don't remove babies/toddlers from bed to play with them if they should be resting for medical reasons
    [HarmonyPatch(typeof(BabyPlayGiver_PlayWalking), nameof(BabyPlayGiver_PlayWalking.CanDo))]
    class BabyPlayGiver_PlayWalking_Patch
    {
        static void Postfix(ref bool __result, Pawn __1)
        {
            if (__result && HealthAIUtility.ShouldSeekMedicalRest(__1) && __1.InBed())
            {
                __result = false;
            }
        }
    }
}
