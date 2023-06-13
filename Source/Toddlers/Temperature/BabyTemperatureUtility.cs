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
    public class BabyTemperatureUtility
    {
        public enum BabyMoveReason
        {
            None,
            TemperatureDanger,
            TemperatureUnsafe,
            Medical,
            OutsideZone,
            Sleepy
        }

        public static float TemperatureAtBed(Building_Bed bed, Map map)
        {
            return GenTemperature.GetTemperatureForCell(bed?.Position ?? IntVec3.Invalid, map);
        }

        public static bool IsBedSafe(Building_Bed bed, Pawn pawn)
        {
            if (pawn.SafeTemperatureRange().Includes(TemperatureAtBed(bed, pawn.MapHeld))
                || BestTemperatureForPawn(bed.Position, pawn.PositionHeld, pawn) == bed.Position
                || !TemperatureInjury(pawn, out var _, TemperatureInjuryStage.Initial))
            {
                return true;
            }
            return false;
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

        public static bool TemperatureInjury(Pawn pawn, out Hediff hediff, TemperatureInjuryStage minStage)
        {
            //Log.Message("pawn: " + pawn + ", minstage: " + minStage);

            if (pawn.health == null || pawn.health.hediffSet == null)
            {
                hediff = null;
                return false;
            }

            float coldSeverity = 0f;
            float hotSeverity = 0f;

            Hediff cold = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Hypothermia);
            //Log.Message("cold: " + cold);
            if (cold != null && cold.CurStageIndex >= (int)minStage)
            {
                coldSeverity = cold.Severity;
            }

            Hediff hot = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Heatstroke);
            //Log.Message("hot: " + hot);
            if (hot != null && hot.CurStageIndex >= (int)minStage)
            {
                hotSeverity = hot.Severity;
            }
            //Log.Message("coldSeverity = " + coldSeverity + ", hotSeverity = " + hotSeverity);

            if (coldSeverity > hotSeverity)
            {
                hediff = cold;
                return true;
            }
            if (hotSeverity > 0f)
            {
                hediff = hot;
                return true;
            }

            hediff = null;
            return false;
        }

        public static IEnumerable<FloatRange> PriorityRecoveryRanges(Pawn pawn, Hediff hediff)
        {
            if (hediff.def != HediffDefOf.Hypothermia && hediff.def != HediffDefOf.Heatstroke) yield break;

            bool cold = hediff.def == HediffDefOf.Hypothermia;
            FloatRange comfRange = pawn.ComfortableTemperatureRange();
            FloatRange safeRange = pawn.SafeTemperatureRange();

            yield return comfRange;

            FloatRange safeButWarm = new FloatRange(comfRange.TrueMax, safeRange.TrueMax);
            FloatRange safeButCold = new FloatRange(safeRange.TrueMin, comfRange.TrueMin);
            FloatRange aBitTooWarm = new FloatRange(safeRange.TrueMax, safeRange.TrueMax + 10f);
            FloatRange aBitTooCold = new FloatRange(safeRange.TrueMin - 10f, safeRange.TrueMin);

            if (cold)
            {
                yield return safeButWarm;
                yield return safeButCold;
                yield return aBitTooWarm;
            }
            else
            {
                yield return safeButCold;
                yield return safeButWarm;
                yield return aBitTooCold;
            }
        }

        public static FloatRange BetterTemperatureRange(Pawn pawn, float temp)
        {
            FloatRange comfRange = pawn.ComfortableTemperatureRange();
            FloatRange safeRange = pawn.SafeTemperatureRange();

            if (comfRange.Includes(temp)) return new FloatRange(temp,temp);
            if (safeRange.Includes(temp)) return comfRange;

            float distFromComf = Math.Abs(temp >= comfRange.TrueMax ? temp - comfRange.TrueMax : temp - comfRange.TrueMin);

            return new FloatRange(comfRange.TrueMin - distFromComf, comfRange.TrueMax + distFromComf);
        }

        public static Region ClosestAllowedRegion(Pawn pawn, Pawn hauler)
        {
            //if we're already in allowed area, don't need to find one
            if (!pawn.Position.IsForbidden(pawn))
            {
                return null;
            }

            //if we're currently on our way to a job that's in an allowed area, leave it be
            Job curJob = pawn.CurJob;
            if (curJob != null)
            {
                foreach (LocalTargetInfo target in new LocalTargetInfo[] { curJob.targetA, curJob.targetB, curJob.targetC })
                {
                    if (target.IsValid
                        && (target.HasThing && target.Thing.Spawned && !target.Thing.Position.IsForbidden(pawn)
                        || (target.Cell.InBounds(pawn.MapHeld) && !target.Cell.IsForbidden(pawn))))
                    {
                        return null;
                    }
                }
            }

            Region startRegion = pawn.GetRegion() ?? hauler.GetRegion();
            if (startRegion == null) return null;    //something funky going on

            TraverseParms traverseParms = TraverseParms.For(hauler);
            RegionEntryPredicate entryCondition = (Region from, Region r)
                => r.Allows(traverseParms, isDestination: false);

            Region outRegion = null;
            RegionProcessor regionProcessor = delegate (Region r)
            {
                if (r.IsDoorway) return false;
                if (r.IsForbiddenEntirely(pawn)) return false;
                if (r.IsForbiddenEntirely(hauler)) return false;
                outRegion = r;
                return true;
            };
            RegionTraverser.BreadthFirstTraverse(startRegion, entryCondition, regionProcessor, 9999);

            return outRegion;
        }

        public static Region ClosestAllowedRegionWithinTemperatureRange(Pawn pawn, Pawn hauler, FloatRange tempRange)
        {
            Region startRegion = pawn.GetRegion() ?? hauler.GetRegion();
            if (startRegion == null) return null;    //something funky going on

            TraverseParms traverseParms = TraverseParms.For(hauler, maxDanger: Danger.Some);
            RegionEntryPredicate entryCondition = (Region from, Region r)
                => r.Allows(traverseParms, isDestination: false);

            Region foundReg = null;
            RegionProcessor regionProcessor = delegate (Region r)
            {
                if (r.IsDoorway) return false;
                if (r.IsForbiddenEntirely(pawn)) return false;
                if (r.IsForbiddenEntirely(hauler)) return false;
                if (!tempRange.Includes(r.Room.Temperature)) return false;

                foundReg = r;
                return true;
            };

            RegionTraverser.BreadthFirstTraverse(startRegion, entryCondition, regionProcessor, 9999);
            return foundReg;
        }

        public static Region ClosestRegionWithinTemperatureRange(Pawn pawn, Pawn hauler, FloatRange tempRange, bool respectHaulerZone = true)
        {
            Region startRegion = pawn.GetRegion() ?? hauler.GetRegion();
            if (startRegion == null) return null;    //something funky going on

            TraverseParms traverseParms = TraverseParms.For(hauler, maxDanger: Danger.Some);
            RegionEntryPredicate entryCondition = (Region from, Region r)
                => r.Allows(traverseParms, isDestination: false);

            Region foundReg = null;
            RegionProcessor regionProcessor = delegate (Region r)
            {
                if (r.IsDoorway) return false;
                if (respectHaulerZone && r.IsForbiddenEntirely(hauler)) return false;
                if (!tempRange.Includes(r.Room.Temperature)) return false;

                foundReg = r;
                return true;
            };

            RegionTraverser.BreadthFirstTraverse(startRegion, entryCondition, regionProcessor, 9999);
            return foundReg;
        }

        //near-copy of RCellFinder.SpotToStandDuringJobInRegion
        //and allows for pawn being carried
        public static IntVec3 SpotForBabyInRegion(Region region, Pawn pawn, float maxDistance, bool desperate = false, bool ignoreDanger = false, Predicate<IntVec3> extraValidator = null)
        {
            Predicate<IntVec3> validator = delegate (IntVec3 c)
            {
                //Log.Message("Testing cell: " + c);
                if ((float)(pawn.PositionHeld - c).LengthHorizontalSquared > maxDistance * maxDistance)
                {
                    return false;
                }
                if (!desperate)
                {
                    if (!c.Standable(pawn.MapHeld))
                    {
                        return false;
                    }
                    if (GenPlace.HaulPlaceBlockerIn(null, c, pawn.MapHeld, checkBlueprintsAndFrames: false) != null)
                    {
                        return false;
                    }
                    if (c.GetRegion(pawn.MapHeld).type == RegionType.Portal)
                    {
                        return false;
                    }
                }
                if (!ignoreDanger && c.GetDangerFor(pawn, pawn.MapHeld) != Danger.None)
                {
                    return false;
                }
                if (c.ContainsStaticFire(pawn.MapHeld) || c.ContainsTrap(pawn.MapHeld))
                {
                    return false;
                }
                if (!pawn.MapHeld.pawnDestinationReservationManager.CanReserve(c, pawn))
                {
                    return false;
                }
                return (extraValidator == null || extraValidator(c)) ? true : false;
            };
            region.TryFindRandomCellInRegion(validator, out var result);
            return result;
        }

        public static IntVec3 SpotForBabyInRegionUnforbidden(Region region, Pawn pawn, Pawn hauler, float maxDistance, bool desperate = false, bool ignoreDanger = false, Predicate<IntVec3> extraValidator = null)
        {
            Predicate<IntVec3> validator = delegate (IntVec3 c)
            {
                //Log.Message("Testing cell " + c + ", desperate: " + desperate + ", Standable = " + c.Standable(pawn.MapHeld));
                if ((float)(hauler.Position - c).LengthHorizontalSquared > maxDistance * maxDistance)
                {
                    //Log.Message("Returning false because too far");
                    return false;
                }
                if (hauler.HostFaction != null && c.GetRoom(hauler.MapHeld) != hauler.GetRoom())
                {
                    //Log.Message("Returning false because wrong room");
                    return false;
                }
                if (!desperate)
                {
                    if (!c.Standable(pawn.MapHeld))
                    {
                        //Log.Message("Returning false because not standable");
                        return false;
                    }
                    if (GenPlace.HaulPlaceBlockerIn(null, c, pawn.MapHeld, checkBlueprintsAndFrames: false) != null)
                    {
                        //Log.Message("Returning false because HaulPlaceBlockerIn");
                        return false;
                    }
                    if (c.GetRegion(pawn.MapHeld).type == RegionType.Portal)
                    {
                        //Log.Message("Returning false because Portal");
                        return false;
                    }
                }
                if (!ignoreDanger && c.GetDangerFor(pawn, pawn.MapHeld) != Danger.None)
                {
                    //Log.Message("Returning false because danger");
                    return false;
                }
                if (c.ContainsStaticFire(pawn.MapHeld) || c.ContainsTrap(pawn.MapHeld))
                {
                    //Log.Message("Returning false because on fire");
                    return false;
                }
                if (!pawn.MapHeld.pawnDestinationReservationManager.CanReserve(c, pawn))
                {
                    //Log.Message("Returning because cannot reserve");
                    return false;
                }
                return (extraValidator == null || extraValidator(c)) ? true : false;
            };
            if (region.IsForbiddenEntirely(pawn)) return IntVec3.Invalid;
            region.TryFindRandomCellInRegionUnforbidden(pawn, validator, out var result);
            return result;
        }


        public static bool BabyNeedsMovingByHauler(Pawn baby, Pawn hauler, out Region preferredRegion, out BabyMoveReason babyMoveReason, IntVec3? positionOverride = null)
        {
            //Log.Message("Fired BabyNeedsMovingByHauler: " + baby + ", " + hauler);
            if (!ChildcareUtility.CanSuckle(baby, out var reason))
            {
                //Log.Message("!CanSuckle");
                preferredRegion = null;
                babyMoveReason = BabyMoveReason.None;
                return false;
            }
            if (!CanHaulBabyNow(hauler, baby, ignoreOtherReservations: false, out reason))
            {
                //Log.Message("!CanHaulBabyNow");
                preferredRegion = null;
                babyMoveReason = BabyMoveReason.None;
                return false;
            }
            if (baby.Drafted)
            {
                preferredRegion = null;
                babyMoveReason = BabyMoveReason.None;
                return false;
            }
            if (Find.TickManager.TicksGame < baby.mindState.lastBroughtToSafeTemperatureTick + 2500)
            {
                preferredRegion = null;
                babyMoveReason = BabyMoveReason.None;
                return false;
            }

            FloatRange comfRange = baby.ComfortableTemperatureRange();
            FloatRange safeRange = baby.SafeTemperatureRange();
            FloatRange closeToSafeRange = new FloatRange(safeRange.TrueMin - 10f, safeRange.TrueMax + 10f);

            FloatRange[] priorityTempRanges = new FloatRange[]
            {
                    comfRange,
                    safeRange,
                    closeToSafeRange,
                    new FloatRange(-100,150)    //no one ought to be putting a baby outside these values
            };

            IntVec3 rootPos = positionOverride ?? baby.PositionHeld;
            float temp = ((!positionOverride.HasValue) ? baby.AmbientTemperature : GenTemperature.GetTemperatureForCell(positionOverride.Value, baby.MapHeld));
            Region region;

            Hediff hediff;

            //if the baby is in serious danger from heatstroke/hypothermia
            //ignore zoning and prioritise opposite temperatures
            if (TemperatureInjury(baby, out hediff, TemperatureInjuryStage.Serious))
            {
                //Log.Message("Found temperature injury");
                foreach (FloatRange tempRange in PriorityRecoveryRanges(baby, hediff))
                {
                    //Log.Message("Checking temperature range " + tempRange);
                    //if the baby's already in the temperature band we're checking, don't move them
                    if (tempRange.Includes(temp))
                    {
                        //Log.Message("Baby already in best temperature range");
                        preferredRegion = null;
                        babyMoveReason = BabyMoveReason.None;
                        return false;
                    }

                    //otherwise look for a region in that temperature band to put the baby
                    region = ClosestRegionWithinTemperatureRange(baby,hauler,tempRange,false);
                    if (region != null)
                    {
                        //Log.Message("Found appropriate region, returning");
                        preferredRegion = region;
                        babyMoveReason = BabyMoveReason.TemperatureDanger;
                        return true;
                    }
                    //Log.Message("Found no region for range");
                }

                //if there's nowhere better to put the baby, consider moving them back to their allowed zone
                //if they aren't already in it and it's not a worse temperature
                if (!ForbidUtility.InAllowedArea(rootPos, baby))
                {
                    //Log.Message("Baby outside allowed area, better temperature range would be " + BetterTemperatureRange(baby,temp));
                    region = ClosestAllowedRegionWithinTemperatureRange(baby, hauler, BetterTemperatureRange(baby, temp));
                    if (region != null && region != baby.GetRegionHeld())
                    {
                        //Log.Message("Found better/equivalent region within allowed zone");
                        preferredRegion = region;
                        babyMoveReason = BabyMoveReason.OutsideZone;
                        return true;
                    }
                }

                //Log.Message("Couldn't solve temperature injury, not moving baby");
                //otherwise leave them where they are (don't move them to a worse temperature just because it's not forbidden)
                preferredRegion = null;
                babyMoveReason = BabyMoveReason.None;
                return false;
            }

            //if the baby is downed for medical reasons
            //ignore zoning to get them to a medical bed
            if (baby.Downed && HealthAIUtility.ShouldSeekMedicalRest(baby))
            {
                //Log.Message("Found baby in need of medical rest");
                Building_Bed currentBed = baby.CurrentBed();
                Building_Bed foundBed;
                if (currentBed == null || !IsBedSafe(currentBed, baby))
                {
                    foundBed = RestUtility.FindBedFor(baby, hauler, true);
                    //Log.Message("currentBed: " + currentBed + ", foundBed: " + foundBed + ", currentBed is null or unsafe");
                    if (foundBed != null && IsBedSafe(foundBed, baby))
                    {
                        //Log.Message("foundBed is temperature-safe, returning it");
                        preferredRegion = foundBed.GetRegion();
                        babyMoveReason = BabyMoveReason.Medical;
                        return true;
                    }
                    //Log.Message("found no temperature-safe alternative");
                }
                else
                {
                    if (currentBed.Medical)
                    {
                        //Log.Message("baby is already in a medical bed, no need to move");
                        preferredRegion = null;
                        babyMoveReason = BabyMoveReason.None;
                        return false;
                    }
                    foundBed = RestUtility.FindBedFor(baby, hauler, true);
                    //Log.Message("foundBed: " + foundBed + ", currentBed is nonmedical");
                    if (foundBed != null && foundBed.Medical && IsBedSafe(foundBed, baby))
                    {
                        //Log.Message("foundBed is medical and temperature-safe, returning it");
                        preferredRegion = foundBed.GetRegion();
                        babyMoveReason = BabyMoveReason.Medical;
                        return true;
                    }
                }
                //Log.Message("Could not find an appropriate bed for medical rest");
            }

            //if it's not urgent, check if the baby is forbidden to the pawn
            if (baby.IsForbidden(hauler))
            {
                //Log.Message("Baby is forbidden, not moving baby");
                preferredRegion = null;
                babyMoveReason = BabyMoveReason.None;
                return false;
            }

            //if it's not urgent, check if the baby is busy with something they shouldn't be removed from
            //like a ceremony 
            if (baby.GetLord() != null)
            {
                //Log.Message("Baby has LordJob, not moving baby");
                preferredRegion = null;
                babyMoveReason = BabyMoveReason.None;
                return false;
            }

            //if there is no immediate health concern
            //get the baby back into their allowed area
            //prioritising by temperature
            if (!ForbidUtility.InAllowedArea(rootPos, baby))
            {
                //Log.Message("Found baby outside allowed area");
                //if the child is outside the allowed zone because they are recovering from a heat injury
                //don't move them back inside if the environment wouldn't be suitable for continued recovery
                if (TemperatureInjury(baby, out hediff, TemperatureInjuryStage.Initial))
                {
                    //Log.Message("Baby has temperature injury, checking temp before returning to allowed area");
                    foreach (FloatRange tempRange in PriorityRecoveryRanges(baby, hediff).Take(4))
                    {
                        //Log.Message("Checking tempRange " + tempRange);
                        region = ClosestAllowedRegionWithinTemperatureRange(baby, hauler, tempRange);
                        if (region != null)
                        {
                            //Log.Message("Found an allowed zone in temperature range");
                            preferredRegion = region;
                            babyMoveReason = BabyMoveReason.OutsideZone;
                            return true;
                        }
                    }

                    //if there's no appropriate place, don't take them back into an unsuitable temperature
                    //Log.Message("Found no appropriate temperature for recovery in allowed area, not moving baby");
                    preferredRegion = null;
                    babyMoveReason = BabyMoveReason.None;
                    return false;
                }

                //Log.Message("Trying to pick an allowed zone by temperature");
                foreach (FloatRange tempRange in priorityTempRanges)
                {
                    //Log.Message("Checking tempRange " + tempRange);
                    region = ClosestAllowedRegionWithinTemperatureRange(baby, hauler, tempRange);
                    if (region != null)
                    {
                        //Log.Message("Found an allowed zone in temperature range");
                        preferredRegion = region;
                        babyMoveReason = BabyMoveReason.OutsideZone;
                        return true;
                    }
                }
                //Log.Message("Could not find an allowed zone to move baby to");

                //if we can't move them to their allowed zone
                //check if they should be moved to a better temperature anyway
                if (!safeRange.Includes(temp))
                {
                    //Log.Message("Baby outside allowed zone needs moving for temperature reasons");
                    foreach (FloatRange tempRange in priorityTempRanges)
                    {
                        //Log.Message("Checking tempRange " + tempRange);

                        if (tempRange.Includes(temp))
                        {
                            //Log.Message("Baby already in this tempRange, no use moving them");
                            break;
                        }

                        region = ClosestAllowedRegionWithinTemperatureRange(baby, hauler, tempRange);
                        if (region != null)
                        {
                            //Log.Message("Found an allowed zone in temperature range");
                            preferredRegion = region;
                            babyMoveReason = BabyMoveReason.TemperatureUnsafe;
                            return true;
                        }
                    }
                    //Log.Message("Could not resolve unsafe temperature");
                }
            }

            //if the baby is inside their allowed area
            else
            {
                //if they're not at a safe temperature
                //or they're a non-toddler baby not at a comf temperature and not in bed
                if (!safeRange.Includes(temp) || (baby.ageTracker.CurLifeStage == LifeStageDefOf.HumanlikeBaby && !comfRange.Includes(temp) && !baby.InBed()))
                {
                    //Log.Message("Found baby inside allowed zone at unsuitable temperature");
                    foreach (FloatRange tempRange in priorityTempRanges)
                    {
                        //Log.Message("Checking tempRange " + tempRange);

                        if (tempRange.Includes(temp))
                        {
                            //Log.Message("Baby already in this tempRange, no use moving them");
                            break;
                        }

                        region = ClosestAllowedRegionWithinTemperatureRange(baby, hauler, tempRange);
                        if (region != null)
                        {
                            //Log.Message("Found an allowed zone in temperature range");
                            preferredRegion = region;
                            babyMoveReason = BabyMoveReason.TemperatureUnsafe;
                            return true;
                        }
                    }
                    //Log.Message("Could not resolve suboptimal temperature");
                }
            }

            //if the baby is sleepy or an infant and not in bed
            if (!baby.InBed() && (baby.needs.rest.CurLevelPercentage < 0.4f || baby.Downed))
            {
                //Log.Message("Found baby or sleepy toddler not in bed");
                //if there is a bed and it's a reasonable place to put them
                Thing bed = RestUtility.FindBedFor(baby, hauler, true);
                if (bed != null && !bed.IsForbidden(baby) && GenTemperature.SafeTemperatureAtCell(baby,bed.Position,baby.MapHeld))
                {
                    //Log.Message("Found bed");
                    preferredRegion = bed.GetRegion();
                    babyMoveReason = BabyMoveReason.Sleepy;
                    return true;
                }
                //Log.Message("Found no suitable bed");
            }

            //Log.Message("No reason to move baby");
            //otherwise no reason to move baby
            preferredRegion = null;
            babyMoveReason = BabyMoveReason.None;
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

        public static LocalTargetInfo SafePlaceForBaby(Pawn baby, Pawn hauler, out BabyMoveReason moveReason)
        {
            //Log.Message("Fired SafePlaceForBaby");
            if (!ChildcareUtility.CanSuckle(baby, out var _) || !CanHaulBabyNow(hauler, baby, false, out var _, true))
            {
                //Log.Message("CanSuckle: " + ChildcareUtility.CanSuckle(baby, out var _) + ", CanHaulBabyNow: " + CanHaulBabyNow(hauler, baby, false, out var _));
                moveReason = BabyMoveReason.None;
                return LocalTargetInfo.Invalid;
            }

            IntVec3 currentCell = baby.PositionHeld;

            if (!BabyNeedsMovingByHauler(baby, hauler, out Region preferredRegion, out moveReason))
            {
                return currentCell;
            }

            //Log.Message("BabyNeedsMovingByHauler: true, preferredRegion: " + preferredRegion + ", babyMoveReason: " + moveReason);

            Thing bed = RestUtility.FindBedFor(baby, hauler, true);

            //these two reasons are only generated if there's a bed to take the baby to, so find it
            if (moveReason == BabyMoveReason.Medical || moveReason == BabyMoveReason.Sleepy)
            {
                return bed;
            }

            //if the baby's already in the right place
            if (preferredRegion == baby.GetRegionHeld())
            {
                //otherwise if the toddler isn't spawned (ie being carried), try to pick a spot near where they are currently (ie just put them down)
                if (!baby.Spawned)
                {
                    //Log.Message("preferredRegion = current region, attempting to put baby down");
                    return SpotForBabyInRegionUnforbidden(preferredRegion, baby, hauler, 9999f, ignoreDanger: true);
                }
                
                //if none of the above, just leave them where they are
                else 
                {
                    //Log.Message("preferredRegion = current region, returning currentCell");
                    return currentCell; 
                }
            }

            //if their bed is in the selected region, take them to it
            if (bed != null && preferredRegion == bed.GetRegion())
            {
                //Log.Message("found bed in preferredRegion, returning bed");
                return bed;
            }

            //find a non-forbidden spot in the selected region if possible
            IntVec3 cell = SpotForBabyInRegionUnforbidden(preferredRegion, baby, hauler, 9999f, ignoreDanger: true);
            if (cell != null && cell.IsValid)
            {
                //Log.Message("found unforbidden cell, returning: " + cell);
                return cell;
            }

            //otherwise find any spot
            cell = SpotForBabyInRegion(preferredRegion, baby, 9999f,ignoreDanger: true);
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
