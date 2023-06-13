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

}
