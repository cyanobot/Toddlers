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

    [HarmonyPatch(typeof(ChildcareUtility), nameof(ChildcareUtility.SwaddleBaby))]
    class SwaddleBaby_Patch
    {
        static bool Prefix(ref bool __result, Pawn baby)
        {
            if (ToddlerUtility.IsToddler(baby))
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


    [HarmonyPatch(typeof(StrippableUtility), nameof(StrippableUtility.CanBeStrippedByColony))]
    class CanBeStrippedByColony_Patch
    {
        static bool Postfix(bool result, Thing th)
        {
            if (th is Pawn pawn && ToddlerUtility.IsToddler(pawn)) return true;
            else return result;
        }
    }


    [HarmonyPatch(typeof(TargetingParameters), nameof(TargetingParameters.ForStrip))]
    class ForStrip_Patch
    {
        static void Postfix(ref TargetingParameters __result, Pawn p)
        {
            if (!ToddlerUtility.CanDressSelf(p))
            {
                __result.canTargetPawns = false;
                __result.canTargetItems = false;
            }
        }
    }

    [HarmonyPatch(typeof(JobGiver_OptimizeApparel),"TryGiveJob")]
    class OptimizeApparel_Patch
    {
        static void Postfix(Job __result, Pawn pawn)
        {
            if (ToddlerUtility.IsToddler(pawn) && __result != null)
                __result.haulDroppedApparel = false;
        }
    }
}
