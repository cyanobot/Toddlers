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

    /*
    //exists solely to insert debugging logging
    [HarmonyPatch(typeof(ChildcareUtility),nameof(ChildcareUtility.FindUnsafeBaby))]
    class FindUnsafeBaby_Patch
    {
        static bool Prefix(ref Pawn __result, Pawn mom, AutofeedMode priorityLevel)
        {
            Log.Message("Fired FindUnsafeBaby");
            if (priorityLevel == AutofeedMode.Never)
            {
                __result = null;
                return false;
            }
            foreach (Pawn item in mom.MapHeld.mapPawns.FreeHumanlikesSpawnedOfFaction(mom.Faction))
            {
                if (!ChildcareUtility.CanSuckle(item, out var _) || item.mindState.AutofeedSetting(mom) != priorityLevel || CaravanFormingUtility.IsFormingCaravanOrDownedPawnToBeTakenByCaravan(item))
                {
                    continue;
                }
                LocalTargetInfo localTargetInfo = ChildcareUtility.SafePlaceForBaby(item, mom);
                Log.Message("SafePlaceForBaby: " + localTargetInfo.ToString());
                if (!localTargetInfo.IsValid)
                {
                    continue;
                }
                if (localTargetInfo.Thing is Building_Bed building_Bed)
                {
                    Log.Message("is bed");
                    Log.Message("baby.CurrentBed: " + item.CurrentBed().ToStringSafe());
                    Log.Message("building_Bed: " + building_Bed.ToStringSafe());
                    Log.Message("currentBed == building_Bed: " + (item.CurrentBed() == building_Bed));
                    if (item.CurrentBed() == building_Bed)
                    {
                        continue;
                    }
                }
                else if (item.Spawned && item.Position == localTargetInfo.Cell)
                {
                    continue;
                }
               __result = item;
                return false;
            }
            __result = null;
            return false;
        }
    }

    //exists solely to insert debugging logging
    [HarmonyPatch(typeof(RestUtility),nameof(RestUtility.FindBedFor), new Type[] { typeof(Pawn), typeof(Pawn), typeof(bool), typeof(bool), typeof(GuestStatus?)})]
    class FindBedFor_Patch
    {
        static bool Prefix(ref Building_Bed __result, Pawn sleeper, Pawn traveler, bool checkSocialProperness, bool ignoreOtherReservations = false, GuestStatus? guestStatus = null)
        {
            Log.Message("Fired FindBedFor, sleeper:" + sleeper);
            if (sleeper.RaceProps.IsMechanoid)
            {
                __result = null;
                return false;
            }
            if (ModsConfig.BiotechActive && sleeper.Deathresting)
            {
                Building_Bed assignedDeathrestCasket = sleeper.ownership.AssignedDeathrestCasket;
                if (assignedDeathrestCasket != null && RestUtility.IsValidBedFor(assignedDeathrestCasket, sleeper, traveler, checkSocialProperness: true))
                {
                    CompDeathrestBindable compDeathrestBindable = assignedDeathrestCasket.TryGetComp<CompDeathrestBindable>();
                    if (compDeathrestBindable != null && (compDeathrestBindable.BoundPawn == sleeper || compDeathrestBindable.BoundPawn == null))
                    {
                        __result = assignedDeathrestCasket;
                        return false;
                    }
                }
            }
            bool flag = false;
            if (sleeper.Ideo != null)
            {
                foreach (Precept item in sleeper.Ideo.PreceptsListForReading)
                {
                    if (item.def.prefersSlabBed)
                    {
                        flag = true;
                        break;
                    }
                }
            }
            List<ThingDef> list = (flag ? (List<ThingDef>)typeof(RestUtility).GetField("bedDefsBestToWorst_SlabBed_Medical", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) : (List<ThingDef>)typeof(RestUtility).GetField("bedDefsBestToWorst_Medical", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null));
            List <ThingDef> list2 = (flag ? (List<ThingDef>)typeof(RestUtility).GetField("bedDefsBestToWorst_SlabBed_RestEffectiveness", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) : (List<ThingDef>)typeof(RestUtility).GetField("bedDefsBestToWorst_RestEffectiveness", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null));
            if (HealthAIUtility.ShouldSeekMedicalRest(sleeper))
            {
                if (sleeper.InBed() && sleeper.CurrentBed().Medical && RestUtility.IsValidBedFor(sleeper.CurrentBed(), sleeper, traveler, checkSocialProperness, allowMedBedEvenIfSetToNoCare: false, ignoreOtherReservations, guestStatus))
                {
                    __result = sleeper.CurrentBed();
                    return false;
                }
                for (int i = 0; i < list.Count; i++)
                {
                    ThingDef thingDef = list[i];
                    if (!RestUtility.CanUseBedEver(sleeper, thingDef))
                    {
                        continue;
                    }
                    for (int j = 0; j < 2; j++)
                    {
                        Danger maxDanger2 = ((j == 0) ? Danger.None : Danger.Deadly);
                        Building_Bed building_Bed = (Building_Bed)GenClosest.ClosestThingReachable(sleeper.Position, sleeper.MapHeld, ThingRequest.ForDef(thingDef), PathEndMode.OnCell, TraverseParms.For(traveler), 9999f, (Thing b) => ((Building_Bed)b).Medical && (int)b.Position.GetDangerFor(sleeper, sleeper.Map) <= (int)maxDanger2 && RestUtility.IsValidBedFor(b, sleeper, traveler, checkSocialProperness, allowMedBedEvenIfSetToNoCare: false, ignoreOtherReservations, guestStatus));
                        if (building_Bed != null)
                        {
                            __result = building_Bed;
                            return false;
                        }
                    }
                }
            }
            if (sleeper.RaceProps.Dryad)
            {
                __result = null;
                return false;
            }
            Log.Message("made it through the irrelevant checks");
            Log.Message("sleeper.ownership == null: " + (sleeper.ownership == null));
            Log.Message("sleeper.ownership.OwnedBed == null: " + (sleeper.ownership.OwnedBed == null));
            Log.Message("IsValidBedFor: " + RestUtility.IsValidBedFor(sleeper.ownership.OwnedBed, sleeper, traveler, checkSocialProperness, allowMedBedEvenIfSetToNoCare: false, ignoreOtherReservations, guestStatus));
            if (sleeper.ownership != null && sleeper.ownership.OwnedBed != null && RestUtility.IsValidBedFor(sleeper.ownership.OwnedBed, sleeper, traveler, checkSocialProperness, allowMedBedEvenIfSetToNoCare: false, ignoreOtherReservations, guestStatus))
            {
                Log.Message("returning sleeper.OwnedBed:" + sleeper.ownership.OwnedBed);
                __result = sleeper.ownership.OwnedBed;
                return false;
            }
            Log.Message("not returning owned bed");
            DirectPawnRelation directPawnRelation = LovePartnerRelationUtility.ExistingMostLikedLovePartnerRel(sleeper, allowDead: false);
            if (directPawnRelation != null)
            {
                Building_Bed ownedBed = directPawnRelation.otherPawn.ownership.OwnedBed;
                if (ownedBed != null && RestUtility.IsValidBedFor(ownedBed, sleeper, traveler, checkSocialProperness, allowMedBedEvenIfSetToNoCare: false, ignoreOtherReservations, guestStatus))
                {
                    __result = ownedBed;
                    return false;
                }
            }
            for (int dg = 0; dg < 3; dg++)
            {
                Danger maxDanger = ((dg <= 1) ? Danger.None : Danger.Deadly);
                for (int k = 0; k < list2.Count; k++)
                {
                    ThingDef thingDef2 = list2[k];
                    if (!RestUtility.CanUseBedEver(sleeper, thingDef2))
                    {
                        continue;
                    }
                    Building_Bed building_Bed2 = (Building_Bed)GenClosest.ClosestThingReachable(
                        sleeper.PositionHeld, sleeper.MapHeld, ThingRequest.ForDef(thingDef2), PathEndMode.OnCell, TraverseParms.For(traveler), 9999f, 
                        (Thing b) => !((Building_Bed)b).Medical && (int)b.Position.GetDangerFor(sleeper, sleeper.MapHeld) <= (int)maxDanger 
                        && RestUtility.IsValidBedFor(b, sleeper, traveler, checkSocialProperness, allowMedBedEvenIfSetToNoCare: false, ignoreOtherReservations, guestStatus)
                        && (dg > 0 || !b.Position.GetItems(b.Map).Any((Thing thing) => thing.def.IsCorpse)));
                    if (building_Bed2 != null)
                    {
                        __result = building_Bed2;
                        return false;
                    }
                }
            }
            __result = null;
            return false;
        }
    }


    //exists solely to insert debugging logging
    [HarmonyPatch(typeof(RestUtility), nameof(RestUtility.IsValidBedFor))]
    class IsValidBedFor_Patch
    {
        static bool Prefix(ref bool __result, Thing bedThing, Pawn sleeper, Pawn traveler, bool checkSocialProperness, bool allowMedBedEvenIfSetToNoCare = false, bool ignoreOtherReservations = false, GuestStatus? guestStatus = null)
        {
            Log.Message("Fired IsValidBedFor, sleeper: " + sleeper + ", bed: " + bedThing);
            Log.Message("CanUseBedNow: " + RestUtility.CanUseBedNow(bedThing, sleeper, checkSocialProperness, allowMedBedEvenIfSetToNoCare, guestStatus));
            if (!RestUtility.CanUseBedNow(bedThing, sleeper, checkSocialProperness, allowMedBedEvenIfSetToNoCare, guestStatus))
            {
                __result = false;
                return false;
            }
            Building_Bed building_Bed = bedThing as Building_Bed;
            Log.Message("CanReserveAndReach: " + traveler.CanReserveAndReach(building_Bed, PathEndMode.OnCell, Danger.Some, building_Bed.SleepingSlotsCount, -1, null, ignoreOtherReservations));
            if (!traveler.CanReserveAndReach(building_Bed, PathEndMode.OnCell, Danger.Some, building_Bed.SleepingSlotsCount, -1, null, ignoreOtherReservations))
            {
                __result = false;
                return false;
            }
            Log.Message("HasReserved: " + traveler.HasReserved<JobDriver_TakeToBed>(building_Bed, sleeper));
            if (traveler.HasReserved<JobDriver_TakeToBed>(building_Bed, sleeper))
            {
                __result = false;
                return false;
            }
            Log.Message("IsForbidden: " + building_Bed.IsForbidden(traveler));
            if (building_Bed.IsForbidden(traveler))
            {
                __result = false;
                return false;
            }
            bool num = guestStatus == GuestStatus.Prisoner;
            bool flag = guestStatus == GuestStatus.Slave;
            if (!num && !flag && building_Bed.Faction != traveler.Faction && (traveler.HostFaction == null || building_Bed.Faction != traveler.HostFaction))
            {
                __result = false;
                return false;
            }
            __result = true;
            return false;
        }
    }

    //exists solely for debugging
    [HarmonyPatch(typeof(ReservationUtility),nameof(ReservationUtility.CanReserveAndReach))]
    class CanReserveAndReachPatch
    {
        static bool Prefix(ref bool __result, Pawn p, LocalTargetInfo target, PathEndMode peMode, Danger maxDanger, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null, bool ignoreOtherReservations = false)
        {
            //Log.Message("p.Spawned: " + p.Spawned);
            if (!p.Spawned)
            {
                __result = false;
                return false;
            }
            //Log.Message("p.CanReach: " + p.CanReach(target, peMode, maxDanger));
            if (p.CanReach(target, peMode, maxDanger))
            {
                Log.Message("CanReserve: " + p.Map.reservationManager.CanReserve(p, target, maxPawns, stackCount, layer, ignoreOtherReservations));
                Log.Message("claimant.Spawned: " + p.Spawned);
                Log.Message("target.IsValid: " + target.IsValid);
                Log.Message("!target.ThingDestroyed: " + !target.ThingDestroyed);
                Log.Message("target.Thing.MapHeld == map: " + (target.Thing.MapHeld == p.Map));
                int num = ((!target.HasThing) ? 1 : target.Thing.stackCount);
                int num2 = ((stackCount == -1) ? num : stackCount);
                Log.Message("num2 > num: " + (num2 > num));
                Log.Message("ignoreOtherReservations: " + ignoreOtherReservations);
                Log.Message("map.physicalInteractionReservationManager.IsReserved(target)" + p.Map.physicalInteractionReservationManager.IsReserved(target));
                Log.Message("map.physicalInteractionReservationManager.IsReservedBy(claimant, target)" + p.Map.physicalInteractionReservationManager.IsReservedBy(p, target));

                int num3 = 0;
                int num4 = 0;
                List<ReservationManager.Reservation> reservations = (List<ReservationManager.Reservation>)typeof(ReservationManager).GetField("reservations", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(p.Map.reservationManager);
                for (int j = 0; j < reservations.Count; j++)
                {
                    ReservationManager.Reservation reservation2 = reservations[j];
                    if (!(reservation2.Target != target) && reservation2.Layer == layer && reservation2.Claimant != p
                        && (bool)typeof(ReservationManager).GetMethod("RespectsReservationsOf", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { p, reservation2.Claimant }))
                    {
                        Log.Message("Found reservation by " + reservation2.Claimant + " on " + reservation2.Target);
                        if (reservation2.MaxPawns != maxPawns)
                        {
                            return false;
                        }
                        num3++;
                        num4 = ((reservation2.StackCount != -1) ? (num4 + reservation2.StackCount) : (num4 + num));
                        if (num3 >= maxPawns || num2 + num4 > num)
                        {
                            return false;
                        }
                    }
                }
                __result = p.Map.reservationManager.CanReserve(p, target, maxPawns, stackCount, layer, ignoreOtherReservations);
                return false;
            }
            __result = false;
            return false;
        }
    }
    */
}
