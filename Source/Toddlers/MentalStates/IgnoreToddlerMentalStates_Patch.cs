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

#if RW_1_5
#else

namespace Toddlers
{
    [HarmonyPatch]
    public static class IgnoreToddlerMentalStates_Patch
    {
        public static MethodInfo m_get_InMentalState = AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.InMentalState));
        public static MethodInfo m_MentalStateAndNotBaby = AccessTools.Method(typeof(IgnoreToddlerMentalStates_Patch), nameof(MentalStateAndNotBaby));

        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(CaravanFormingUtility), nameof(CaravanFormingUtility.AllSendablePawns));
            yield return AccessTools.Method(typeof(CaravanFormingUtility), nameof(CaravanFormingUtility.ForceCaravanDepart));
            yield return AccessTools.Method(typeof(CaravanFormingUtility), nameof(CaravanFormingUtility.GetForceDepartWarningMessage));
        }

        public static bool MentalStateAndNotBaby(Pawn pawn)
        {
            return pawn.InMentalState && pawn.DevelopmentalStage != DevelopmentalStage.Baby;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction cur in instructions)
            {
                if (cur.Calls(m_get_InMentalState))
                {
                    yield return new CodeInstruction(OpCodes.Call, m_MentalStateAndNotBaby);
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