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
            if (!ToddlerUtility.IsToddler(pawn)) return result;

            string header = (__instance.LabelCap + ": " + __instance.CurLevelPercentage.ToStringPercent()).Colorize(ColoredText.TipSectionTitleColor);
            string body = "Toddlers can entertain themselves for a while, but without regular attention they become lonely and unable to fulfil their own need for play.";
            string lonelyReport = "Loneliness: " + ToddlerUtility.GetLoneliness(pawn).ToStringPercent();


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
                float factor = ToddlerUtility.IsToddler(pawn) ? Toddlers_Settings.playFallFactor_Toddler : Toddlers_Settings.playFallFactor_Baby;
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
            else if (ToddlerUtility.IsLiveToddler(___pawn) && ToddlerUtility.GetLoneliness(___pawn) >= 0.4f && __instance.CurLevelPercentage <= 0.8f)
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
                ToddlerPlayUtility.CureLoneliness((Pawn)toil.actor.jobs.curJob.GetTarget(babyIndex).Thing);
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

    //don't stand on the baby you're playing with
    [HarmonyPatch(typeof(JobDriver_BabyPlay),"CreateStartingCondition")]
    class CreateStartingCondition_Patch
    {
        static IEnumerable<Toil> Postfix(IEnumerable<Toil> __result, JobDriver_BabyPlay __instance)
        {
            JobDriver_BabyPlay.StartingConditions startingCondition = (JobDriver_BabyPlay.StartingConditions)typeof(JobDriver_BabyPlay)
                .GetProperty("StartingCondition", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
            if (startingCondition == JobDriver_BabyPlay.StartingConditions.GotoBaby)
            {
                RCellFinder.TryFindGoodAdjacentSpotToTouch(__instance.pawn, __instance.job.targetA.Thing, out IntVec3 adjacentSpot);
                yield return Toils_Goto.GotoCell(adjacentSpot, PathEndMode.OnCell);
            }
            else
            {
                foreach (Toil toil in __result) yield return toil;
            }
        }
    }
}
