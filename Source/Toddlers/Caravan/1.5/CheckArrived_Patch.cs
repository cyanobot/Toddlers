using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;

namespace Toddlers
{
#if RW_1_5
    //accept carried toddlers as ready to  leave on caravans
    [HarmonyPatch(typeof(GatherAnimalsAndSlavesForCaravanUtility),nameof(GatherAnimalsAndSlavesForCaravanUtility.CheckArrived))]
    class CheckArrived_Patch
    {
        static MethodInfo m_Spawned = AccessTools.Property(typeof(Thing), nameof(Thing.Spawned)).GetGetMethod();
        static MethodInfo m_Position = AccessTools.Property(typeof(Thing), nameof(Thing.Position)).GetGetMethod();
        static MethodInfo m_PositionHeld = AccessTools.Property(typeof(Thing), nameof(Thing.PositionHeld)).GetGetMethod();
        static MethodInfo m_IsToddler = AccessTools.Method(typeof(ToddlerUtility), nameof(ToddlerUtility.IsToddler));
        //public static bool CanReach(this Pawn pawn, LocalTargetInfo dest, PathEndMode peMode, Danger maxDanger, bool canBashDoors = false, bool canBashFences = false, TraverseMode mode = TraverseMode.ByPawn)
        static MethodInfo m_CanReach = AccessTools.Method(typeof(ReachabilityUtility), nameof(ReachabilityUtility.CanReach), parameters: new Type[] { typeof(Pawn), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(Danger), typeof(bool), typeof(bool), typeof(TraverseMode) });

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction prevInstruction = null;
            CodeInstruction getPawn = null;
            foreach (var instruction in instructions)
            {
                //replace
                //pawn.Spawned
                //with
                //pawn.Spawned || IsToddler(pawn)
                if (instruction.Calls(m_Spawned))
                {
                    getPawn = prevInstruction;

                    yield return instruction;
                    yield return getPawn;
                    yield return new CodeInstruction(OpCodes.Callvirt, m_IsToddler);
                    yield return new CodeInstruction(OpCodes.Or);
                }
                else if (instruction.Calls(m_Position))
                {
                    yield return new CodeInstruction(OpCodes.Callvirt, m_PositionHeld);
                }
                else if (instruction.Calls(m_CanReach))
                {
                    yield return instruction;
                    if (getPawn != null)
                        yield return getPawn;
                    else
                        yield return new CodeInstruction(OpCodes.Ldloc_2);
                    yield return new CodeInstruction(OpCodes.Callvirt, m_IsToddler);
                    yield return new CodeInstruction(OpCodes.Or);
                }
                else
                {
                    yield return instruction;
                }

                prevInstruction = instruction;
            }
        }
    }   

#endif
}