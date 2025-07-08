using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;
using RimWorld.Planet;
using Verse;
using RimWorld;
using Verse.AI;

#if RW_1_5
#else

namespace Toddlers
{
    [HarmonyPatch]
    public static class ToddlersCannotHelp_Patch
    {

        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(JobGiver_PrepareCaravan_GatherDownedPawns), "TryGiveJob");
            yield return AccessTools.Method(typeof(JobGiver_PrepareCaravan_GatherItems), "TryGiveJob");
            yield return AccessTools.Method(typeof(JobGiver_PrepareCaravan_RopePawns), "TryGiveJob");
        }

        public static bool Prefix(ref Job __result, Pawn pawn)
        {
            if (ToddlerUtility.IsToddler(pawn))
            {
                __result = null;
                return false;
            }
            return true;
        }
    }
}
#endif