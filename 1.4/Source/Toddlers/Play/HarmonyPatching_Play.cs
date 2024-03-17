using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using static Toddlers.ToddlerUtility;
using static Toddlers.ToddlerPlayUtility;

namespace Toddlers
{
    [HarmonyPatch(typeof(Need), nameof(Need.GetTipString))]
    class NeedTipString_Patch
    {
        static string Postfix(string result, ref Need __instance)
        {
            //not interested in needs other than Play
            if (!(__instance is Need_Play)) return result;

            Pawn pawn = (Pawn)typeof(Need).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);

            //not interested if not a toddler
            if (!IsToddler(pawn)) return result;

            string header = (__instance.LabelCap + ": " + __instance.CurLevelPercentage.ToStringPercent()).Colorize(ColoredText.TipSectionTitleColor);
            string body = "NeedTipStringPlay".Translate();
            string lonelyReport = "Loneliness".Translate() + ": " + GetLoneliness(pawn).ToStringPercent();


            return header + "\n" + body + "\n\n" + lonelyReport;
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
                float factor = IsToddler(pawn) ? Toddlers_Settings.playFallFactor_Toddler : Toddlers_Settings.playFallFactor_Baby;
                __instance.CurLevel -= Need_Play.BaseFallPerInterval * factor;
            }
            return false;
        }
    }

    //raises the threshold at which pawns will do childcare work: play with baby
    [HarmonyPatch(typeof(Need_Play))]
    class Play_IsLow_Patch
    {
        public static MethodBase TargetMethod()
        {
            return typeof(Need_Play).GetProperty(nameof(Need_Play.IsLow), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(false);
        }

        static bool Prefix(ref bool __result, Need_Play __instance, Pawn ___pawn)
        {
            if (__instance.CurLevelPercentage <= 0.4f) __result = true;
            else if (IsLiveToddler(___pawn) && GetLoneliness(___pawn) >= 0.4f && __instance.CurLevelPercentage <= 0.8f)
                __result = true;
            else __result = false;
            return false;
        }
    }


    [HarmonyPatch(typeof(ChildcareUtility), nameof(ChildcareUtility.MakeBabyPlayAsLongAsToilIsActive))]
    class MakeBabyPlayAsLongAsToilIsActive_Patch
    {
        static Toil Postfix(Toil toil, TargetIndex babyIndex)
        {
            toil.AddPreTickAction(delegate
            {
                CureLoneliness((Pawn)toil.actor.jobs.curJob.GetTarget(babyIndex).Thing);
            });
            return toil;
        }
    }

    //overwrite play job given to baby while being played with
    //with our own
    [HarmonyPatch(typeof(ChildcareUtility),nameof(ChildcareUtility.MakeBabyPlayJob))]
    class MakeBabyPlayJob_Patch
    {
        static bool Prefix(ref Job __result, Pawn feeder)
        {
            Job job = JobMaker.MakeJob(Toddlers_DefOf.BePlayedWith, feeder);
            job.count = 1;
            __result = job;
            return false;
        }
    }

    //don't put babies/toddlers on the floor to play with them if they're downed for medical reasons
    [HarmonyPatch()]
    class BabyPlayGiver_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return typeof(BabyPlayGiver_PlayStatic).GetMethod(nameof(BabyPlayGiver_PlayStatic.CanDo));
            yield return typeof(BabyPlayGiver_PlayToys).GetMethod(nameof(BabyPlayGiver_PlayToys.CanDo));
        }

        static void Postfix(ref bool __result, Pawn __1)
        {
            if (__result && __1.Downed && HealthAIUtility.ShouldSeekMedicalRest(__1))
            {
                __result = false;
            }
        }
    }

    //small tweak so that the adult and baby more often successfully end up in adjacent squares
    //rather than several squares apart
    //or with the adult standing on the baby
    [HarmonyPatch(typeof(JobDriver_PlayStatic), "Play")]
    class PlayStatic_Patch
    {
        static IEnumerable<Toil> Postfix(IEnumerable<Toil> result, Pawn ___pawn, JobDriver __instance)
        {
            //second Goto toil to shift adult off the baby's square if they're already standing on it
            //or make adult close any distance that baby might have covered since adult started trying to reach them

            Toil toil = ToilMaker.MakeToil("DelayedGoTo");
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                Pawn baby = (Pawn)toil.actor.jobs.curJob.GetTarget(TargetIndex.A).Thing;

                if (RCellFinder.TryFindGoodAdjacentSpotToTouch(___pawn, baby, out IntVec3 dest))
                {
                    actor.pather.StartPath(dest, PathEndMode.OnCell);
                }
                else
                {
                    actor.pather.StartPath(baby, PathEndMode.ClosestTouch);
                }
                    
            };
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            toil.AddPreInitAction(delegate
            {
                Pawn baby = (Pawn)toil.actor.jobs.curJob.GetTarget(TargetIndex.A).Thing;
                Job babyPlay = ChildcareUtility.MakeBabyPlayJob(toil.actor);
                //force baby to stay still for adult to finish getting close
                baby.jobs.StartJob(babyPlay, JobCondition.InterruptForced);
            });

            return result.Prepend(toil);
        }
    }
}
