using HarmonyLib;
using RimWorld;
using Verse;

namespace Toddlers
{
    [HarmonyPatch(typeof(ChildcareUtility), nameof(ChildcareUtility.SwaddleBaby))]
    class SwaddleBaby_Patch
    {
        static bool Prefix(ref bool __result, Pawn baby)
        {
            if (ToddlerUtility.IsToddler(baby))
            {
                __result = false;
                return false;
            }
            else if (baby.DevelopmentalStage.Baby() && !baby.apparel.PsychologicallyNude)
            {
                __result = false;
                return false;
            }
            else return true;
        }
    }
}
