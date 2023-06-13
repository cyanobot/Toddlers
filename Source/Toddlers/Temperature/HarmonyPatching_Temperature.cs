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

    [HarmonyPatch(typeof(ChildcareUtility), nameof(ChildcareUtility.SafePlaceForBaby))]
    class SafePlaceForBaby_Patch
    {
        static bool Prefix(ref LocalTargetInfo __result, Pawn baby, Pawn hauler)
        {
            __result = BabyTemperatureUtility.SafePlaceForBaby(baby, hauler, out var _);
            return false;
        }
    }

    /*
    [HarmonyPatch(typeof(ChildcareUtility), nameof(ChildcareUtility.BabyNeedsMovingForTemperatureReasons))]
    class BabyNeedsMovingForTemperatureReasons_Patch
    {
        static bool Prefix(ref bool __result, Pawn baby, ref Region preferredRegion, IntVec3? positionOverride)
        {
            __result = BabyTemperatureUtility.BabyNeedsMoving()
        }
    }

    
    [HarmonyPatch(typeof(ChildcareUtility), nameof(ChildcareUtility.BabyNeedsMovingForTemperatureReasons))]
    class BabyNeedsMovingForTemperatureReasons_Patch
    {
        static bool Prefix(ref bool __result, Pawn baby, ref Region preferredRegion, IntVec3? positionOverride)
        {
            float f = ((!positionOverride.HasValue) ? baby.AmbientTemperature : GenTemperature.GetTemperatureForCell(positionOverride.Value, baby.MapHeld));
            if (ToddlerUtility.IsLiveToddler(baby) && baby.SafeTemperatureRange().Includes(f))
            {
                preferredRegion = null;
                __result = false;
                return false;
            }
            else return true;
        }
    }
    */

    //adds a check to the rescue job that trained animals use
    //to stop them repeatedly taking babies back to beds that are not good for temperature reasons
    [HarmonyPatch(typeof(JobGiver_RescueNearby), "TryGiveJob")]
    class RescueNearby_Patch
    {
        static bool Prefix(ref Job __result, JobGiver_RescueNearby __instance, Pawn pawn)
        {
            //Log.Message("Fired RescueNearby_Patch");
            float radius = (float)typeof(JobGiver_RescueNearby).GetField("radius", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            //Region babyMoveRegion;

            Predicate<Thing> validator = delegate (Thing t)
            {
                Pawn pawn3 = (Pawn)t;

                return pawn3.Downed && pawn3.Faction == pawn.Faction && !pawn3.InBed() && pawn.CanReserve(pawn3)
                    && !pawn3.IsForbidden(pawn) && !GenAI.EnemyIsNear(pawn3, 25f) && !pawn.ShouldBeSlaughtered()
                    //extra logic for only finding babies that need moving, similar to FindUnsafeBaby
                    && ((!(pawn3.DevelopmentalStage == DevelopmentalStage.Baby))
                        || BabyTemperatureUtility.BabyNeedsMovingByHauler(pawn3, pawn, out Region _, out BabyTemperatureUtility.BabyMoveReason _));
            };
            Pawn pawn2 = (Pawn)GenClosest.ClosestThingReachable(pawn.Position, pawn.MapHeld, ThingRequest.ForGroup(ThingRequestGroup.Pawn), PathEndMode.OnCell, TraverseParms.For(pawn), radius, validator);
            if (pawn2 == null)
            {
                __result = null;
                return false;
            }
            //Log.Message("pawn2: " + pawn2);

            Job job = null;
            if (pawn2.DevelopmentalStage == DevelopmentalStage.Baby)
            {
                LocalTargetInfo target = BabyTemperatureUtility.SafePlaceForBaby(pawn2, pawn , out var _);
                if (target != null && target.Cell != pawn2.PositionHeld)
                {
                    job = JobMaker.MakeJob(JobDefOf.BringBabyToSafety, pawn2);
                    job.count = 1;
                }
                else
                {
                    __result = null;
                    return false;
                }
            }
            //back to vanilla logic
            else
            {
                Building_Bed building_Bed = RestUtility.FindBedFor(pawn2, pawn, checkSocialProperness: false, ignoreOtherReservations: false, pawn2.GuestStatus);
                if (building_Bed == null || !pawn2.CanReserve(building_Bed))
                {
                    __result = null;
                    return false;
                }
                job = JobMaker.MakeJob(JobDefOf.Rescue, pawn2, building_Bed);
                job.count = 1;
            }
            __result = job;
            return false;
        }
    }
}
