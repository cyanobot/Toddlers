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

#if RW_1_5
#else

namespace Toddlers

{
    [HarmonyPatch]
    public static class TreatToddlerAsDowned_Patch
    {
        public static MethodInfo m_get_Downed = AccessTools.PropertyGetter(typeof(Pawn),nameof(Pawn.Downed));
        public static MethodInfo m_DownedOrToddler = AccessTools.Method(typeof(TreatToddlerAsDowned_Patch), nameof(DownedOrToddler));
        
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(JobGiver_PrepareCaravan_GatherDownedPawns), "FindDownedPawn");
            yield return AccessTools.Method(typeof(JobGiver_GotoTravelDestination), "CaravanBabyToCarry");
            yield return AccessTools.Method(typeof(LordJob_FormAndSendCaravan), nameof(LordJob_FormAndSendCaravan.LordJobTick));
            yield return AccessTools.Method(typeof(CaravanFormingUtility), nameof(CaravanFormingUtility.LateJoinFormingCaravan));
        }

        public static bool DownedOrToddler(Pawn pawn)
        {
            return pawn.Downed || ToddlerUtility.IsToddler(pawn);
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction cur in instructions)
            {
                if (cur.Calls(m_get_Downed))
                {
                    yield return new CodeInstruction(OpCodes.Call, m_DownedOrToddler);
                }
                else
                {
                    yield return cur;
                }
            }
        }
    }
}
#endif
