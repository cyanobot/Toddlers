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
    class BabyMoveUtility
    {
        public const float SLEEP_THRESHOLD = 0.3f;

        public enum BabyMoveReason
        {
            None,
            TemperatureDanger,
            OutsideZone,
            Medical,
            TemperatureNonUrgent,
            ReturnToBed
        }

        public static bool IsSleepy(Pawn pawn)
        {
            if (pawn == null || pawn.needs == null || pawn.needs.rest == null) return false;
            if (pawn.needs.rest.CurLevelPercentage < SLEEP_THRESHOLD) return true;
            return false;
        }

        public static float DistanceFromSafeTemperature(Pawn pawn, float temp)
        {
            FloatRange safeRange = pawn.SafeTemperatureRange();
            if (safeRange.Includes(temp)) return 0f;
            if (temp < safeRange.min) return safeRange.min - temp;
            else return temp - safeRange.max;
        }

        public static bool DangerAcceptable(Pawn p, IntVec3 c)
        {
            if (DangerUtility.GetDangerFor(c, p, p.MapHeld) <= DangerUtility.NormalMaxDanger(p))
                return true;
            return false;
        }

        public static FloatRange MaxDangerTemperataureRange(Pawn p)
        {
            Danger maxDanger = DangerUtility.NormalMaxDanger(p);
            switch (maxDanger)
            {
                case Danger.None:
                    return p.SafeTemperatureRange();
                case Danger.Some:
                    FloatRange safeRange = p.SafeTemperatureRange();
                    return new FloatRange(safeRange.min - 80f, safeRange.max + 80f);
                default:
                    return new FloatRange(-270f, 1000f);
            }
        }

        //for some reason FindBedFor ignores current bed under most circumstances
        //bit of logic to compare current bed also
        public static Building_Bed BestBedFor(Pawn p, Pawn traveler, bool checkSocialProperness, bool ignoreOtherReservations = false, GuestStatus? guestStatus = null)
        {
            Building_Bed findBed = RestUtility.FindBedFor(p, traveler, checkSocialProperness, ignoreOtherReservations, guestStatus);
            Building_Bed currentBed = p.CurrentBed();

            if (currentBed == null) return findBed;         //if not in bed, use findbed (which could also be null, but we'll just return that)
            
            //if in bed:
            
            if (findBed == null) return currentBed;         //if there's no other bed, current bed wins by default

            if (findBed == currentBed) return currentBed;   //if they're the same no further logic needed

            if (HealthAIUtility.ShouldSeekMedicalRest(p))
            {
                //if currentBed is medical it is checked by findbedfor so don't need to consider that
                if (findBed.Medical) return findBed;            //medical bed is better than nonmedical
            }

            //if current bed is a bad temperature
            //and findBed is better
            //then findBed is a more appropriate bed
            Map map = p.MapHeld;
            if (!DangerAcceptable(p, currentBed.Position))
            {
                if (DangerUtility.GetDangerFor(findBed.Position,p,map) < DangerUtility.GetDangerFor(currentBed.Position, p, map))
                {
                    return findBed;
                }
            }

            //in the absence of a compelling reason to move, prefer current bed
            return currentBed;

        }

        public static Region ClosestAllowedRegion(Pawn baby, Pawn hauler)
        {
            //if we're already in allowed area, don't need to find one
            if (!baby.PositionHeld.IsForbidden(baby))
            {
                return baby.GetRegionHeld();
            }

            Region startRegion = baby.GetRegionHeld() ?? hauler.GetRegion();
            if (startRegion == null) return null;    //something funky going on

            TraverseParms traverseParms = TraverseParms.For(hauler, maxDanger: Danger.Some);
            RegionEntryPredicate entryCondition = (Region from, Region r)
                => r.Allows(traverseParms, isDestination: false);

            Region foundReg = null;
            RegionProcessor regionProcessor = delegate (Region r)
            {
                if (r.IsDoorway) return false;
                if (r.IsForbiddenEntirely(hauler) || r.IsForbiddenEntirely(baby)) return false;

                foundReg = r;
                return true;
            };

            RegionTraverser.BreadthFirstTraverse(startRegion, entryCondition, regionProcessor, 9999);
            return foundReg;
        }

        public static Region ClosestRegionWithinTemperatureRange(Pawn baby, Pawn hauler, FloatRange tempRange, bool ignoreBabyZone = false, bool ignoreHaulerZone = false)
        {
            //Log.Message("ClosestRegionWithinTemperatureRange - baby: " + baby + ", hauler: " + hauler + ", tempRange: " + tempRange);

            Region startRegion = baby.GetRegionHeld() ?? hauler.GetRegion();
            if (startRegion == null) return null;    //something funky going on

            TraverseParms traverseParms = TraverseParms.For(hauler, maxDanger: Danger.Some);
            RegionEntryPredicate entryCondition = (Region from, Region r)
                => r.Allows(traverseParms, isDestination: false);

            Region foundReg = null;
            RegionProcessor regionProcessor = delegate (Region r)
            {
                if (r.IsDoorway) return false;
                if (!ignoreHaulerZone && r.IsForbiddenEntirely(hauler)) return false;
                if (!ignoreBabyZone && r.IsForbiddenEntirely(baby)) return false;
                if (!tempRange.Includes(r.Room.Temperature)) return false;

                foundReg = r;
                return true;
            };

            RegionTraverser.BreadthFirstTraverse(startRegion, entryCondition, regionProcessor, 9999);
            //Log.Message("Returning foundReg: " + foundReg);
            return foundReg;
        }

        public static Region BestTemperatureRegion(Pawn baby, Pawn hauler, bool ignoreBabyZone = false, bool ignoreHaulerZone = false)
        {
            Region startRegion = baby.GetRegionHeld() ?? hauler.GetRegion();
            if (startRegion == null) return null;    //something funky going on

            TraverseParms traverseParms = TraverseParms.For(hauler, maxDanger: Danger.Deadly);
            RegionEntryPredicate entryCondition = (Region from, Region r)
                => r.Allows(traverseParms, isDestination: false);

            Region bestRegion = startRegion;
            float bestTempDiff = DistanceFromSafeTemperature(baby, startRegion.Room.Temperature);

            RegionProcessor regionProcessor = delegate (Region r)
            {
                if (r.IsDoorway) return false;
                if (!ignoreBabyZone && r.IsForbiddenEntirely(baby)) return false;
                if (!ignoreHaulerZone && r.IsForbiddenEntirely(hauler)) return false;

                float tempDiff = DistanceFromSafeTemperature(baby, r.Room.Temperature);

                if (tempDiff < bestTempDiff)
                {
                    bestTempDiff = tempDiff;
                    bestRegion = r;
                }
                return false;
            };

            RegionTraverser.BreadthFirstTraverse(startRegion, entryCondition, regionProcessor);
            return bestRegion;
        }

        //near-copy of RCellFinder.SpotToStandDuringJobInRegion
        //and allows for pawn being carried
        public static IntVec3 SpotForBabyInRegion(Region region, Pawn baby, float maxDistance, bool desperate = false, bool ignoreZone = false, Predicate<IntVec3> extraValidator = null)
        {
            Predicate<IntVec3> validator = delegate (IntVec3 c)
            {
                if (!ignoreZone && region.IsForbiddenEntirely(baby)) 
                    return false;
                //Log.Message("Testing cell: " + c);
                if ((float)(baby.PositionHeld - c).LengthHorizontalSquared > maxDistance * maxDistance)
                    return false;
                if (!ignoreZone && c.IsForbidden(baby))
                    return false;
                if (!desperate)
                {
                    if (!c.Standable(baby.MapHeld))
                        return false;
                    if (GenPlace.HaulPlaceBlockerIn(null, c, baby.MapHeld, checkBlueprintsAndFrames: false) != null)
                        return false;
                    if (c.GetRegion(baby.MapHeld).type == RegionType.Portal)
                        return false;
                }
                if (c.ContainsStaticFire(baby.MapHeld) || c.ContainsTrap(baby.MapHeld))
                    return false;
                if (!baby.MapHeld.pawnDestinationReservationManager.CanReserve(c, baby))
                    return false;
                return (extraValidator == null || extraValidator(c)) ? true : false;
            };
            if (!ignoreZone && region.IsForbiddenEntirely(baby))
                return IntVec3.Invalid;
            region.TryFindRandomCellInRegion(validator, out var result);
            return result;
        }

        public static IntVec3 SpotToHaulBabyInRegion(Region region, Pawn baby, Pawn hauler, float maxDistance, bool desperate = false,
            bool ignoreBabyZone = false, bool ignoreHaulerZone = false, Predicate<IntVec3> extraValidator = null)
        {
            Predicate<IntVec3> validator = delegate (IntVec3 c)
            {
                //Log.Message("Testing cell: " + c);
                if ((float)(hauler.Position - c).LengthHorizontalSquared > maxDistance * maxDistance)
                    return false;
                if (!ignoreBabyZone && c.IsForbidden(baby))
                    return false;
                if (!ignoreHaulerZone && c.IsForbidden(hauler))
                    return false;
                if (!desperate)
                {
                    if (!c.Standable(baby.MapHeld))
                        return false;
                    if (GenPlace.HaulPlaceBlockerIn(null, c, baby.MapHeld, checkBlueprintsAndFrames: false) != null)
                        return false;
                    if (c.GetRegion(baby.MapHeld).type == RegionType.Portal)
                        return false;
                }
                if (c.ContainsStaticFire(baby.MapHeld) || c.ContainsTrap(baby.MapHeld))
                    return false;
                if (!baby.MapHeld.pawnDestinationReservationManager.CanReserve(c, baby))
                    return false;
                return (extraValidator == null || extraValidator(c)) ? true : false;
            };
            if (!ignoreBabyZone && region.IsForbiddenEntirely(baby))
                return IntVec3.Invalid;
            if (!ignoreHaulerZone && region.IsForbiddenEntirely(hauler))
                return IntVec3.Invalid;

            region.TryFindRandomCellInRegion(validator, out var result);
            return result;
        }

        public static bool NeedsMoving_TemperatureDanger(Pawn baby, IntVec3 pos)
        {
            if (!DangerAcceptable(baby, pos)) return true;
            return false;
        }

        public static bool NeedsMoving_OutsideZone(Pawn baby, IntVec3 pos)
        {
            if (!pos.InAllowedArea(baby)) return true;
            return false;
        }

        public static bool NeedsMoving_Medical(Pawn baby)
        {
            if (!HealthAIUtility.ShouldSeekMedicalRest(baby)) return false;
            if (!baby.InBed()) return true;
            Building_Bed currentBed = baby.CurrentBed();
            if (currentBed != null && !currentBed.Medical) return true;
            return false;
        }

        public static bool NeedsMoving_TemperatureNonUrgent(Pawn baby, IntVec3 pos)
        {
            if (!baby.Downed) return false;
            if (DangerUtility.NormalMaxDanger(baby) == Danger.None) return false;               //this case is caught by TemperatureDanger instead
            if (DangerUtility.GetDangerFor(pos, baby, baby.MapHeld) != Danger.None) return true;
            return false;
        }

        public static bool NeedsMoving_ReturnToBed(Pawn baby)
        {
            if (baby.InBed()) return false;

            if (baby.Downed) return true;
            if (!RestUtility.Awake(baby)) return true;

            if (baby.needs == null || baby.needs.rest == null) return false;

            if (baby.timetable != null && baby.timetable.CurrentAssignment == TimeAssignmentDefOf.Sleep) return true;

            if (baby.needs.rest.CurLevelPercentage < SLEEP_THRESHOLD) return true;

            return false;
        }

        //checks if a baby is in a situation that would cause them to be potentially moved
        //does not check if there is a better place for them to be
        //or if any given pawn can move them there
        //used as a first-pass / targeting parameter
        public static bool BabyNeedsMoving(Pawn baby, out BabyMoveReason babyMoveReason)
        {
            babyMoveReason = BabyMoveReason.None;
            //don't need to move dead/null/etc babies
            if (!ChildcareUtility.CanSuckle(baby, out var reason))
                return false;

            //don't move drafted babies to avoid conflict with player intention
            if (baby.Drafted)
                return false;

            //don't move babies too often
            if (Find.TickManager.TicksGame < baby.mindState.lastBroughtToSafeTemperatureTick + 2500)
                return false;

            //check potential reasons in priority order
            IntVec3 pos = baby.PositionHeld;
            Map map = baby.MapHeld;

            if (NeedsMoving_TemperatureDanger(baby,pos))
            {
                babyMoveReason = BabyMoveReason.TemperatureDanger;
                return true;
            }

            if (NeedsMoving_OutsideZone(baby,pos))
            {
                babyMoveReason = BabyMoveReason.OutsideZone;
                return true;
            }

            if (NeedsMoving_Medical(baby))
            {
                babyMoveReason = BabyMoveReason.Medical;
                return true;
            }

            if (NeedsMoving_TemperatureNonUrgent(baby, pos))
            {
                babyMoveReason = BabyMoveReason.TemperatureNonUrgent;
                return true;
            }

            if (NeedsMoving_ReturnToBed(baby))
            {
                babyMoveReason = BabyMoveReason.ReturnToBed;
                return true;
            }

            return false;
        }

        //checks whether there is anywhere for the baby to go
        //and whether the given hauler can get them there
        public static bool BabyNeedsMovingByHauler(Pawn baby, Pawn hauler, out Region preferredRegion, out BabyMoveReason babyMoveReason)
        {
            //Log.Message("Firing BabyNeedsMovingByHauler, baby: " + baby + ", hauler: " + hauler);

            //preferredRegion = null;

            IntVec3 pos = baby.PositionHeld;
            Map map = baby.MapHeld;
            //find baby's bed as a default to return them to
            Building_Bed bed = BestBedFor(baby, hauler, true);

            //if there's no pressing reason to move the baby
            //woulld usually happen because the player gave the order to return baby to a safe place
            //still try to output a region
            if (!BabyNeedsMoving(baby, out babyMoveReason))
            {
                //if the baby can be returned to bed, try that
                if (bed != null && !bed.IsForbidden(baby) && !bed.IsForbidden(hauler) && DangerAcceptable(baby,bed.Position)
                    && (!baby.Downed || baby.SafeTemperatureAtCell(bed.Position, map)))
                {
                    preferredRegion = bed.GetRegion();
                }
                //otherwise current location must be fine
                else
                {
                    preferredRegion = baby.GetRegionHeld();
                }

                return false;
            }
            

            //Log.Message("First pass babyMoveReason: " + babyMoveReason);

            //shouldn't happen but just in case
            /*
            if (babyMoveReason == BabyMoveReason.None) 
                return false;
            */


            //second-pass investigation of each potential move reason
            //in priority order
            bool badTemp = false;

            //temperature danger
            if (NeedsMoving_TemperatureDanger(baby, pos))
            {
                //Log.Message("!DangerAcceptable");
                babyMoveReason = BabyMoveReason.TemperatureDanger;

                bool forbiddenBed = false;

                //first check if baby's bed is okay
                if (bed != null && DangerAcceptable(baby, bed.Position))
                {
                    if (!bed.IsForbidden(hauler) && hauler.CanReach(bed,PathEndMode.Touch, Danger.Some))
                    {

                        if (bed.IsForbidden(baby))
                        {
                            forbiddenBed = true;
                        }
                        else
                        {
                            //Log.Message("Found suitable bed, returning");
                            preferredRegion = bed.GetRegion();
                            return true;
                        }
                    }
                }
                //Log.Message("Found no suitable bed");

                //then look for just a safe, allowed spot
                FloatRange tempRange = MaxDangerTemperataureRange(baby);
                preferredRegion = ClosestRegionWithinTemperatureRange(baby,hauler,tempRange);
                if (preferredRegion != null && preferredRegion != baby.GetRegionHeld())
                {
                    //Log.Message("Found safe allowed region, returning");
                    return true;
                }

                //if there's nowhere safe and allowed
                //and their bed  is safe but not allowed
                //return them to bed
                if (forbiddenBed)
                {
                    //Log.Message("Found safe but disallowed bed, returning");
                    preferredRegion = bed.GetRegion();
                    return true;
                }

                //failing that, somewhere safe and not allowed
                preferredRegion = ClosestRegionWithinTemperatureRange(baby, hauler, tempRange, true, true);
                if (preferredRegion != null && preferredRegion != baby.GetRegionHeld())
                {
                    //Log.Message("Found safe but disallowed region, returning");
                    return true;
                }

                //failing that, somewhere better than current location
                preferredRegion = BestTemperatureRegion(baby, hauler, true, true);
                if (preferredRegion != null && preferredRegion != baby.GetRegionHeld())
                {
                    //Log.Message("Found somewhere better than current location, returning");
                    return true;
                }

                //if we can't do anything about the temperature, still do other checks
                //and inform them that our starting location was unacceptable temperature
                //Log.Message("Couldn't find a solution to temperature danger");
                badTemp = true;
            }

            //outside zone
            if (NeedsMoving_OutsideZone(baby, pos))
            {
                //Log.Message("Baby outside zone");
                babyMoveReason = BabyMoveReason.OutsideZone;
                
                //if we started in an unfixable bad temperature, ignore temperature concerns
                if (badTemp)
                {
                    //Log.Message("Ignoring temperature, seeking closest allowed region");
                    preferredRegion = ClosestAllowedRegion(baby, hauler);
                }
                //otherwise don't return the baby to their zone if the temperature is unacceptable
                else
                {
                    //Log.Message("Seeking closest allowed region of acceptable temperature");
                    preferredRegion = ClosestRegionWithinTemperatureRange(baby, hauler, MaxDangerTemperataureRange(baby));
                }
                
                if (preferredRegion != null && preferredRegion != baby.GetRegionHeld())
                {
                    //Log.Message("Found a suitable allowed region, returning");
                    return true;
                }
            }

            //medical reasons
            if (HealthAIUtility.ShouldSeekMedicalRest(baby))
            {
                //Log.Message("Baby should seek medical rest");
                babyMoveReason = BabyMoveReason.Medical;
                                
                if (bed != null)
                {
                    //don't move the baby if they're already in the best bed
                    if (bed == baby.CurrentBed())
                    {
                        preferredRegion = bed.GetRegion();
                        return false;
                    }

                    //don't take the baby to a forbidden bed
                    //and
                    //don't take the baby to a bed of unacceptable temperature
                    //unless the starting temperature was also bad and unfixable
                    if (!bed.IsForbidden(baby) && (badTemp || DangerAcceptable(baby, bed.Position)))
                    {
                        //Log.Message("Found a suitable bed, retrning");
                        preferredRegion = bed.GetRegion();
                        return true;
                    }
                    //Log.Message("Found a bed but it's unsuitable");
                }
                //Log.Message("Couldn't find an appropriate bed for medical rest");
            }

            //helpless babies have tighter temperature restrictions than active toddlers
            if (NeedsMoving_TemperatureNonUrgent(baby, pos))
            {
                //Log.Message("Helpless baby is in inappropriate but non urgent temperature");
                babyMoveReason = BabyMoveReason.TemperatureNonUrgent;

                //first check if baby's bed is okay
                if (bed != null && DangerUtility.GetDangerFor(bed.Position,baby,map) == Danger.None)
                {
                    if (!bed.IsForbidden(hauler) && !bed.IsForbidden(baby) && hauler.CanReach(bed, PathEndMode.Touch, Danger.Some))
                    { 
                        //Log.Message("Found appropriate bed, returning");
                        preferredRegion = bed.GetRegion();
                        return true;
                    }
                }
                //Log.Message("Couldn't find appropriate bed");

                //then look for just a safe, allowed spot
                FloatRange tempRange = baby.SafeTemperatureRange();
                preferredRegion = ClosestRegionWithinTemperatureRange(baby, hauler, tempRange);
                if (preferredRegion != null && preferredRegion != baby.GetRegionHeld())
                {
                    //Log.Message("Found a safe allowed region, returning");
                    return true;
                }

                //don't move them  out of their allowed region if they're not actively in danger
                badTemp = true;
            }

            //return helpless babies and sleepy toddlers to their beds
            if (NeedsMoving_ReturnToBed(baby))
            {
                //Log.Message("Baby is downed or sleepy");
                babyMoveReason = BabyMoveReason.ReturnToBed;
                if (bed != null && !bed.IsForbidden(baby) && !bed.IsForbidden(hauler))
                {
                    //check if the temperature at the bed is okay before moving them there
                    if (baby.Downed && DangerUtility.GetDangerFor(bed.Position, baby, map) == Danger.None)
                    {
                        //Log.Message("Found safe bed for a downed baby, returning");
                        preferredRegion = bed.GetRegion();
                        return true;
                    }
                    if (!baby.Downed && DangerAcceptable(baby, bed.Position))
                    {
                        //Log.Message("Found an acceptable bed for a non-downed baby, returning");
                        preferredRegion = bed.GetRegion();
                        return true;
                    }
                    //unless temp at current location is also bad, then compare
                    if (badTemp && (
                        bed.GetRegion() == baby.GetRegionHeld()
                        || DistanceFromSafeTemperature(baby,bed.AmbientTemperature) <= DistanceFromSafeTemperature(baby,baby.AmbientTemperature)
                        ))
                    {
                        //Log.Message("Found a bed that isn't worse than current location, returning");
                        preferredRegion = bed.GetRegion();
                        return true;
                    }
                }
                //Log.Message("Couldn't find an appropriate bed");
            }

            //if we can't do any of the above
            //either because the problems didn't apply
            //or because there wasn't anywhere the hauler could take the baby that'd be better
            preferredRegion = baby.GetRegionHeld();
            babyMoveReason = BabyMoveReason.None;
            //Log.Message("Couldn't find any solvable problems, returning false");
            return false;
        }

        public static bool CanHaulBaby(Pawn hauler, Pawn baby, out ChildcareUtility.BreastfeedFailReason? reason, bool allowForbidden = false)
        {
            reason = null;
            if (hauler == null)
            {
                reason = ChildcareUtility.BreastfeedFailReason.HaulerNull;
            }
            else if (baby == null)
            {
                reason = ChildcareUtility.BreastfeedFailReason.BabyNull;
            }
            else if (hauler.Dead)
            {
                reason = ChildcareUtility.BreastfeedFailReason.HaulerDead;
            }
            else if (hauler.Downed)
            {
                reason = ChildcareUtility.BreastfeedFailReason.HaulerDowned;
            }
            else if (hauler.Map == null)
            {
                reason = ChildcareUtility.BreastfeedFailReason.HaulerNotOnMap;
            }
            else if (baby.MapHeld == null)
            {
                reason = ChildcareUtility.BreastfeedFailReason.BabyNotOnMap;
            }
            else if (hauler.Map != baby.MapHeld)
            {
                reason = ChildcareUtility.BreastfeedFailReason.HaulerNotOnBabyMap;
            }
            else if (baby.IsForbidden(hauler) && !allowForbidden)
            {
                reason = ChildcareUtility.BreastfeedFailReason.BabyForbiddenToHauler;
            }
            else if (!ChildcareUtility.HasBreastfeedCompatibleFactions(hauler, baby))
            {
                if (!ChildcareUtility.BabyHasFeederInCompatibleFaction(hauler.Faction, baby))
                {
                    reason = ChildcareUtility.BreastfeedFailReason.BabyInIncompatibleFactionToHauler;
                }
            }
            else if (!hauler.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                reason = ChildcareUtility.BreastfeedFailReason.HaulerIncapableOfManipulation;
            }
            return !reason.HasValue;
        }

        public static bool CanHaulBabyNow(Pawn hauler, Pawn baby, bool ignoreOtherReservations, out ChildcareUtility.BreastfeedFailReason? reason, bool allowForbidden = false)
        {
            if (!CanHaulBaby(hauler, baby, out reason, true))
            {
                return false;
            }
            if (!hauler.CanReserve(baby, 1, -1, null, ignoreOtherReservations))
            {
                reason = ChildcareUtility.BreastfeedFailReason.HaulerCannotReserveBaby;
            }
            else if (!hauler.CanReach(baby, PathEndMode.Touch, Danger.Deadly))
            {
                reason = ChildcareUtility.BreastfeedFailReason.HaulerCannotReachBaby;
            }
            return !reason.HasValue;
        }

        public static LocalTargetInfo SafePlaceForBaby(Pawn baby, Pawn hauler, out BabyMoveReason babyMoveReason)
        {
            babyMoveReason = BabyMoveReason.None;

            if (!ChildcareUtility.CanSuckle(baby, out var reason) || !CanHaulBabyNow(hauler, baby, false, out var _, true))
            {
                return LocalTargetInfo.Invalid;
            }

            Building_Bed bed = BestBedFor(baby, hauler, true);

            if (!BabyNeedsMovingByHauler(baby, hauler, out Region preferredRegion, out babyMoveReason))
            {
                return LocalTargetInfo.Invalid;
            }

            if (bed != null && bed.GetRegion() == preferredRegion)
            {
                return bed;
            }

            //if the baby's already in the right place
            if (preferredRegion == baby.GetRegionHeld() 
                && baby.Spawned
                && (preferredRegion.IsForbiddenEntirely(baby) || !baby.Position.IsForbidden(baby)))
            {
                return baby.Position;
            }

            //if the whole region is forbidden, ignore zone
            //otherwise look for an allowed spot
            bool ignoreZone = false;
            if (preferredRegion.IsForbiddenEntirely(baby)) ignoreZone = true;

            IntVec3 cell = SpotForBabyInRegion(preferredRegion, baby, 9999f, false, ignoreZone);
            if (cell != null && cell.IsValid)
            {
                //Log.Message("found random cell, returning");
                return cell;
            }

            //Log.Message("fell through, returning invalid");
            //fall-through
            return LocalTargetInfo.Invalid;
        }

        public static Pawn FindUnsafeBaby(Pawn hauler, AutofeedMode autofeedMode, out BabyMoveReason moveReason)
        {
            //Log.Message("Firing FindUnsafeBaby for " + hauler + ", autofeedMode: " + autofeedMode);
            foreach (Pawn pawn in hauler.MapHeld.mapPawns.FreeHumanlikesSpawnedOfFaction(hauler.Faction))
            {
                //only consider babies, that aren't being taken to a caravan
                if (!ChildcareUtility.CanSuckle(pawn, out var _)
                    || CaravanFormingUtility.IsFormingCaravanOrDownedPawnToBeTakenByCaravan(pawn))
                {
                    continue;
                }

                //find out if the baby needs to go somewhere 
                //and that it isn't where they already are
                LocalTargetInfo localTargetInfo = SafePlaceForBaby(pawn, hauler, out moveReason);
                //Log.Message("moveReason: " + moveReason);
                if (moveReason == BabyMoveReason.None) continue;
                if (!localTargetInfo.IsValid)
                {
                    //Log.Message("Invalid localTargetInfo");
                    continue;
                }
                if (pawn.Spawned && pawn.Position == localTargetInfo.Cell)
                {
                    //Log.Message("Position == localTargetInfo");
                    continue;
                }
                else if (localTargetInfo.Thing is Building_Bed building_Bed)
                {
                    if (pawn.CurrentBed() == building_Bed)
                    {
                        //Log.Message("Already in target bed");
                        continue;
                    }
                }

                //Log.Message("FindUnsafeBaby returning hauler: "  + hauler + ", baby: " + pawn + ", localTargetInfo: " + localTargetInfo + ", moveReason: " + moveReason);
                return pawn;
            }
            moveReason = BabyMoveReason.None;
            return null;
        }
    }
}
