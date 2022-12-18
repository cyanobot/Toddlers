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

namespace Toddlers
{
    class BabyTemperatureUtility
    {
        public static float TemperatureAtBed(Building_Bed bed, Map map)
        {
            return GenTemperature.GetTemperatureForCell(bed?.Position ?? IntVec3.Invalid, map);
        }

        //returns target1 if equivalent
        public static LocalTargetInfo BestTemperatureForPawn(LocalTargetInfo target1, LocalTargetInfo target2, Pawn pawn)
        {
            IntVec3 c1 = target1.Cell;
            IntVec3 c2 = target2.Cell;
            float temp1;
            float temp2;

            if (!GenTemperature.TryGetTemperatureForCell(c1, pawn.MapHeld, out temp1) || !GenTemperature.TryGetTemperatureForCell(c2, pawn.MapHeld, out temp2)) return LocalTargetInfo.Invalid;

            FloatRange comfRange = pawn.ComfortableTemperatureRange();

            if (comfRange.Includes(temp1)) return target1;
            else if (comfRange.Includes(temp2)) return target2;

            FloatRange safeRange = pawn.SafeTemperatureRange();

            if (safeRange.Includes(temp1) && safeRange.Includes(temp2))
            {
                float distFromComf1 = Math.Abs(temp1 >= comfRange.TrueMax ? temp1 - comfRange.TrueMax : temp1 - comfRange.TrueMin);
                float distFromComf2 = Math.Abs(temp2 >= comfRange.TrueMax ? temp2 - comfRange.TrueMax : temp2 - comfRange.TrueMin);

                return distFromComf1 <= distFromComf2 ? target1 : target2;
            }
            else if (safeRange.Includes(temp1)) return target1;
            else if (safeRange.Includes(temp2)) return target2;

            float distFromSafe1 = Math.Abs(temp1 >= safeRange.TrueMax ? temp1 - safeRange.TrueMax : temp1 - safeRange.TrueMin);
            float distFromSafe2 = Math.Abs(temp2 >= safeRange.TrueMax ? temp2 - safeRange.TrueMax : temp2 - safeRange.TrueMin);

            return distFromSafe1 <= distFromSafe2 ? target1 : target2;

        }

        public static LocalTargetInfo SafePlaceForBaby(Pawn baby, Pawn hauler)
        {
            //Log.Message("Fired SafePlaceForBaby");
            if (!ChildcareUtility.CanSuckle(baby, out var _) || !ChildcareUtility.CanHaulBabyNow(hauler, baby, false, out var _))
                return LocalTargetInfo.Invalid;

            Building_Bed bed = baby.CurrentBed() ?? RestUtility.FindBedFor(baby, hauler, checkSocialProperness: true, ignoreOtherReservations: false, baby.GuestStatus);
            //Log.Message("bed: " + bed);

            IntVec3 currentCell = baby.PositionHeld;
            LocalTargetInfo target = LocalTargetInfo.Invalid;

            //sick babies and toddlers should both be taken to medical beds if possible
            //requiring safe temperature but not necessarily comfortable
            if (HealthAIUtility.ShouldSeekMedicalRest(baby))
            {
                //Log.Message("Baby " + baby + " is sick");

                //if the best available bed is at a safe temperature, pick that
                if (bed != null && baby.SafeTemperatureRange().Includes(TemperatureAtBed(bed, baby.MapHeld)))
                    return bed;

                //if we didn't find a good bed, look for just a spot that's a good temperature 
                target = ChildcareUtility.BabyNeedsMovingForTemperatureReasons(baby, hauler, out var preferredRegion)
                    ? ((LocalTargetInfo)RCellFinder.SpotToStandDuringJob(hauler, null, preferredRegion))
                    : ((!baby.Spawned)
                        ? ((LocalTargetInfo)RCellFinder.SpotToStandDuringJob(hauler, null, baby.GetRegionHeld()))
                        : ((LocalTargetInfo)baby.Position));

                return target;
            }

            //more relaxed logic for healthy toddlers
            if (!baby.Downed)
            {
                //Log.Message("Baby " + baby + " is a toddler");

                //if the toddler is in a non-optimal temperature, try to pick a spot that would be better
                if (ChildcareUtility.BabyNeedsMovingForTemperatureReasons(baby, hauler, out var preferredRegion))
                {
                    //Log.Message("Baby " + baby + " needs moving for temperature reasons");

                    //if their bed would be comfortable, take them there
                    if (bed != null && baby.ComfortableTemperatureRange().Includes(TemperatureAtBed(bed, baby.MapHeld)))
                        return bed;
                    //otherwise pick a spot in the region indicated by BabyNeedsMovingForTemperatureReasons
                    else target = (LocalTargetInfo)RCellFinder.SpotToStandDuringJob(hauler, null, preferredRegion);
                    //if they have a bed check which of the bed and the target spot is the better temperature, preferring the bed if they're equivalent
                    if (bed != null) return BestTemperatureForPawn(bed, target, baby);
                    return target;
                }

                //if the toddler is tired and they have a bed to go to, consider moving them to it
                else if (bed != null && baby.needs.rest.CurLevelPercentage < 0.28)
                {
                    //Log.Message("Baby " + baby + " is tired");

                    //if the bed is a comfortable temperature, just do it
                    if (baby.ComfortableTemperatureRange().Includes(TemperatureAtBed(bed, baby.MapHeld))) return bed;

                    //otherwise if the bed is a better temperature than where the toddller is now, do it
                    return BestTemperatureForPawn(bed, currentCell, baby);
                }

                //otherwise if the toddler isn't spawned (ie being carried), try to pick a spot near where they are currently (ie just put them down)
                else if (!baby.Spawned) return RCellFinder.SpotToStandDuringJob(hauler, null, baby.GetRegionHeld());

                //if none of the above, just leave them where they are
                else return currentCell;
            }

            //mostly-vanilla logic for babies that prefers taking them to bed even if they aren't tired
            else
            {
                //if the bed's a comfortable temperature, take them straight there
                if (baby.ComfortableTemperatureRange().Includes(TemperatureAtBed(bed, baby.MapHeld)))
                {
                    //Log.Message("bed is fine");
                    return bed;
                }

                //if they need moving for temperature reasons
                if (ChildcareUtility.BabyNeedsMovingForTemperatureReasons(baby, hauler, out var preferredRegion))
                {
                    //Log.Message("needs moving for temperature reasons");
                    //pick a spot in the region indicated by BabyNeedsMovingForTemperatureReasons
                    target = (LocalTargetInfo)RCellFinder.SpotToStandDuringJob(hauler, null, preferredRegion);
                    //if they have a bed check which of the bed and the target spot is the better temperature, preferring the bed if they're equivalent
                    //Log.Message("bed temperature: " + GenTemperature.GetTemperatureForCell(bed.Position, hauler.MapHeld));
                    //Log.Message("target: " + target.Cell.ToString() + ", temperature: " + GenTemperature.GetTemperatureForCell(target.Cell, hauler.MapHeld));
                    if (bed != null) return BestTemperatureForPawn(bed, target, baby);
                    return target;
                }
                //otherwise
                else
                {
                    //take them to their bed so long as it's not a worse temperature than where they are now
                    if (bed != null) return BestTemperatureForPawn(bed, currentCell, baby);

                    //otherwise if the toddler isn't spawned (ie being carried), try to pick a spot near where they are currently (ie just put them down)
                    else if (!baby.Spawned) return RCellFinder.SpotToStandDuringJob(hauler, null, baby.GetRegionHeld());

                    //if none of the above, just leave them where they are
                    else return currentCell;
                }
            }

            //fall-through, shouldn't happen
            return LocalTargetInfo.Invalid;
        }

        public static bool NeedsRescue(Pawn baby, Pawn hauler)
        {
            //Log.Message("Fired NeedsRescue");
            if (baby == null || hauler == null) return false;
            if (CaravanFormingUtility.IsFormingCaravanOrDownedPawnToBeTakenByCaravan(baby)) return false;
            LocalTargetInfo safePlace = SafePlaceForBaby(baby, hauler);
            //Log.Message("safePlace: " + safePlace.ToString());
            //Log.Message("baby.PositionHeld: " + safePlace.ToString());
            if (safePlace.IsValid && safePlace.Cell != baby.PositionHeld) return true;
            return false;
        }
    }


    [HarmonyPatch(typeof(ChildcareUtility), nameof(ChildcareUtility.SafePlaceForBaby))]
    class SafePlaceForBaby_Patch
    {
        static bool Prefix(ref LocalTargetInfo __result, Pawn baby, Pawn hauler)
        {
            __result = BabyTemperatureUtility.SafePlaceForBaby(baby, hauler);
            return false;
        }
    }

    //adds a check to the rescue job that trained animals use
    //to stop them repeatedly taking babies back to beds that are not good for temperature reasons
    [HarmonyPatch(typeof(JobGiver_RescueNearby), "TryGiveJob")]
    class RescueNearby_Patch
    {
        static bool Prefix(ref Job __result, JobGiver_RescueNearby __instance, Pawn pawn)
        {
            //Log.Message("Fired RescueNearby_Patch");
            float radius = (float)typeof(JobGiver_RescueNearby).GetField("radius", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);

            Predicate<Thing> validator = delegate (Thing t)
            {
                Pawn pawn3 = (Pawn)t;
                return pawn3.Downed && pawn3.Faction == pawn.Faction && !pawn3.InBed() && pawn.CanReserve(pawn3)
                    && !pawn3.IsForbidden(pawn) && !GenAI.EnemyIsNear(pawn3, 25f) && !pawn.ShouldBeSlaughtered()
                    //extra logic for only finding babies that need moving, similar to FindUnsafeBaby
                    && (!(pawn3.DevelopmentalStage == DevelopmentalStage.Baby || BabyTemperatureUtility.NeedsRescue(pawn3, pawn)));
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
                LocalTargetInfo target = BabyTemperatureUtility.SafePlaceForBaby(pawn2, pawn);
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
