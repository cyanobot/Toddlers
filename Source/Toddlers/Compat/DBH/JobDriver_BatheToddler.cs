using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using static Toddlers.Patch_DBH;
using static Toddlers.LogUtil;
using static UnityEngine.Scripting.GarbageCollector;
using static Toddlers.WashBabyUtility;

namespace Toddlers
{
    public class JobDriver_BatheToddler : JobDriver
    {
        float roomPlayGainFactor = -1f;
        bool reachedBath;
        //int waterQuality;
        public Thing Bath => job.targetB.Thing;
        public Pawn Baby => (Pawn)job.targetA.Thing;
        public IntVec3 StandCell
        {
            get => job.targetC.Cell;
            set
            {
                job.targetC = value;
            }
        } 

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!ReservationUtility.Reserve(pawn, Baby, job, 1, -1, (ReservationLayerDef)null, errorOnFailed))
            {
                return false;
            }
            if (!ReservationUtility.Reserve(pawn, Bath, job, 1, -1, (ReservationLayerDef)null, errorOnFailed))
            {
                return false;                
            }
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFailCondition(() =>
                pawn.Downed
                || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)
                || (pawn.Drafted && !job.playerForced)
                );
            AddFailCondition(() =>
                !WashBabyUtility.ColonistShouldWash(Baby)
                || (Baby.Drafted && !job.playerForced)
                );
            this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
            this.FailOnDestroyedNullOrForbidden(TargetIndex.B);

            AddFinishAction(delegate
            {
                job.playerForced = false;
                if (!Baby.DestroyedOrNull())
                {
                    Baby.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.PlayedWithMe, pawn);
                }
            });
            SetFinalizerJob((JobCondition condition) => (!reachedBath) ? null : ChildcareUtility.MakeBringBabyToSafetyJob(pawn, Baby));

            //create go to bath toil in advance so we can JumpIf to it
            Toil goToBath = Toils_Goto.GotoCell(Bath.Position, PathEndMode.Touch)
                .FailOnSomeonePhysicallyInteracting(TargetIndex.B)
                .FailOn(() => !pawn.IsCarryingPawn(Baby));

            //jump ahead if we're already holding the baby
            yield return Toils_Jump.JumpIf(goToBath,
                () => pawn.IsCarryingPawn(Baby));

            //go to the baby
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(TargetIndex.A);

            //pick  up the baby
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, putRemainderInQueue: false, false);

            //actually go to the bath
            yield return goToBath;

            //put baby in bath
            yield return PlaceInBath();

            //go to stand cell
            yield return GoToStandCell();

            //define bathtime in order to jumpif
            Toil bathtime = Bathtime();

            //fill bath
            Toil fillBath = FillBath();
            fillBath.JumpIf(() => (bool)p_IsFull.GetValue(Bath), bathtime);
            yield return fillBath;

            //actually do bathtime
            yield return bathtime;

            //take toddler out of bath
            foreach (Toil item in JobDriver_PickupToHold.Toils(this))
            {
                yield return item;
            }

            //put toddler down out of bath (provided permitted)
            
            yield return RemoveFromBath();
        }

        public static bool IsValidStandingSpot(IntVec3 c, Map map, Pawn pawn)
        {
            DebugLog("IsValidStandingSpot - c: " + c + ", pawn: " + pawn);
#if RW_1_5
            if (c == null) return false;
#endif
            if (!c.IsValid) return false;
            if (!c.Standable(map)) return false;
            if (GenPlace.HaulPlaceBlockerIn(null, c, map, checkBlueprintsAndFrames: false) != null)
            {
                return false;
            }
            if (c.GetRegion(map).type == RegionType.Portal)
            {
                return false;
            }
            if (c.ContainsStaticFire(map) || c.ContainsTrap(map))
            {
                return false;
            }
            if (c.IsForbidden(pawn))
            {
                return false;
            }
            return true;
        }
    
        public Toil PlaceInBath()
        {
            Toil placeInBath = ToilMaker.MakeToil("PlaceInBath");
            if (Bath.GetType() == t_Building_bath)
            {
                placeInBath.initAction = delegate
                {
                    IntVec3 bathPos = Bath.Position;
                    IntVec3 cellForBaby = bathPos;
                    if (Bath.Rotation == Rot4.South)
                    {
                        //cellForBaby += IntVec3.South;
                    }
                    else if (Bath.Rotation == Rot4.East)
                    {
                        cellForBaby += IntVec3.East;
                    }
                    else if (Bath.Rotation == Rot4.North)
                    {
                        cellForBaby += IntVec3.North;
                    }
                    else if (Bath.Rotation == Rot4.West)
                    {
                        cellForBaby += IntVec3.West;
                    }

                    IntVec3 cellForAdult = GenAdj.CellsAdjacent8Way(new TargetInfo(cellForBaby, Bath.Map))
                        .ToList().Find(c => IsValidStandingSpot(c, Bath.Map, placeInBath.actor));
                    DebugLog("cellForBaby: " + cellForBaby + ", cellForAdult: " + cellForAdult);
#if RW_1_5
                    if (cellForAdult == null || !cellForAdult.IsValid || cellForAdult == IntVec3.Zero)
#else
                    if (!cellForAdult.IsValid || cellForAdult == IntVec3.Zero)
#endif
                    {
                        cellForAdult = GenAdj.CellsAdjacent8Way(new TargetInfo(bathPos, Bath.Map))
                        .ToList().Find(c => IsValidStandingSpot(c, Bath.Map, placeInBath.actor));
                    }

#if RW_1_5
                    if (cellForAdult == null || !cellForAdult.IsValid || cellForAdult == IntVec3.Zero)
#else
                    if (!cellForAdult.IsValid || cellForAdult == IntVec3.Zero)
#endif
                    {
                        StandCell = placeInBath.actor.Position;
                    }
                    else
                    {
                        StandCell = cellForAdult;
                    }

                    reachedBath = true;
                    placeInBath.actor.carryTracker.TryDropCarriedThing(
                        cellForBaby,
                        ThingPlaceMode.Direct, out var _);

                    Job waitJob = JobMaker.MakeJob(DBHDefOf.ToddlerBeWashed, placeInBath.actor, 10000);
                    Baby.jobs.StartJob(waitJob, JobCondition.InterruptForced);
                    Baby.health.hediffSet.AddDirect(HediffMaker.MakeHediff(DBHDefOf.Washing, Baby));
                    Baby.jobs.posture = PawnPosture.LayingOnGroundFaceUp;
                };
            }
            else
            {
                placeInBath.initAction = delegate
                {
                    StandCell = placeInBath.actor.Position;

                    reachedBath = true;
                    placeInBath.actor.carryTracker.TryDropCarriedThing(
                        Bath.Position,
                        ThingPlaceMode.Direct, out var _);

                    Job waitJob = JobMaker.MakeJob(DBHDefOf.ToddlerBeWashed, placeInBath.actor, 10000);
                    Baby.jobs.StartJob(waitJob, JobCondition.InterruptForced);
                    Baby.health.hediffSet.AddDirect(HediffMaker.MakeHediff(DBHDefOf.Washing, Baby));
                    Baby.jobs.posture = PawnPosture.LayingOnGroundFaceUp;
                };
            }
                
            placeInBath.defaultCompleteMode = ToilCompleteMode.Instant;
            return placeInBath;
        }

        public Toil GoToStandCell()
        {
            Toil goToStandCell = ToilMaker.MakeToil("GotoStandCell");
            goToStandCell.initAction = delegate
            {
                Pawn actor = goToStandCell.actor;
                DebugLog("StandCell: " + StandCell);
#if RW_1_5
                if (StandCell == null) actor.jobs.curDriver.ReadyForNextToil();
#endif
                if (!StandCell.IsValid)
                {
                    actor.jobs.curDriver.ReadyForNextToil();
                }
                if (actor.Position == StandCell)
                {
                    actor.jobs.curDriver.ReadyForNextToil();
                }
                else
                {
                    actor.pather.StartPath(StandCell, PathEndMode.OnCell);
                }
            };
            return goToStandCell;
        }

        public Toil FillBath()
        {
            Toil fillBath;
            if (Bath.GetType() == t_Building_bath)
            {
                fillBath = ToilMaker.MakeToil("FillBath");
                fillBath.PlaySustainerOrSound(DBHDefOf.shower_Ambience);
                fillBath.defaultDuration = 1000;
                fillBath.defaultCompleteMode = ToilCompleteMode.Delay;
                fillBath.initAction = delegate
                {
                };
                fillBath.tickAction = delegate
                {
                    bool tryFillBath = (bool)m_TryFillBath.Invoke(Bath, new object[] { });
                    if (!tryFillBath)
                    {
                        EndJobWith(JobCondition.Incompletable);
                    }
                };                
            }
            else
            {
                //washtub is only a candidate if already full, so does not need filling
                fillBath = ToilMaker.MakeToil("EmptyToil");
            }
            return fillBath;
        }

        public Toil Bathtime()
        {
            Toil bathtime = ToilMaker.MakeToil("BathTime");
            bathtime.defaultDuration = 1000;
            bathtime.PlaySustainerOrSound(() => SoundDefOf.Interact_CleanFilth);
            bathtime.defaultCompleteMode = ToilCompleteMode.Delay;
            bathtime.WithEffect(DBHDefOf.WashingEffect, TargetIndex.A);
            bathtime.WithProgressBar(TargetIndex.A, () =>
                (WashBabyUtility.HygieneNeedFor(Baby).CurLevel
                + Baby.needs.play.CurLevel
                ) / 2f);
            bathtime.handlingFacing = true;
            bathtime.initAction = delegate
            {
                //as washBaby
                pawn.rotationTracker.FaceTarget(Baby);

                //as takeBath
                base.Map.mapDrawer.MapMeshDirty(Bath.Position, MapMeshFlagDefOf.Buildings);
                Baby.needs.mood.thoughts.memories.RemoveMemoriesOfDef(DefDatabase<ThoughtDef>.GetNamed("SoakingWet"));
            };
#if RW_1_5
            bathtime.tickAction = delegate
            {
                int delta = 1;
#else
            bathtime.tickIntervalAction = delegate(int delta)
            {
#endif
                Need need_Hygiene = Baby.needs.AllNeeds.Find(n => n.def == DBHDefOf.Hygiene);
                if (need_Hygiene == null) pawn.jobs.EndCurrentJob(JobCondition.Errored);

                if (Baby.needs.play.CurLevel > 0.99f
                    && need_Hygiene.CurLevel > 0.99f)
                {
                    pawn.jobs.curDriver.ReadyForNextToil();
                }

                //as wash
                need_Hygiene.CurLevel = Mathf.Min(need_Hygiene.CurLevel + 0.001f, 1f);
                f_lastGainTick.SetValue(need_Hygiene, Find.TickManager.TicksGame);

                //as JobDriver_BabyPlay
                if (Find.TickManager.TicksGame % 1250 == 0)
                {
                    pawn.interactions.TryInteractWith(Baby, InteractionDefOf.BabyPlay);
                }
                if (roomPlayGainFactor < 0f)
                {
                    roomPlayGainFactor = BabyPlayUtility.GetRoomPlayGainFactors(Baby);
                }

                //treat bath as as fun as a toy box
                float playChange = delta * 0.0002f * 1.25f * roomPlayGainFactor;
                Baby.needs.play.Play(playChange);
                ToddlerPlayUtility.CureLoneliness(Baby);

                float joyChange = 0.000144f * 1.25f;
                pawn.needs?.joy?.GainJoy(joyChange, JoyKindDefOf.Social);
            };
            bathtime.AddFinishAction(delegate
            {
                if (Baby.health.hediffSet.HasHediff(DBHDefOf.Washing))
                {
                    Baby.health.RemoveHediff(Baby.health.hediffSet.GetFirstHediffOfDef(DBHDefOf.Washing));
                }
                Baby.filth.CarriedFilthListForReading.Clear();
                
                if (Baby.CurJobDef == DBHDefOf.ToddlerBeWashed)
                {
                    Baby.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
            });
            if (Bath.GetType() == t_Building_bath)
            {
                bathtime.AddPreInitAction(delegate
                {
                    f_occupant.SetValue(Bath, Baby);
                });
                bathtime.AddFinishAction(delegate
                {
                    f_occupant.SetValue(Bath, null);
                    Need need_Hygiene = HygieneNeedFor(Baby);
                    if (need_Hygiene != null)
                    {
                        f_contaminated.SetValue(need_Hygiene, false);
                    }
                    m_ContaminationCheckWater.Invoke(null, new object[]
                        { Baby, f_contamination_bath.GetValue(Bath) });
                    m_CheckForBlockage.Invoke(null, new object[] { Bath as Building });
                    m_TryPullPlug.Invoke(Bath, new object[] { });
                });
            }
            else if (Bath.GetType() == t_Building_washbucket)
            {
                bathtime.AddFinishAction(delegate
                {
                    f_WaterUsesRemaining.SetValue(Bath, 0);
                });
            }
            return bathtime;
        }

        public Toil RemoveFromBath()
        {
            Toil removeFromBath = ToilMaker.MakeToil("RemoveFromBath");
            removeFromBath.initAction = delegate
            {
                if (!IsValidStandingSpot(StandCell, Bath.Map, Baby)) return;
                removeFromBath.actor.carryTracker.TryDropCarriedThing(
                    StandCell,
                    ThingPlaceMode.Direct, out var _);
            };
            removeFromBath.defaultCompleteMode = ToilCompleteMode.Instant;
            return removeFromBath;
        }
    }

    
}
