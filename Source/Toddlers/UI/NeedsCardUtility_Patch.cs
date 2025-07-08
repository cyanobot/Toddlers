using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using Verse;

namespace Toddlers
{
    //don't show baby talk for toddlers
    [HarmonyPatch(typeof(NeedsCardUtility), "DrawThoughtGroup")]
    class NeedsCardUtility_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            /*
            if (Toddlers_Settings.toddlerBabyTalk)
            {
                foreach (var instruction in instructions) yield return instruction;
                yield break;
            }
            */

            bool foundStart = false;
            bool foundEnd = false;
            bool foundTarget = false;
            bool done = false;
            int targetDist = 4;

            Label targetLabel = il.DefineLabel();

            object methodDevStage = AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.DevelopmentalStage));
            object methodAppendTagged = AccessTools.Method(typeof(ColoredText), nameof(ColoredText.AppendTagged), new Type[] { typeof(StringBuilder), typeof(TaggedString) });

            foreach (var instruction in instructions)
            {

                if (done)
                {
                    yield return instruction;
                    continue;
                }

                if (!foundStart && instruction.opcode == OpCodes.Callvirt && instruction.OperandIs(methodDevStage))
                {
                    foundStart = true;
                    continue;
                }

                //skip ahead until we find the end point
                if (foundStart && !foundEnd)
                {
                    if (instruction.opcode == OpCodes.Brfalse_S || instruction.opcode == OpCodes.Brfalse)
                    {
                        foundEnd = true;

                        //insert our own instructions
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn), nameof(Pawn.ageTracker)));
                        yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Pawn_AgeTracker), nameof(Pawn_AgeTracker.CurLifeStageIndex)));
                        yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Toddlers_Settings), nameof(Toddlers_Settings.ToddlerTalkInt)));
                        yield return new CodeInstruction(OpCodes.Bgt, targetLabel);

                    }

                    continue;
                }

                //look for the target that will let us identify where we want to put the label
                if (foundEnd && !foundTarget)
                {
                    if (instruction.opcode == OpCodes.Call && instruction.OperandIs(methodAppendTagged))
                    {
                        foundTarget = true;
                    }
                }

                //we actually want to jump back in 3 lines below the target, so we count
                if (foundTarget && targetDist > 0)
                {
                    targetDist--;
                }

                if (targetDist == 0 && !done)
                {
                    instruction.labels.Add(targetLabel);
                    done = true;
                }

                yield return instruction;
            }
        }
    }

    
}