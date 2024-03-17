using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using static Toddlers.ToddlerUtility;

namespace Toddlers
{

    [HarmonyPatch(typeof(Pawn_MindState))]
    class IsIdle_Patch
    {
        public static MethodBase TargetMethod()
        {
            return typeof(Pawn_MindState).GetProperty(nameof(Pawn_MindState.IsIdle), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(false);
        }
        static bool Postfix(bool result, Pawn ___pawn)
        {
            if (ToddlerUtility.IsToddler(___pawn)) return false;
            return result;
        }
    }


    [HarmonyPatch(typeof(Building_Door), nameof(Building_Door.PawnCanOpen))]
    class Door_Patch
    {
        static bool Prefix(ref bool __result, Pawn p)
        {
            //Log.Message("Firing Door_Patch");
            if (ToddlerUtility.IsCrawler(p))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn_StoryTracker))]
    class WorkTags_Patch
    {
        public static MethodBase TargetMethod()
        {
            return typeof(Pawn_StoryTracker).GetProperty(nameof(Pawn_StoryTracker.DisabledWorkTagsBackstoryTraitsAndGenes), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(false);
        }

        static WorkTags Postfix(WorkTags worktags, Pawn ___pawn)
        {
            //AllWork catches most things, and should catch most modded worktypes as well
            //violent stops eg equipping weapons, manning mortars
            if (ToddlerUtility.IsToddler(___pawn))
            {
                worktags |= WorkTags.AllWork | WorkTags.Violent;
                //Log.Message("Fired DisabledWorkTagsBackstoryTraitsAndGenes for pawn: " + ___pawn + ", worktags: " + worktags.ToString());
            }               
            return worktags;
        }
    }

    
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

    [HarmonyPatch(typeof(JobGiver_GetRest), "TryGiveJob")]
    class GetRest_Patch
    {
        static Job Postfix(Job job, JobGiver_GetRest __instance, Pawn pawn)
        {
            if (ToddlerUtility.IsCrawler(pawn) && job.targetA.Thing != null && job.targetA.Thing is Building_Bed)
            {
                if (job.targetA.Cell == pawn.Position) return job;
                job.targetA = (IntVec3)typeof(JobGiver_GetRest).GetMethod("FindGroundSleepSpotFor", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] { pawn });
            }
            return job;
        }
    }

    //toddlers can't carry people, for any reason
    [HarmonyPatch]
    class TargetingParameters_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return typeof(TargetingParameters).GetMethod(nameof(TargetingParameters.ForArrest));
            yield return typeof(TargetingParameters).GetMethod(nameof(TargetingParameters.ForCarryDeathresterToBed));
            yield return typeof(TargetingParameters).GetMethod(nameof(TargetingParameters.ForCarryToBiosculpterPod));
            yield return typeof(TargetingParameters).GetMethod(nameof(TargetingParameters.ForDraftedCarryBed));
            yield return typeof(TargetingParameters).GetMethod(nameof(TargetingParameters.ForDraftedCarryCryptosleepCasket));
            yield return typeof(TargetingParameters).GetMethod(nameof(TargetingParameters.ForRescue));
            yield return typeof(TargetingParameters).GetMethod(nameof(TargetingParameters.ForShuttle));
        }

        static void Postfix(object[] __args, MethodBase __originalMethod, ref TargetingParameters __result)
        {
            Pawn p;
            if (__originalMethod == typeof(TargetingParameters).GetMethod(nameof(TargetingParameters.ForDraftedCarryBed))) 
            {
                p = (Pawn)__args[1];
            }
            else
            {
                p = (Pawn)__args[0];
            }

            if (ToddlerUtility.IsToddler(p))
            {
                __result.canTargetPawns = false;
            }
            else if (__originalMethod == typeof(TargetingParameters).GetMethod(nameof(TargetingParameters.ForRescue)))
            {
                __result.onlyTargetIncapacitatedPawns = false;
                __result.validator = delegate (TargetInfo targ)
                {
                    if (!targ.HasThing || !(targ.Thing is Pawn pawn))
                    {
                        return false;
                    }
                    return pawn.Downed || ToddlerUtility.IsLiveToddler(pawn);
                };
            }
        }
    }

    [HarmonyPatch(typeof(TargetingParameters), nameof(TargetingParameters.ForCarry))]
    class ForCarry_Patch
    {
        static TargetingParameters Postfix(TargetingParameters result, Pawn p)
        {
            result.onlyTargetIncapacitatedPawns = false;
            result.validator = delegate (TargetInfo targ)
            {
                if (!targ.HasThing) return false;                           //nothing there
                if (ToddlerUtility.IsLiveToddler(p)) return false;          //toddlers can't carry anyone
                Pawn toCarry = targ.Thing as Pawn; 
                if (toCarry == null) return false;                          //no pawn
                if (toCarry == p) return false;                             //can't carry self
                if (ToddlerUtility.IsLiveToddler(toCarry)) return true;     //can carry toddlers
                if (!toCarry.Downed) return false;                          //can't carry non-downed non-toddlers
                return true;
            };
            return result;
        }
    }

    [HarmonyPatch(typeof(JobDriver_CarryDownedPawn), "MakeNewToils")]
    class CarryDownedPawn_Patch
    {
        static IEnumerable<Toil> Postfix(IEnumerable<Toil> result, JobDriver_CarryDownedPawn __instance)
        {
            Pawn toCarry = (Pawn)__instance.job.GetTarget(TargetIndex.A).Thing;
            if (!toCarry.DestroyedOrNull() && ToddlerUtility.IsLiveToddler(toCarry))
            {
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.A)
                    .FailOnSomeonePhysicallyInteracting(TargetIndex.A);
                yield return Toils_Haul.StartCarryThing(TargetIndex.A);
            }
            else
            {
                foreach (Toil toil in result) yield return toil;
            }
        }
    }


    //without this toddlers won't eat baby food because it's desperateonly
    [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.TryFindBestFoodSourceFor_NewTemp))]
    class BestFoodSource_Patch
    {
        static void Prefix(Pawn eater, ref FoodPreferability minPrefOverride)
        {
            if (ToddlerUtility.IsToddler(eater)) minPrefOverride = FoodPreferability.DesperateOnly;
        }
    }

    //makes young toddlers eat on the floor
    [HarmonyPatch(typeof(Toils_Ingest), nameof(Toils_Ingest.CarryIngestibleToChewSpot))]
    class CarryIngestibleToChewSpot_Patch
    {
        static Toil Postfix(Toil result, Pawn pawn, TargetIndex ingestibleInd)
        {
            if (!ToddlerUtility.IsLiveToddler(pawn) || !ToddlerUtility.EatsOnFloor(pawn)) return result;

            result.initAction = delegate
            {
                Pawn actor = result.actor;
                IntVec3 cell = IntVec3.Invalid;
                Thing food = actor.CurJob.GetTarget(ingestibleInd).Thing;

                cell = RCellFinder.SpotToChewStandingNear(actor, actor.CurJob.GetTarget(ingestibleInd).Thing, (IntVec3 c) => actor.CanReserveSittableOrSpot(c) && c.GetDangerFor(actor, actor.Map) == Danger.None);
                actor.ReserveSittableOrSpot(cell, actor.CurJob);
                actor.Map.pawnDestinationReservationManager.Reserve(actor, actor.CurJob, cell);
                actor.pather.StartPath(cell, PathEndMode.OnCell);
            };
            return result;
        }
    }

    //stops (Disliked food) from showing for toddlers considering baby food
    [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.MoodFromIngesting))]
    class MoodFromIngesting_Patch
    {
        static bool Prefix(ref float __result, Pawn ingester)
        {
            if (ingester.DevelopmentalStage == DevelopmentalStage.Baby)
            {
                __result = 0f;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn_DraftController))]
    class ShowDraftGizmo_Patch
    {
        public static MethodBase TargetMethod()
        {
            return typeof(Pawn_DraftController).GetProperty(nameof(Pawn_DraftController.ShowDraftGizmo), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(false);
        }

        static bool Postfix(bool result, Pawn_DraftController __instance, Pawn ___pawn)
        {
            if (!__instance.Drafted && ToddlerUtility.IsToddler(___pawn) && !Toddlers_Settings.canDraftToddlers)
            {
                return false;
            }
            return result;
        }
    }

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

    //don't  show mental break  warnings for toddlers
    //or for that matter any other pawn who can't do random mental breaks
    [HarmonyPatch(typeof(MentalBreaker))]
    class BreakExtremeIsImminent_Patch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return typeof(MentalBreaker).GetProperty(nameof(MentalBreaker.BreakExtremeIsImminent), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(false);
            yield return typeof(MentalBreaker).GetProperty(nameof(MentalBreaker.BreakMajorIsImminent), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(false);
            yield return typeof(MentalBreaker).GetProperty(nameof(MentalBreaker.BreakMinorIsImminent), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(false);
            yield return typeof(MentalBreaker).GetProperty(nameof(MentalBreaker.BreakExtremeIsApproaching), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(false);
        }

        static bool Postfix(bool result, Pawn ___pawn)
        {
            if (result && !___pawn.mindState.mentalBreaker.CanDoRandomMentalBreaks)
                return false;
            return result;
        }
    }

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

    [HarmonyPatch(typeof(Hediff_VatLearning), nameof(Hediff_VatLearning.PostTick))]
    class VatLearning_Patch
    {
        static void Postfix(Pawn ___pawn)
        {
            Hediff_ToddlerLearning learningHediff_walk = (Hediff_ToddlerLearning)___pawn.health.hediffSet.GetFirstHediffOfDef(Toddlers_DefOf.LearningToWalk);
            Hediff_ToddlerLearning learningHediff_manipulation = (Hediff_ToddlerLearning)___pawn.health.hediffSet.GetFirstHediffOfDef(Toddlers_DefOf.LearningManipulation);

            //0.6 = factor so that the growth vat is less efficient than learning by doing
            float factor = (float)Building_GrowthVat.AgeTicksPerTickInGrowthVat * 0.6f;

            if (learningHediff_walk != null)
            {
                learningHediff_walk.InnerTick(factor);
                if (learningHediff_walk.Severity >= 1f) ___pawn.health.RemoveHediff(learningHediff_walk);
            }
            if (learningHediff_manipulation != null)
            {
                learningHediff_manipulation.InnerTick(factor);
                if (learningHediff_manipulation.Severity >= 1f) ___pawn.health.RemoveHediff(learningHediff_manipulation);
            }
        }
    }

    //Can take toddlers on caravans even if they are in a mental state
    [HarmonyPatch(typeof(CaravanFormingUtility), nameof(CaravanFormingUtility.AllSendablePawns))]
    class AllSendablePawns_Patch
    {
        static List<Pawn> Postfix(List<Pawn> result, Map map, bool allowEvenIfInMentalState)
        {
            //if allowEvenIfInMentalState was true, we don't need to meddle
            if (allowEvenIfInMentalState) return result;

            List<Pawn> allPawnsSpawned = map.mapPawns.AllPawnsSpawned;
            foreach (Pawn pawn in allPawnsSpawned)
            {
                if (pawn.InMentalState && ToddlerUtility.IsLiveToddler(pawn))
                    result.Add(pawn);
            }
            return result;
        }
    }

    //babies crying or giggling should not interrupt lord jobs like rituals or caravan formation
    [HarmonyPatch(typeof(Trigger_MentalState), nameof(Trigger_MentalState.ActivateOn))]
    class Trigger_MentalState_Patch
    {
        static bool Prefix(ref bool __result, Lord lord, TriggerSignal signal)
        {
            if (signal.type == TriggerSignalType.Tick)
            {
                for (int i = 0; i < lord.ownedPawns.Count; i++)
                {
                    if (lord.ownedPawns[i].InMentalState && lord.ownedPawns[i].DevelopmentalStage != DevelopmentalStage.Baby)
                    {
                        __result = true;
                        return false;
                    }
                }
            }
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Trigger_NoMentalState), nameof(Trigger_NoMentalState.ActivateOn))]
    class Trigger_NoMentalState_Patch
    {
        static bool Prefix(ref bool __result, Lord lord, TriggerSignal signal)
        {
            if (signal.type == TriggerSignalType.Tick)
            {
                for (int i = 0; i < lord.ownedPawns.Count; i++)
                {
                    if (lord.ownedPawns[i].InMentalState && lord.ownedPawns[i].DevelopmentalStage != DevelopmentalStage.Baby)
                    {
                        __result = false;
                        return false;
                    }
                }
                __result = true;
                return false;
            }
            __result = false;
            return false;
        }
    }

    //crying/giggling toddlers still count as PlayerControlled
    //which allows them to eg be given orders, drafted
    [HarmonyPatch(typeof(Pawn))]
    class IsColonistPlayerControlled_Patch
    {
        public static MethodBase TargetMethod()
        {
            return typeof(Pawn).GetProperty(nameof(Pawn.IsColonistPlayerControlled), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(false);
        }

        static bool Postfix(bool result, Pawn __instance)
        {
            if (result == false && __instance.Spawned && __instance.IsColonist
                && (__instance.HostFaction == null || __instance.IsSlave)
                && ToddlerUtility.IsLiveToddler(__instance))
            {
                result = true;
            }
            return result;
        }
    }

    //crying/giggling toddlers still respect forbiddances
    [HarmonyPatch(typeof(ForbidUtility),nameof(ForbidUtility.CaresAboutForbidden))]
    class CaresAboutForbidden_Patch
    {
        static bool Postfix(bool result, Pawn pawn, bool cellTarget)
        {
            //only care about toddlers in mental states
            if (ToddlerUtility.IsToddler(pawn))
            {
                if (pawn.HostFaction != null 
                    && (pawn.HostFaction != Faction.OfPlayer || !pawn.Spawned 
                    || pawn.Map.IsPlayerHome 
                    || (pawn.GetRoom() != null && pawn.GetRoom().IsPrisonCell) 
                    || (pawn.IsPrisoner && !pawn.guest.PrisonerIsSecure)))
                {
                    return false;
                }
                if (SlaveRebellionUtility.IsRebelling(pawn))
                {
                    return false;
                }
                if (cellTarget && ThinkNode_ConditionalShouldFollowMaster.ShouldFollowMaster(pawn))
                {
                    return false;
                }
                return true;
            }
            return result;
        }
    }

    //when generating toddlers, give age-appropriate hediffs
    [HarmonyPatch(typeof(PawnGenerator),nameof(PawnGenerator.GeneratePawn), new Type[] { typeof(PawnGenerationRequest) })]
    class GeneratePawn_Patch
    {
        static void Postfix(ref Pawn __result)
        {
            if (ToddlerUtility.IsToddler(__result))
            {
                ToddlerUtility.ResetHediffsForAge(__result);
            }
        }
    }

    //toddlers should be carried on caravans if possible
    [HarmonyPatch(typeof(Caravan_CarryTracker),"WantsToBeCarried")]
    class WantsToBeCarried_Patch
    {
        static bool Prefix(ref bool __result, Pawn p)
        {
            if (ToddlerUtility.IsToddler(p))
            {
                __result = true;
                return false;
            }

            return true;
        }
    }

    //treat toddlers (in some ways) as downed pawns when trying to form caravans
    [HarmonyPatch]
    class PrepareCaravan_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(LordToil_PrepareCaravan_GatherDownedPawns), nameof(LordToil_PrepareCaravan_GatherDownedPawns.UpdateAllDuties));
            yield return AccessTools.Method(typeof(LordToil_PrepareCaravan_GatherDownedPawns), "CheckAllPawnsArrived");
            yield return AccessTools.Method(typeof(JobGiver_PrepareCaravan_GatherDownedPawns), "FindDownedPawn");
            yield return AccessTools.Method(typeof(LordToil_PrepareCaravan_RopeAnimals), nameof(LordToil_PrepareCaravan_GatherDownedPawns.UpdateAllDuties));
            yield return AccessTools.Method(typeof(LordToil_PrepareCaravan_GatherItems), nameof(LordToil_PrepareCaravan_GatherDownedPawns.UpdateAllDuties));
            yield return AccessTools.Method(typeof(LordToil_PrepareCaravan_GatherItems), nameof(LordToil_PrepareCaravan_GatherDownedPawns.LordToilTick));
        }

        static List<Pawn> FindDownedAndToddlers(LordJob_FormAndSendCaravan lordJob)
        {
            return lordJob.downedPawns.Concat(lordJob.lord.ownedPawns.Where(x => ToddlerUtility.IsToddler(x))).ToList();
        }

        static MethodInfo m_IsColonist = AccessTools.Property(typeof(Pawn), nameof(Pawn.IsColonist)).GetGetMethod();
        static MethodInfo m_Downed = AccessTools.Property(typeof(Pawn), nameof(Pawn.Downed)).GetGetMethod();
        static MethodInfo m_IsToddler = AccessTools.Method(typeof(ToddlerUtility), nameof(ToddlerUtility.IsToddler));
        static MethodInfo m_FindDownedAndToddlers = AccessTools.Method(typeof(PrepareCaravan_Patch), nameof(PrepareCaravan_Patch.FindDownedAndToddlers));

        static FieldInfo f_lord = AccessTools.Field(typeof(LordToil), nameof(LordToil.lord));
        static FieldInfo f_downedPawns = AccessTools.Field(typeof(LordJob_FormAndSendCaravan), nameof(LordJob_FormAndSendCaravan.downedPawns));

        /*
        static void Prefix(object[] __args, MethodBase __originalMethod)
        {
            Log.Message(__originalMethod.Name + " firing, args: " + __args.ToStringSafeEnumerable());
        }
        */
        
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction prevInstruction = null;
            foreach (var instruction in instructions)
            { 
                //convert all instances of
                //pawn.IsColonist
                //to
                //pawn.IsColonist && !ToddlerUtility.IsToddler(pawn)
                if (instruction.Calls(m_IsColonist))
                {
                    yield return instruction;
                    yield return prevInstruction;
                    yield return new CodeInstruction(OpCodes.Callvirt, m_IsToddler);
                    yield return new CodeInstruction(OpCodes.Not);
                    yield return new CodeInstruction(OpCodes.And);
                }
                //convert
                //pawn.Downed
                //to
                //pawn.Downed || ToddlerUtility.IsToddler(pawn)
                else if (instruction.Calls(m_Downed))
                {
                    yield return instruction;
                    yield return prevInstruction;
                    yield return new CodeInstruction(OpCodes.Callvirt, m_IsToddler);
                    yield return new CodeInstruction(OpCodes.Or);
                }
                //convert
                //downedPawns = ((LordJob_FormAndSendCaravan)lord.LordJob).downedPawns;
                //to
                //downedPawns = ((LordJob_FormAndSendCaravan)lord.LordJob).downedPawns.Concat(FindToddlers(lord)).ToList();
                else if (instruction.LoadsField(f_downedPawns))
                {
                    yield return new CodeInstruction(OpCodes.Call, m_FindDownedAndToddlers);
                }
                else
                {
                    yield return instruction;
                }

                prevInstruction = instruction;
            }
        }
        
    }

    //accept carried toddlers as ready to  leave on caravans
    [HarmonyPatch(typeof(GatherAnimalsAndSlavesForCaravanUtility),nameof(GatherAnimalsAndSlavesForCaravanUtility.CheckArrived))]
    class CheckArrived_Patch
    {
        static MethodInfo m_Spawned = AccessTools.Property(typeof(Thing), nameof(Thing.Spawned)).GetGetMethod();
        static MethodInfo m_Position = AccessTools.Property(typeof(Thing), nameof(Thing.Position)).GetGetMethod();
        static MethodInfo m_PositionHeld = AccessTools.Property(typeof(Thing), nameof(Thing.PositionHeld)).GetGetMethod();
        static MethodInfo m_IsToddler = AccessTools.Method(typeof(ToddlerUtility), nameof(ToddlerUtility.IsToddler));
        static MethodInfo m_CanReach = AccessTools.Method(typeof(ReachabilityUtility), nameof(ReachabilityUtility.CanReach));

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

    //toddlers should never count as threats
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.ThreatDisabled))]
    class ThreatDisabled_Patch
    {
        static bool Postfix(bool result, Pawn __instance)
        {
            if (result) return true;
            if (IsToddler(__instance)) return true;
            return false;
        }
    }

    //raiders should ignore toddlers
    /*
    [HarmonyPatch(typeof(AttackTargetFinder), "ShouldIgnoreNoncombatant")]
    class ShouldIgnoreNoncombatant_Patch
    {
        static bool Postfix(bool result, Thing searcherThing, IAttackTarget t)
        {
            if (result == false && t is Pawn pawn && ToddlerUtility.IsToddler(pawn))
            {
                return true;
            }

            return result;
        }
    }
    */

    //can always kidnap toddlers
    [HarmonyPatch(typeof(KidnapAIUtility),nameof(KidnapAIUtility.TryFindGoodKidnapVictim))]
    class TryFindGoodKidnapVictim_Patch
    {
        //very slightly different from vanilla
        private static bool KidnapValidator(Thing t, Pawn kidnapper, List<Thing> disallowed = null)
        {
            Pawn pawn = t as Pawn;
            if (!pawn.RaceProps.Humanlike)
            {
                return false;
            }
            if (!pawn.Downed && !ToddlerUtility.IsToddler(pawn))
            {
                return false;
            }
            if (pawn.Faction != Faction.OfPlayer)
            {
                return false;
            }
            if (!pawn.Faction.HostileTo(kidnapper.Faction))
            {
                return false;
            }
            if (!kidnapper.CanReserve(pawn))
            {
                return false;
            }
            return (disallowed == null || !disallowed.Contains(pawn)) ? true : false;
        }

        //same as vanilla except uses adjusted validator above
        static bool Prefix(ref bool __result, Pawn kidnapper, float maxDist, out Pawn victim, List<Thing> disallowed = null)
        {
            if (!kidnapper.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) || !kidnapper.Map.reachability.CanReachMapEdge(kidnapper.Position, TraverseParms.For(kidnapper, Danger.Some)))
            {
                victim = null;
                __result = false;
                return false;
            }

            victim = (Pawn)GenClosest.ClosestThingReachable(kidnapper.Position, kidnapper.Map, 
                ThingRequest.ForGroup(ThingRequestGroup.Pawn), PathEndMode.OnCell, 
                TraverseParms.For(TraverseMode.NoPassClosedDoors, Danger.Some), maxDist, 
                t => KidnapValidator(t,kidnapper,disallowed));
            __result = victim != null;

            return false;
        }
    }

    //JobGiver_Kidnap fails if trying to target a non-downed awake pawn
    //new version KidnapToddler does not
    [HarmonyPatch(typeof(JobGiver_Kidnap),"TryGiveJob")]
    class JobGiver_Kidnap_Patch
    {
        static Job Postfix(Job oldjob, Pawn pawn)
        {
            if (oldjob != null)
            {
                Pawn victim = (Pawn)oldjob.targetA;

                if (ToddlerUtility.IsToddler(victim))
                {
                    Job newjob = JobMaker.MakeJob(Toddlers_DefOf.KidnapToddler);
                    newjob.targetA = victim;
                    newjob.targetB = oldjob.targetB;
                    newjob.count = 1;

                    return newjob;
                }
            }

            return oldjob;
        }
    }

    //replaces the Pawns property for the schedule window
    //to allow babies and toddlers to appear in it
    [HarmonyPatch(typeof(MainTabWindow_Schedule),"Pawns",MethodType.Getter)]
    class MainTabWindow_Schedule_Patch
    {
        static bool Prefix(ref IEnumerable<Pawn> __result)
        {
            __result = Find.CurrentMap.mapPawns.FreeColonists;

            return false;
        }
    }

    //replaces the Pawns property for the assign window
    //to allow both babies and toddlers to appear in it
    [HarmonyPatch(typeof(MainTabWindow_Assign), "Pawns", MethodType.Getter)]
    class MainTabWindow_Assign_Patch
    {
        static bool Prefix(ref IEnumerable<Pawn> __result)
        {
            __result = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists;

            return false;
        }
    }

    //
    [HarmonyPatch(typeof(ChildcareUtility),nameof(ChildcareUtility.WantsSuckle))]
    class WantsSuckle_Patch
    {
        static bool Postfix(bool result, Pawn baby, ref ChildcareUtility.BreastfeedFailReason? reason)
        {
            if (!result) return false;
            if (!IsToddler(baby)) return result;
            if (!Toddlers_Settings.feedCapableToddlers && CanFeedSelf(baby) && FoodUtility.TryFindBestFoodSourceFor_NewTemp(baby, baby, false, out var _, out var _)) return false;
            return result;
        }

    }
}