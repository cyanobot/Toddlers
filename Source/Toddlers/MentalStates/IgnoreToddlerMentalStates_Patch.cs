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
using Verse.AI.Group;

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
            yield return AccessTools.Method(typeof(ForbidUtility), nameof(ForbidUtility.CaresAboutForbidden));
            yield return AccessTools.Method(typeof(Trigger_MentalState), nameof(Trigger_MentalState.ActivateOn));
            yield return AccessTools.Method(typeof(Trigger_NoMentalState), nameof(Trigger_NoMentalState.ActivateOn));
            yield return AccessTools.Method(typeof(GatheringsUtility), nameof(GatheringsUtility.PawnCanStartOrContinueGathering));
            yield return AccessTools.Method(typeof(RitualRoleAssignments), nameof(RitualRoleAssignments.PawnNotAssignableReason)
                , new Type[] { typeof(Pawn), typeof(RitualRole), typeof(Precept_Ritual), typeof(RitualRoleAssignments), typeof(TargetInfo), typeof(bool).MakeByRefType() });
            yield return AccessTools.Method(typeof(CompShuttle), "PawnIsHealthyEnoughForShuttle");
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