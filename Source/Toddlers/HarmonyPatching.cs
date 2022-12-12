using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Toddlers
{
    [HarmonyPatch(typeof(Need),nameof(Need.GetTipString))]
    class NeedTipString_Patch
    {
        static string Postfix(string result,  ref Need __instance)
        {
            //not interested in needs other than Play
            if (!(__instance is Need_Play)) return result;

            Pawn pawn = (Pawn)typeof(Need).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);

            //not interested if not a toddler
            if (!ToddlerUtility.IsToddler(pawn)) return result;

            string header = (__instance.LabelCap + ": " + __instance.CurLevelPercentage.ToStringPercent()).Colorize(ColoredText.TipSectionTitleColor);
            string body = "Toddlers can entertain themselves for a while, but without regular attention they become lonely and unable to fulfil their own need for play.";
            string lonelyReport = "Loneliness: " + ToddlerUtility.GetLoneliness(pawn).ToStringPercent();


            return header + "\n" + body + "\n\n" + lonelyReport;
        }
    }

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

    [HarmonyPatch(typeof(ITab_Pawn_Gear))]
    class ITab_Pawn_Gear_Patch
    {
        public static MethodBase TargetMethod()
        {
            return typeof(ITab_Pawn_Gear).GetProperty(nameof(ITab_Pawn_Gear.IsVisible), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(false);
        }

        static bool Postfix(bool result, ITab_Pawn_Gear __instance)
        {
            if (result == true) return true;
            
            Pawn selPawnForGear = (Pawn)typeof(ITab_Pawn_Gear).GetProperty("SelPawnForGear", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            if (selPawnForGear.ageTracker.CurLifeStage == LifeStageDefOf.HumanlikeBaby)
            {
                object[] prms = new object[] { selPawnForGear };
                if (!(bool)typeof(ITab_Pawn_Gear).GetMethod("ShouldShowInventory", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, prms) && !(bool)typeof(ITab_Pawn_Gear).GetMethod("ShouldShowApparel", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, prms))
                {
                    return (bool)typeof(ITab_Pawn_Gear).GetMethod("ShouldShowEquipment", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, prms);
                }
                return true;
            }

            return result;
        }
    }

    [HarmonyPatch(typeof(Need_Play), nameof(Need_Play.NeedInterval))]
    class Play_NeedInterval_Patch
    {
        static bool Prefix(ref Need_Play __instance)
        {
            bool isFrozen = (bool)typeof(Need_Play).GetProperty("IsFrozen", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            if (!isFrozen)
            {
                Pawn pawn = (Pawn)typeof(Need_Play).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
                float factor = ToddlerUtility.IsToddler(pawn) ? Toddlers_Settings.playFallFactor_Toddler : Toddlers_Settings.playFallFactor_Baby;
                __instance.CurLevel -= Need_Play.BaseFallPerInterval * factor;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(Building_Door), nameof(Building_Door.PawnCanOpen))]
    class Door_Patch
    {
        static bool Prefix(ref bool __result, Pawn p)
        {
            if (p.ageTracker.CurLifeStage == Toddlers_DefOf.HumanlikeToddler && ToddlerUtility.IsCrawler(p))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(ChildcareUtility), nameof(ChildcareUtility.SwaddleBaby))]
    class SwaddleBaby_Patch
    {
        static bool Prefix(ref bool __result, Pawn baby)
        {
            if (baby.ageTracker.CurLifeStage == Toddlers_DefOf.HumanlikeToddler)
            {
                __result = false;
                return false;
            }
            else if (baby.DevelopmentalStage.Baby() && !baby.apparel.PsychologicallyNude)
            {
                __result = false;
                return false;
            }
            else return true;
        }
    }



    [HarmonyPatch(typeof(ChildcareUtility), nameof(ChildcareUtility.MakeBabyPlayAsLongAsToilIsActive))]
    class MakeBabyPlayAsLongAsToilIsActive_Patch
    {
        static Toil Postfix(Toil toil, TargetIndex babyIndex)
        {
            toil.AddPreTickAction(delegate
            {
                ToddlerPlayUtility.CureLoneliness((Pawn)toil.actor.jobs.curJob.GetTarget(babyIndex).Thing);
            });
            return toil;
        }
    }

    [HarmonyPatch(typeof(Pawn))]
    class WorkTags_Patch
    {
        public static MethodBase TargetMethod()
        {
            return typeof(Pawn).GetProperty(nameof(Pawn.CombinedDisabledWorkTags), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(false);
        }

        static WorkTags Postfix(WorkTags worktags, Pawn __instance)
        {
            if (ToddlerUtility.IsToddler(__instance)) worktags |= WorkTags.Violent;
            return worktags;
        }
    }

    [HarmonyPatch(typeof(JobGiver_GetRest), "TryGiveJob")]
    class GetRest_Patch
    {
        static Job Postfix(Job job, JobGiver_GetRest __instance, Pawn pawn)
        {
            if (job.targetA.Thing is Building_Bed && ToddlerUtility.IsCrawler(pawn))
            {
                if (job.targetA.Cell == pawn.Position) return job;
                job.targetA = (IntVec3)typeof(JobGiver_GetRest).GetMethod("FindGroundSleepSpotFor", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] { pawn });
            }
            return job;
        }
    }

    [HarmonyPatch(typeof(TargetingParameters),nameof(TargetingParameters.ForCarry))]
    class ForCarry_Patch
    {
        static TargetingParameters Postfix(TargetingParameters result)
        {
            result.onlyTargetIncapacitatedPawns = false;
            result.validator = delegate (TargetInfo targ)
            {
                if (!targ.HasThing) return false;
                Pawn toCarry = targ.Thing as Pawn;
                if (toCarry == null) return false;
                if (ToddlerUtility.IsLiveToddler(toCarry)) return true;
                if (!toCarry.Downed) return false;
                return true;
            };
            return result;
        }
    }

    [HarmonyPatch(typeof(JobDriver_CarryDownedPawn),"MakeNewToils")]
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
    [HarmonyPatch(typeof(FoodUtility),nameof(FoodUtility.TryFindBestFoodSourceFor_NewTemp))]
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
    [HarmonyPatch(typeof(FoodUtility),nameof(FoodUtility.MoodFromIngesting))]
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

    [HarmonyPatch(typeof(NeedsCardUtility),"DrawThoughtGroup")]
    class NeedsCardUtility_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
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
                        yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                        yield return new CodeInstruction(OpCodes.Bne_Un,targetLabel);
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

    [HarmonyPatch(typeof(Hediff_VatLearning),nameof(Hediff_VatLearning.PostTick))]
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


}
