using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using HarmonyLib;
using static Toddlers.BabyMoveUtility;
using System.Reflection;
using System.Reflection.Emit;
using Verse.AI;

namespace Toddlers
{
    [HarmonyPatch(typeof(ChildcareUtility),nameof(ChildcareUtility.FindUnsafeBaby))]
    public static class FindUnsafeBaby_Patch
    {
        public static bool Prefix(Pawn mom, AutofeedMode priorityLevel, ref Pawn __result)
        {
            __result = FindBabyNeedsMoving(mom, priorityLevel);
            BabyMoveLog("FindUnsafeBaby - mom: " + mom + ", result: " + __result);
            return false;
        }
    }


}
