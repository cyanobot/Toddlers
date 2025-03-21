﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using static Verse.CellFinder;
using static Toddlers.ToddlerUtility;
using static RimWorld.ChildcareUtility;
using RimWorld.Planet;
using System.Reflection;
using HarmonyLib;

namespace Toddlers
{
    
    public enum BabyMoveReason
    {
        Undetermined = 0,
        OutsideZone = 1,
        Medical = 2,
        UnsafeTemperature = 3,
        ReturnToBed = 4,
        Held = 5,

        None = 99
    }


    public static class BabyMoveUtility
    {
        public const float SLEEPY_THRESHOLD_PERCENT = 0.3f;
       
        public static bool LOG_BABY_MOVE => Toddlers_Settings.debugBabySafety;



        public static void BabyMoveLog(string message)
        {
            if (LOG_BABY_MOVE) Log.Message("[Toddlers] " + message);
        }
                
        public static string MessageKeyForMoveReason(BabyMoveReason reason)
        {
            string messageKey;
            switch (reason)
            {
                case BabyMoveReason.OutsideZone:
                    messageKey = "MessageBabySafetyOutsideZone";
                    break;
                case BabyMoveReason.Medical:
                    messageKey = "MessageBabySafetyMedical";
                    break;
                case BabyMoveReason.UnsafeTemperature:
                    messageKey = "MessageBabySafetyTemperatureDanger";
                    break;
                case BabyMoveReason.ReturnToBed:
                    messageKey = "MessageBabySafetyReturnToBed";
                    break;
                case BabyMoveReason.Held:
                    messageKey = "MessageBabySafetyHeld";
                    break;
                default:
                    messageKey = "MesssageBabySafetyUnknown";
                    break;
            }

            return messageKey;
        }
        
        public static bool ShouldBeInBed(Pawn baby)
        {
            BabyMoveLog("ShouldBeInBed(" + baby + ") - " 
                + "DevelopmentalStage: " + baby.DevelopmentalStage
                + ", Downed: " + baby.Downed
                + ", Awake: " + RestUtility.Awake(baby)
                + ", careAboutFloorSleep: " + Toddlers_Settings.careAboutFloorSleep
                + ", rest.CurLevelPercentage: " + baby.needs?.rest?.CurLevelPercentage
                + ", ParentHolder: " + baby.ParentHolder
                + ", CurrentAssignment: " + baby.timetable?.CurrentAssignment
                + ", careAboutBedtime: " + Toddlers_Settings.careAboutBedtime
                );

            //if we have somehow started assessing a not-baby, they do not need putting to bed
            if (!(baby.DevelopmentalStage == DevelopmentalStage.Baby)) return false;

            //pre-toddler babies and downed toddlers should always be in bed if possible
            if (baby.Downed) return true;

            //if asleep, should be in a bed, unless adults don't care about floor sleep
            if (!RestUtility.Awake(baby) && Toddlers_Settings.careAboutFloorSleep) return true;

            //babies that do not sleep don't need to be put to bed for sleepiness
            if (baby.needs == null || baby.needs.rest == null) return false;

            //babies who are tired and being held should go to bed instead of onto floor
            if (baby.needs.rest.CurLevelPercentage < SLEEPY_THRESHOLD_PERCENT && baby.ParentHolder is Pawn) return true;

            //if adults care about bedtimes and baby is scheduled to sleep, should be in a bed
            if (baby.timetable != null && baby.timetable.CurrentAssignment == TimeAssignmentDefOf.Sleep && Toddlers_Settings.careAboutBedtime) return true;

            return false;
        }

        public static bool IsCurLocationSafe(Pawn baby)
        {
            BabyMoveLog("IsCurLocationSafe(" + baby + ") - "
                + "PositionHeld.IsForbidden: " + baby.PositionHeld.IsForbidden(baby)
                + ", SafeTemperatureAtCell: " + GenTemperature.SafeTemperatureAtCell(baby, baby.PositionHeld, baby.MapHeld)
                );
            if (baby.PositionHeld.IsForbidden(baby)) return false;
            if (!GenTemperature.SafeTemperatureAtCell(baby, baby.PositionHeld, baby.MapHeld)) return false;
            return true;
        }

        public static bool AlreadyAtTarget(LocalTargetInfo target, Pawn pawn)
        {
            BabyMoveLog("AlreadyAtTarget - target: " + target + ", pawn: " + pawn
                + ", target.Thing: " + target.Thing
                + ", pawn.CurrentBed(): " + pawn.CurrentBed()
                + ", pawn.Spawned: " + pawn.Spawned
                + ", pawn.Position: " + pawn.Position);

            if (target.HasThing && target.Thing is Building_Bed)
            {
                return pawn.CurrentBed() == target.Thing;
            }
            else
            {
                return pawn.Spawned && pawn.Position == target.Cell;
            }
        }

        //Takes the place of FindUnsafeBaby
        public static Pawn FindBabyNeedsMoving(Pawn carer, AutofeedMode priorityLevel)
        {
            if (priorityLevel == AutofeedMode.Never) return null;

            foreach (Pawn otherPawn in carer.MapHeld.mapPawns.FreeHumanlikesSpawnedOfFaction(carer.Faction))
            {
                //BabyMoveLog("FindBabyNeedsMoving checking pawn " + otherPawn);
                if (CanSuckle(otherPawn, out var _))
                {
                    BabyMoveLog(
                        "FindBabyNeedsMoving checking baby: " + otherPawn + ", carer: " + carer
                        + ", AutofeedSetting: " + otherPawn.mindState.AutofeedSetting(carer)
                        + ", IsBabyBusy: " + IsBabyBusy(otherPawn));
                }
                else continue;
                if (otherPawn.mindState.AutofeedSetting(carer) != priorityLevel 
                    || IsBabyBusy(otherPawn))
                {
                    continue;
                }
                //BabyMoveLog("FindBabyNeedsMoving passed basic checks for carer: " + carer + ", baby: " + otherPawn);

                BabyMoveReason moveReason = BabyMoveReason.None;
                LocalTargetInfo localTargetInfo = BestPlaceForBaby(otherPawn, carer, ref moveReason);
                if (!localTargetInfo.IsValid)
                {
                    BabyMoveLog("no better place found for: " + otherPawn);
                    continue;
                }                             
                
                if (AlreadyAtTarget(localTargetInfo, otherPawn))
                {
                    BabyMoveLog(otherPawn + " already at best location");
                    continue;
                }

                if (moveReason == BabyMoveReason.None && otherPawn.CarriedBy != carer)
                {
                    BabyMoveLog("moveReason None for baby: " + otherPawn + " and carer: " + carer);
                    continue;
                }

                BabyMoveLog("FindBabyNeedsMoving returning: " + otherPawn);
                return otherPawn;
            }
            return null;
        }

        //takes the place of SafePlaceForBaby + BabyNeedsMovingForTemperatureReasons
        //version with no out BabyMoveReason  for calling from SafePlaceForBaby
        //for compatibility
        public static LocalTargetInfo BestPlaceForBaby(Pawn baby, Pawn hauler, bool ignoreOtherReservations = false)
        {
            BabyMoveReason moveReason = BabyMoveReason.Undetermined;
            return BestPlaceForBaby(baby, hauler, ref moveReason, ignoreOtherReservations);
        }

        //version with out BabyMoveReason for use within our own code
        public static LocalTargetInfo BestPlaceForBaby(Pawn baby, Pawn hauler, ref BabyMoveReason moveReason, bool ignoreOtherReservations = false)
        {
            BabyMoveLog("BestPlaceForBaby - baby: " + baby + ", hauler: " + hauler);
            BabyMoveLog("Current state: .Spawned: " + baby.Spawned
                + ", PositionHeld: " + baby.PositionHeld
                + ", ParentHolder: " + baby.ParentHolder
                + ", CurrentBed: " + baby.CurrentBed()
                + ", CurJob: " + baby.CurJob
                + ", .ignoresForbidden: " + baby.CurJob?.ignoreForbidden
                + ", preexisting moveReason: " + moveReason
                );

            if (moveReason == BabyMoveReason.Undetermined) moveReason = BabyMoveReason.None;

            if (!CanSuckle(baby, out _))
            {
                BabyMoveLog(baby + " cannot suckle, returning Invalid");
                return LocalTargetInfo.Invalid;
            }
            if (!CanHaulBabyNow(hauler, baby, ignoreOtherReservations, out _))
            {
                BabyMoveLog(hauler + " cannot haul " + baby + " now, returning Invalid");
                return LocalTargetInfo.Invalid;
            }

            //first check beds, because that's probably cheaper than searching all regions
            Building_Bed bed = FindBedFor_Clone(baby, hauler, true, ignoreOtherReservations);
            BabyMoveLog("bed: " + bed);

            bool safeTemperatureAtBed = false;
            bool bedUnsuitableForTemperatureReasons = false;
            bool bedInaccessible = false;

            bool shouldBeInBed = (ShouldBeInBed(baby) || moveReason == BabyMoveReason.ReturnToBed);

            if (bed != null)
            {
                //if bed is forbidden to baby should not return it
                //EXCEPT: downed pawns are allowed to be taken to forbidden beds 
                //we recreate this logic to avoid weird interactions with the vanilla logic
                if (!baby.Downed && bed.IsForbidden(baby))
                {
                    BabyMoveLog("bed: " + bed + " is forbidden to " + baby);
                    bedInaccessible = true;
                }

                //if hauler cannot reach bed, no point considering bed
                //if baby's already in bed, won't be able to reserve, and we'll already have checked if can reach baby
                if (baby.CurrentBed() != bed &&
                    !ReservationUtility.CanReserveAndReach(hauler, bed, PathEndMode.ClosestTouch, Danger.Deadly, ignoreOtherReservations: ignoreOtherReservations))
                {
                    BabyMoveLog("bed: " + bed + " is inaccessible to " + hauler);
                    bedInaccessible = true;
                }
            }
            if (bed != null && !bedInaccessible)
            {

                //temperature injury takes priority over even medical rest
                //FindBedFor checks the best temperature beds first, so only need to check the one returned, not all possible beds
                safeTemperatureAtBed = GenTemperature.SafeTemperatureAtCell(baby, bed.Position, bed.Map);

                //medical rest is high priority
                if (HealthAIUtility.ShouldSeekMedicalRest(baby))
                {
                    //if bed is safe temperature, no other concerns
                    if (safeTemperatureAtBed)
                    {
                        //if already in bed, do not move them
                        if (baby.CurrentBed() == bed)
                        {
                            BabyMoveLog(
                                baby + " needs medical rest, already in best bed: "
                                + bed 
                                + ", returning Invalid");
                            return LocalTargetInfo.Invalid;
                        }
                        else
                        {
                            BabyMoveLog(
                                baby + " needs medical rest, bed: "
                                + bed
                                + " at safe temperature, returning: " + bed);
                            moveReason = BabyMoveReason.Medical;
                            return bed;
                        }
                    }
                    else
                    {
                        BabyMoveLog(
                             baby + " needs medical rest, bed: "
                             + bed + " not at safe temperature, checking for temperature industry");
                        //threshold for removing from current bed higher than threshold for not putting into bed
                        //to prevent continuously taking them back and forth
                        if (baby.CurrentBed() == bed)
                        {
                            //BabyMoveLog(baby + " already in bed " + bed);
                            //remove from current medical bed only if temperature injury serious or worse
                            if (baby.health.hediffSet.HasTemperatureInjury(TemperatureInjuryStage.Serious))
                            {
                                BabyMoveLog(baby + " has serious+ temperature injury, needs removing from bed: " + bed);
                                moveReason = BabyMoveReason.UnsafeTemperature;
                                bedUnsuitableForTemperatureReasons = true;      //mark this for future logic
                                //do not return here, further logic required
                            }
                            else
                            {
                                //otherwise currently in best bed, don't move baby
                                BabyMoveLog(baby + " does not have serious+ temperature injury, current bed is best despite bad temperature, returning Invalid");
                                return LocalTargetInfo.Invalid;
                            }
                        }
                        else
                        {
                            //don't take to an unsafe medical bed if temperature injury minor or worse
                            if (baby.health.hediffSet.HasTemperatureInjury(TemperatureInjuryStage.Minor))
                            {
                                BabyMoveLog(baby + " has minor+ temperature injury, do not take to unsafe bed: " + bed);
                                bedUnsuitableForTemperatureReasons = true;      //mark this for future logic
                                //do not return here, further logic required 
                            }
                            else
                            {
                                BabyMoveLog(baby + " does not have minor+ temperature injury, bed: " + bed + " is best option despite unsafe temperature, returning bed");
                                //otherwise bed is best option for them
                                moveReason = BabyMoveReason.Medical;
                                return bed;
                            }
                        }

                        //if the bed returned by FindBedFor is medical and at an unsafe temperature
                        //then check for an alternative non-medical bed at a more suitable temperature
                        //before considering just leaving the baby on the floor
                        if (bed.Medical && bedUnsuitableForTemperatureReasons)
                        {
                            Building_Bed secondaryBed = FindSecondaryBedFor(baby,hauler);
                            BabyMoveLog("secondaryBed: " + secondaryBed);
                            if (secondaryBed != null)
                            {
                                //if already in bed, do not move them
                                if (secondaryBed == baby.CurrentBed())
                                {
                                    BabyMoveLog(baby + " already in best (secondary) bed, returning Invalid");
                                    return LocalTargetInfo.Invalid;
                                }
                                else
                                {
                                    BabyMoveLog("secondary bed at safe temperature, returning secondary bed: " + secondaryBed);
                                    moveReason = BabyMoveReason.Medical;
                                    return secondaryBed;
                                }
                            }
                        }
                    }
                }

                //if not medical rest, unsafe temperature is higher priority than putting them to bed
                else if (!safeTemperatureAtBed)
                {
                    BabyMoveLog(baby + " does not need medical rest, bed: " + bed + " is not at a safe temperature, do not take to bed");
                    bedUnsuitableForTemperatureReasons = true;
                }

                else if (shouldBeInBed)
                {                    
                    //if already in bed, do not move them
                    if (baby.CurrentBed() == bed)
                    {
                        BabyMoveLog(
                            baby + " should be in bed, bed: " + bed + " is a safe temperature, "
                            + "already in best bed, returning Invalid");
                        return LocalTargetInfo.Invalid;
                    }
                    else
                    {
                        BabyMoveLog(
                            baby + " should be in bed, bed: " + bed + " is a safe temperature, "
                            + "returning " + bed);
                        moveReason = BabyMoveReason.ReturnToBed;
                        return bed;
                    }
                }
            }

            //if we haven't immediately decided to return them to bed
            //a - may be no bed/no accessible bed
            //b - bed may be unsafe temperature
            //c - baby may not need to be in bed (eg active toddler)

            //next check current location
            bool curLocationGood = true;
            bool safeTemperatureAtCur = GenTemperature.SafeTemperatureAtCell(baby, baby.PositionHeld, baby.MapHeld);

            //allowed zone is highest priority
            if (baby.PositionHeld.IsForbidden(baby))
            {
                BabyMoveLog(baby + "'s current location is forbidden, need to move them");
                moveReason = BabyMoveReason.OutsideZone;
                curLocationGood = false;
            }

            //next temperature
            else if (!safeTemperatureAtCur)
            {
                BabyMoveLog(baby + "'s current location is not a safe temperature, need to move them");
                moveReason = BabyMoveReason.UnsafeTemperature;
                curLocationGood = false;
            }

            //medical rest and bedtime have already been covered above

            //otherwise the current location for the baby is fine
            //put them down if we're holidng them
            else if (baby.ParentHolder == hauler)
            {
                BabyMoveLog(baby + " currently held by " + hauler + ", current location is good to put down");
                moveReason = BabyMoveReason.Held;

                //find a good nearby standable location instead eg not in a fire/in a wall/etc
                IntVec3 targetCell = TryFindSpotForBabyInRegion(hauler.GetRegion(), baby, hauler);

                if (targetCell.IsValid)
                {
                    BabyMoveLog("found an allowed spot nearby for " + baby + ", returning that spot: " + targetCell);
                    return (LocalTargetInfo)targetCell;
                }
            }
            //otherwise don't move them
            else
            {
                BabyMoveLog(baby + "'s current location is fine, do not move them, returning Invalid");
                return LocalTargetInfo.Invalid;
            }

            //if we have a reason to move the baby (current location is no good)
            if (!curLocationGood)
            {
                //otherwise we need to find a suitable place on the map
                FloatRange comfRange = baby.ComfortableTemperatureRange();
                FloatRange safeRange = baby.SafeTemperatureRange();
                
                Region region = JobGiver_SeekSafeTemperature.ClosestRegionWithinTemperatureRange(
                    baby.PositionHeld, baby.MapHeld, baby, comfRange,
                    TraverseParms.For(hauler, Danger.Some));
                LocalTargetInfo targetCell = LocalTargetInfo.Invalid;

                BabyMoveLog(
                    "current location of baby " + baby + " deemed bad, looking for a new location.  "
                    + "search for a comfortable region returned: " + region);
                
                //if found a comfortable region
                if (region != null)
                {
                    targetCell = TryFindSpotForBabyInRegion(region, baby, hauler);
                    //BabyMoveLog("targetCell: " + targetCell);

                    if (targetCell.IsValid)
                    {
                        BabyMoveLog("found an allowed spot in region: " + region + " for " + baby + ", returning that spot: " + targetCell);
                        return (LocalTargetInfo)targetCell;
                    }
                }

                region = JobGiver_SeekSafeTemperature.ClosestRegionWithinTemperatureRange(
                    baby.PositionHeld, baby.MapHeld, baby, safeRange,
                    TraverseParms.For(hauler, Danger.Some));

                BabyMoveLog(
                    "Couldn't find a suitable place at comfortable temperature for " + baby
                    + ". Search for a safe region returned: " + region);

                //if found a safe region
                if (region != null)
                {
                    targetCell = TryFindSpotForBabyInRegion(region, baby, hauler);
                    //BabyMoveLog("targetCell: " + targetCell);

                    if (targetCell.IsValid)
                    {
                        BabyMoveLog("found an allowed spot in region: " + region + " for " + baby + ", returning that spot: " + targetCell);
                        return (LocalTargetInfo)targetCell;
                    }
                }

                BabyMoveLog("couldn't find anywhere at a safe temperature for " + baby);

                /*
                //consider bed again
                if (bed != null && !bedInaccessible)
                {
                    BabyMoveLog("bed " + bed + " available, returning bed");
                    return bed;
                }
                */

                //if we're moving them back to their allowed zone
                //look for somewhere allowed to put them regardless of temperature
                if (baby.PositionHeld.IsForbidden(baby))
                {
                    //BabyMoveLog("Searching for allowed region for " + baby);

                    Region startRegion = baby.GetRegionHeld();
                    TraverseParms traverseParms = TraverseParms.For(hauler);
                    RegionEntryPredicate entryCondition = (Region from, Region r) => r.Allows(traverseParms, false);
                    RegionProcessor regionProcessor = delegate (Region r)
                    {
                        if (r.IsDoorway)
                        {
                            return false;
                        }
                        if (r.IsForbiddenEntirely(baby))
                        {
                            return false;
                        }
                        if (r.IsForbiddenEntirely(hauler))
                        {
                            return false;
                        }
                        region = r;
                        return true;
                    };
                    RegionTraverser.BreadthFirstTraverse(startRegion, entryCondition, regionProcessor, 9999);

                    BabyMoveLog("Search for allowed region for baby " + baby + " found: " + region);

                    //if found an allowed region
                    if (region != null)
                    {
                        //targetCell = TryFindSpotForBabyInRegion(region, baby, hauler);
                        BabyMoveLog("targetCell: " + targetCell);

                        if (targetCell.IsValid)
                        {
                            BabyMoveLog("found an allowed spot in region: " + region + " for " + baby + ", returning that spot: " + targetCell);
                            return (LocalTargetInfo)targetCell;
                        }
                    }
                }
            }

            //otherwise either:
            //a - baby doesn't need moving (never did the if block above)
            //b - couldn't find anywhere suitable to put baby (fell through the if block)
           
            //if we are holding the baby and just need to put them down, do so anyway - right here is as good as anywhere
            if (baby.ParentHolder == hauler)
            {
                moveReason = BabyMoveReason.Held;

                //find a good nearby standable location instead eg not in a fire/in a wall/etc
                IntVec3 targetCell = TryFindSpotForBabyInRegion(hauler.GetRegion(), baby, hauler, true);
                if (targetCell.IsValid)
                {
                    BabyMoveLog(
                        hauler + " is carrying " + baby + " and needs to put them down. "
                        + "found nearby acceptable position: " + targetCell + ", returning that");
                    return targetCell;
                }
                else
                {
                    BabyMoveLog(
                        hauler + " is carrying " + baby + " and needs to put them down "
                        + "but could not find a suitable spot, dropping baby at current location: " + hauler.Position);
                    return hauler.Position;
                }
            }

            //otherwise don't attempt to move the baby
            BabyMoveLog(hauler + " couldn't find a good place for " + baby + ", returning Invalid");
            moveReason = BabyMoveReason.None;
            return LocalTargetInfo.Invalid;
        }

        public static FieldInfo f_bedDefsBestToWorst_Medical = HarmonyLib.AccessTools.Field(typeof(RestUtility), "bedDefsBestToWorst_Medical");
        public static List<ThingDef> bedDefsBestToWorst_Medical = (List<ThingDef>)f_bedDefsBestToWorst_Medical.GetValue(null);

        public static FieldInfo f_bedDefsBestToWorst_RestEffectiveness = HarmonyLib.AccessTools.Field(typeof(RestUtility), "bedDefsBestToWorst_RestEffectiveness");
        public static List<ThingDef> bedDefsBestToWorst_RestEffectiveness = (List<ThingDef>)f_bedDefsBestToWorst_RestEffectiveness.GetValue(null);

        public static FieldInfo f_bedDefsBestToWorst_SlabBed_Medical = HarmonyLib.AccessTools.Field(typeof(RestUtility), "bedDefsBestToWorst_SlabBed_Medical");
        public static List<ThingDef> bedDefsBestToWorst_SlabBed_Medical = (List<ThingDef>)f_bedDefsBestToWorst_SlabBed_Medical.GetValue(null);

        public static FieldInfo f_bedDefsBestToWorst_SlabBed_RestEffectiveness = HarmonyLib.AccessTools.Field(typeof(RestUtility), "bedDefsBestToWorst_SlabBed_RestEffectiveness");
        public static List<ThingDef> bedDefsBestToWorst_SlabBed_RestEffectiveness = (List<ThingDef>)f_bedDefsBestToWorst_SlabBed_RestEffectiveness.GetValue(null);

        public static MethodInfo m_IsEmptySleeve = Toddlers_Mod.alteredCarbonLoaded ? AccessTools.Method(
            AccessTools.TypeByName("AlteredCarbon.AC_Utils"), "IsEmptySleeve",
            new Type[] {typeof(Pawn)}) : null;

        //clone of FindBedFor with a few (annotated) tweaks
        //to improve temperature checking behaviour for carried sleepers
        //and potentially avoid nullreference/nullargument errors 
        //resulting from sleeper.Map being null
        public static Building_Bed FindBedFor_Clone(Pawn sleeper, Pawn traveler, bool checkSocialProperness, bool ignoreOtherReservations = false, GuestStatus? guestStatus = null)
        {
            if (sleeper.RaceProps.IsMechanoid)
            {
                return null;
            }

            //omitted biotech check bc mod is dependent on biotech

            if (sleeper.Deathresting)
            {
                Building_Bed assignedDeathrestCasket = sleeper.ownership.AssignedDeathrestCasket;
                if (assignedDeathrestCasket != null && RestUtility.IsValidBedFor(assignedDeathrestCasket, sleeper, traveler, checkSocialProperness: true))
                {
                    CompDeathrestBindable compDeathrestBindable = assignedDeathrestCasket.TryGetComp<CompDeathrestBindable>();
                    if (compDeathrestBindable != null && (compDeathrestBindable.BoundPawn == sleeper || compDeathrestBindable.BoundPawn == null))
                    {
                        return assignedDeathrestCasket;
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
            
            List<ThingDef> list = (flag ? bedDefsBestToWorst_SlabBed_Medical : bedDefsBestToWorst_Medical);
            List<ThingDef> list2 = (flag ? bedDefsBestToWorst_SlabBed_RestEffectiveness : bedDefsBestToWorst_RestEffectiveness);
            
            //replicating Altered Carbon's FindBedFor patch
            if (Toddlers_Mod.alteredCarbonLoaded && (bool)m_IsEmptySleeve.Invoke(null, new object[] { sleeper }))
            {
                ThingDef casketDef = DefDatabase<ThingDef>.GetNamed("AC_SleeveCasket");
                if (casketDef != null)
                {
                    //clone lists before messing with them
                    list = list.ToList();
                    list2 = list2.ToList();

                    list.Insert(0, casketDef);
                    list2.Insert(0, casketDef);
                }                
            }
            if (HealthAIUtility.ShouldSeekMedicalRest(sleeper))
            {
                if (sleeper.InBed() 
                    && sleeper.CurrentBed().Medical 
                    && RestUtility.IsValidBedFor(sleeper.CurrentBed(), sleeper, traveler, checkSocialProperness, allowMedBedEvenIfSetToNoCare: false, ignoreOtherReservations, guestStatus)
                    )
                {
                    return sleeper.CurrentBed();
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
                        Building_Bed building_Bed = (Building_Bed)GenClosest.ClosestThingReachable(
                            sleeper.Position, sleeper.MapHeld, ThingRequest.ForDef(thingDef), 
                            PathEndMode.OnCell, TraverseParms.For(traveler), 9999f, 
                            (Thing b) => ((Building_Bed)b).Medical &&
                                //changed sleeper.Map to sleeper.MapHeld
                                (int)b.Position.GetDangerFor(sleeper, sleeper.MapHeld) <= (int)maxDanger2 
                                && RestUtility.IsValidBedFor(b, sleeper, traveler, checkSocialProperness, allowMedBedEvenIfSetToNoCare: false, ignoreOtherReservations, guestStatus)
                            );
                        if (building_Bed != null)
                        {
                            return building_Bed;
                        }
                    }
                }
            }
            if (sleeper.RaceProps.Dryad)
            {
                return null;
            }
            if (sleeper.ownership != null && sleeper.ownership.OwnedBed != null 
                && RestUtility.IsValidBedFor(sleeper.ownership.OwnedBed, sleeper, traveler, checkSocialProperness, allowMedBedEvenIfSetToNoCare: false, ignoreOtherReservations, guestStatus)
                )
            {
                return sleeper.ownership.OwnedBed;
            }
            
            //omitted section on love partner bed sharing because irrelevant to babies
        
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
                        sleeper.PositionHeld, sleeper.MapHeld, ThingRequest.ForDef(thingDef2), PathEndMode.OnCell, TraverseParms.For(traveler), 
                        9999f, (Thing b) => !((Building_Bed)b).Medical 
                            && (int)b.Position.GetDangerFor(sleeper, sleeper.MapHeld) <= (int)maxDanger 
                            && RestUtility.IsValidBedFor(b, sleeper, traveler, checkSocialProperness, allowMedBedEvenIfSetToNoCare: false, ignoreOtherReservations, guestStatus) 
                            && (dg > 0 || !b.Position.GetItems(b.Map).Any((Thing thing) => thing.def.IsCorpse))
                        );
                    if (building_Bed2 != null)
                    {
                        return building_Bed2;
                    }
                }
            }
            return null;
        }


        //looks for a non-medical-marked bed
        //for a baby that can't be taken to the best medical-marked bed due to bad temperature
        //simplified version of the logic in RestUtility.FindBedFor
        //sorts beds by suitability for medical rest
        //only checks beds at safe temperature
        public static Building_Bed FindSecondaryBedFor(Pawn sleeper, Pawn traveler)
        {
            BabyMoveLog("FindSecondaryBed: sleeper - " + sleeper + ", traveler: " + traveler);
            if (sleeper.RaceProps.IsMechanoid)
            {
                return null;
            }
            if (ModsConfig.BiotechActive && sleeper.Deathresting)
            {
                Building_Bed assignedDeathrestCasket = sleeper.ownership.AssignedDeathrestCasket;
                if (assignedDeathrestCasket != null && RestUtility.IsValidBedFor(assignedDeathrestCasket, sleeper, traveler, true))
                {
                    CompDeathrestBindable compDeathrestBindable = assignedDeathrestCasket.TryGetComp<CompDeathrestBindable>();
                    if (compDeathrestBindable != null && (compDeathrestBindable.BoundPawn == sleeper || compDeathrestBindable.BoundPawn == null))
                    {
                        return assignedDeathrestCasket;
                    }
                }
            }

            if (sleeper.ownership != null
                && sleeper.ownership.OwnedBed != null
                && RestUtility.IsValidBedFor(sleeper.ownership.OwnedBed, sleeper, traveler, true))
            {
                Building_Bed ownedBed = sleeper.ownership.OwnedBed;
                //BabyMoveLog("ownedBed: " + ownedBed 
                //    + ", Medical: " + ownedBed.Medical
                //    + ", danger: " + ownedBed.Position.GetDangerFor(sleeper, sleeper.MapHeld));
                if (ownedBed.Position.GetDangerFor(sleeper, sleeper.MapHeld) == Danger.None)
                    return sleeper.ownership.OwnedBed;
                else return null;
            }

            Predicate<Thing> validator = delegate(Thing t)
                {
                    //BabyMoveLog("validator with t: " + t);
                    if (!(t is Building_Bed bed)) return false;
                    /*
                    BabyMoveLog("bed: " + bed
                        + ", danger: " + t.Position.GetDangerFor(sleeper, sleeper.MapHeld)
                        + ", IsValidBed: " + RestUtility.IsValidBedFor(bed, sleeper, traveler, true)
                        );
                    */
                    if (bed.Medical) return false;
                    if (t.Position.GetDangerFor(sleeper, sleeper.MapHeld) != Danger.None) return false;
                    if (!RestUtility.IsValidBedFor(bed, sleeper, traveler, true)) return false;
                    return true;
                };

            for (int i = 0; i < bedDefsBestToWorst_Medical.Count; i++)
            {
                ThingDef thingDef = bedDefsBestToWorst_Medical[i];
                //BabyMoveLog("For loop considering def: " + thingDef
                //    + ", CanUseBedEver: " + RestUtility.CanUseBedEver(sleeper, thingDef));
                if (!RestUtility.CanUseBedEver(sleeper, thingDef))
                {
                    continue;
                }

                Building_Bed building_Bed = (Building_Bed)GenClosest.ClosestThingReachable(
                        sleeper.PositionHeld, sleeper.MapHeld, 
                        ThingRequest.ForDef(thingDef), PathEndMode.OnCell, TraverseParms.For(traveler), 9999f, 
                        validator
                        );

                if (building_Bed != null)
                {
                    return building_Bed;
                }
            }
            return null;
        }
    
        public static IntVec3 TryFindSpotForBabyInRegion(Region region, Pawn baby, Pawn hauler, bool ignoreForbidden = false, bool allowPeril = false)
        {
            IntVec3 result = SpotForBabyInRegion(region, baby, hauler, false, false, ignoreForbidden);
            if (result.IsValid) return result;

            result = SpotForBabyInRegion(region, baby, hauler, true, false, ignoreForbidden);
            if (result.IsValid) return result;
           
            if (allowPeril) result = SpotForBabyInRegion(region, baby, hauler, true, true, ignoreForbidden);
            return result;
        }
        
        public static IntVec3 SpotForBabyInRegion(Region region, Pawn baby, Pawn hauler, bool ignoreStandable = false, bool ignorePeril = false, bool ignoreForbidden = false)
        {
            Predicate<IntVec3> validator = delegate (IntVec3 c)
            {
                //BabyMoveLog("Testing c: " + c 
                //    + ", c.region: " + c.GetRegion(hauler.Map)
                //    + ", desired region: " + region
                //    );
                if (hauler.HostFaction != null && c.GetRoom(hauler.Map) != hauler.GetRoom())
                {
                    //BabyMoveLog(c + " fails because hauler cannot leave room");
                    return false;
                }
                if (!ignoreStandable)
                {
                    if (!c.Standable(hauler.Map))
                    {
                        //BabyMoveLog(c + " fails because !Standable");
                        return false;
                    }
                    if (GenPlace.HaulPlaceBlockerIn(null, c, hauler.Map, false) != null)
                    {
                        //BabyMoveLog(c + " fails because haul place blocker");
                        return false;
                    }
                }
                if (!ignorePeril)
                {
                    if (c.GetDangerFor(baby, hauler.Map) > Danger.Some)
                    {
                        //BabyMoveLog(c + " fails because danger " + c.GetDangerFor(baby, hauler.Map));
                        return false;
                    }
                    if (c.ContainsStaticFire(hauler.Map) || c.ContainsTrap(hauler.Map))
                    {
                        //BabyMoveLog(c + " fails because fire or trap");
                        return false;
                    }
                }
                if (!ignoreForbidden)
                {
                    if (c.IsForbidden(hauler) || c.IsForbidden(baby))
                    {
                        //BabyMoveLog(c + " fails because forbidden, IsForbidden(" + hauler + "): " + c.IsForbidden(hauler)
                        //    + ", IsForbidden(" + baby + "): " + c.IsForbidden(baby));
                        return false;
                    }
                }
                return true;
            };
            if (!region.TryFindRandomCellInRegion(validator, out var result))
            {
                return IntVec3.Invalid;
            }

            //BabyMoveLog("SpotForBaby returning " + result);
            return result;
        }
    }
}
