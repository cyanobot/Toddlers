using HarmonyLib;
using RimWorld;
using System.Reflection;
using Verse;
using static Toddlers.ToddlerUtility;
using static Toddlers.ToddlerPlayUtility;

namespace Toddlers
{
    //raises the threshold at which pawns will do childcare work: play with baby
    [HarmonyPatch(typeof(Need_Play))]
    class Play_IsLow_Patch
    {
        public static MethodBase TargetMethod()
        {
            return typeof(Need_Play).GetProperty(nameof(Need_Play.IsLow), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(false);
        }

        static bool Prefix(ref bool __result, Need_Play __instance, Pawn ___pawn)
        {
            if (__instance.CurLevelPercentage <= 0.4f) __result = true;
            else if (IsLiveToddler(___pawn) && GetLoneliness(___pawn) >= 0.4f && __instance.CurLevelPercentage <= 0.8f)
                __result = true;
            else __result = false;
            return false;
        }
    }
}
