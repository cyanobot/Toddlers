using HarmonyLib;
using System.Collections.Generic;
using Verse;

namespace Toddlers
{
    [HarmonyPatch(typeof(Pawn),nameof(Pawn.GetDisabledWorkTypes))]
    class DisabledWorkTypes_Patch
    {
        static List<WorkTypeDef> Postfix(List<WorkTypeDef> result, Pawn __instance)
        {
            if (ToddlerUtility.IsToddler(__instance))
            {
                return DefDatabase<WorkTypeDef>.AllDefsListForReading;
            }
            else return result;
        }
    }

    
}