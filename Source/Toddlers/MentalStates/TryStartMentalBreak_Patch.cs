using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;

namespace Toddlers
{
    //don't break draft for toddlers entering mental states
    //since they cry/giggle a lot
    [HarmonyPatch(typeof(MentalStateHandler),nameof(MentalStateHandler.TryStartMentalState))]
    class TryStartMentalBreak_Patch
    {
        public static MentalStateDef[] babyStateDefs = new MentalStateDef[]
        {
            DefDatabase<MentalStateDef>.GetNamed("Crying"),
            DefDatabase<MentalStateDef>.GetNamed("Giggling"),
            Toddlers_DefOf.RemoveClothes
        };

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            int babyEnumInt = (int)DevelopmentalStage.Baby;
            
            bool foundGetDrafted = false;
            bool changedPointer = false;
            bool insertedCode = false;
            bool foundSetDrafted = false;
            bool setPostLabel = false;

            Label defTestingFoundMatch = il.DefineLabel();
            Label defTestingEnd = il.DefineLabel();
            Label postSetDrafted = il.DefineLabel();

            foreach (CodeInstruction instruction in instructions)
            {
                //if we've already set the final label then we are done here
                //don't need to fuck with any further instructions
                if (setPostLabel)
                {
                    ;
                }

                //if we haven't yet found the get_Drafted instruction then we need to be looking for it
                else if (!foundGetDrafted)
                {
                    //if we see the instruction we're looking for, set the flag to say we've found it
                    if (instruction.Calls(typeof(Pawn).GetProperty(nameof(Pawn.Drafted)).GetGetMethod()))
                    {
                        foundGetDrafted = true;
                    }

                    //if we don't see it, just carry on
                }

                //if we have found get_Drafted
                //if we haven't yet changed the pointer
                //then we must be on the line directly after the get Drafted instruction
                //which ought to be the pointer
                else if (!changedPointer)
                {
                    //check that we really are dealing with a Brfalse
                    //otherwise something has gone totally wrong
                    if (instruction.opcode == OpCodes.Brfalse_S)
                    {
                        //change it from the line reference
                        //which might be messed up by all this insertion
                        //to the new label for this purpose
                        instruction.operand = postSetDrafted;
                    }
                    else
                    {
                        Log.Error("Toddlers: Transpiler for MentalStateHandler.TryStartMentalState failed to find brfalse.s after call to Verse.Pawn::get_Drafted()");
                    }

                    //set the flag to say we've done this
                    changedPointer = true;
                }

                //if we have changed the pointer, and thus are past the pointer
                //but we haven't yet inserted our code
                //then insert said code
                else if (!insertedCode)
                {
                    //arg0 ought to be "this" aka the instance of MentalStateHandler
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    //get the pawn field from this MentalStateHandler
                    yield return CodeInstruction.LoadField(typeof(MentalStateHandler), "pawn");
                    //get pawn.DevelopmentalStage
                    yield return CodeInstruction.Call(typeof(Pawn),
                        typeof(Pawn).GetProperty(nameof(Pawn.DevelopmentalStage)).GetGetMethod().Name);
                    //convert (hopefully) from enum to int32
                    yield return new CodeInstruction(OpCodes.Conv_I4);

                    //leave this int on the stack

                    //put the int that corresponds to DevelopmentalStage.Baby also onto the stack
                    yield return new CodeInstruction(OpCodes.Ldc_I4, babyEnumInt);
                    
                    //compare pawn.DevelopmentalStage to DevelopmentalStage.Baby
                    yield return new CodeInstruction(OpCodes.Ceq);

                    //stack now contains:
                    //1 if pawn is DevelopmentalStage.Baby
                    //0 if not

                    foreach (MentalStateDef stateDef in babyStateDefs)
                    {
                        //arg1 ought to be the MentalStateDef that TryStartMentalState is being called
                        yield return new CodeInstruction(OpCodes.Ldarg_1);

                        //leave this on the stack

                        //put the name of the stateDef we want to test against onto the stack
                        yield return new CodeInstruction(OpCodes.Ldstr, stateDef.defName);
                        //put a zero onto the stack for the second argument of GetNamed
                        yield return new CodeInstruction(OpCodes.Ldc_I4_0);

                        //call DefDatabase.GetNamed to get the stateDef
                        MethodInfo getNamedDef = typeof(DefDatabase<MentalStateDef>).GetMethod(nameof(DefDatabase<MentalStateDef>.GetNamed));
                        yield return CodeInstruction.Call(typeof(DefDatabase<MentalStateDef>), getNamedDef.Name);

                        //if we found one that matches, we don't need to test any others
                        yield return new CodeInstruction(OpCodes.Beq, defTestingFoundMatch);
                    }

                    //this only fires if no match found
                    //because if we found a match we'd have jumped ahead
                    //load 0 onto the stack
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    //and jump to the end of the def testing
                    //skipping the "match found" block
                    yield return new CodeInstruction(OpCodes.Br, defTestingEnd);

                    //next the "match found" action
                    //load 1 onto the stack
                    CodeInstruction instruction1 = new CodeInstruction(OpCodes.Ldc_I4_1);
                    instruction1.labels.Add(defTestingFoundMatch);
                    yield return instruction1;

                    //stack now contains:
                    // 1/0 based on pawn.DevelopmentalStage == DevelopmentalStage.Baby
                    // 1/0 based on match found or not for the stateDef

                    //if both are true we want to skip ahead and not cancel the drafting
                    //exactly as we would jump if not drafted in the first place

                    //and them to find out if both are true
                    CodeInstruction instruction2 = new CodeInstruction(OpCodes.And); 
                    instruction2.labels.Add(defTestingEnd);
                    yield return instruction2;

                    //jump if true
                    yield return new CodeInstruction(OpCodes.Brtrue, postSetDrafted);

                    insertedCode = true;
                }

                //if we've done all the above
                //but haven't yet reached the set_Drafted instruction
                //just watch for it
                else if (!foundSetDrafted)
                {
                    if (instruction.Calls(typeof(Pawn_DraftController).GetProperty(nameof(Pawn_DraftController.Drafted)).GetSetMethod()))
                    {
                        foundSetDrafted = true;
                    }
                }

                //if we've found the set Drafted instruction but haven't yet set the final label
                //then we must be on the first instruction after set Drafted
                //and we should set the final label here
                else if (!setPostLabel)
                {
                    instruction.labels.Add(postSetDrafted);
                    setPostLabel = true;
                }

                //in every case, we want to return the original instruction after we're done doing whatever we did
                yield return instruction;

            }
            if (!(foundGetDrafted && changedPointer && insertedCode && foundSetDrafted && setPostLabel))
                Log.Error("Toddlers: Transpiler for MentalStateHandler.TryStartMentalState failed to complete all its checkpoints. Something has gone wrong with this transpiler.");
        }

        static void Prefix(MentalStateDef stateDef, MentalStateHandler __instance, out bool __state)
        {
            Pawn pawn = (Pawn)typeof(MentalStateHandler).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            //Log.Message("Firing TryStartMEntalBreak_Patch for " + pawn + ", DevelopmentalStage: " + pawn.DevelopmentalStage)
            if (pawn.DevelopmentalStage == DevelopmentalStage.Baby && pawn.Drafted && babyStateDefs.Contains(stateDef))
            {
                __state = true;
            }
            else __state = false;
        }

        static void Postfix(MentalStateHandler __instance, bool __state)
        {
            Pawn pawn = (Pawn)typeof(MentalStateHandler).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            if (__state)
            {
                pawn.drafter.Drafted = true;
            }
        }
    }

    
}