using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toddlers
{
    class JobDriver_ToddlerWatchTelevision : JobDriver_WatchBuilding
    {
        protected override void WatchTickAction()
        {
            if (!((Building)base.TargetA.Thing).TryGetComp<CompPowerTrader>().PowerOn)
            {
                base.EndJobWith(JobCondition.Incompletable);
                return;
            }

            this.pawn.rotationTracker.FaceCell(base.TargetA.Cell);
            ToddlerPlayUtility.ToddlerPlayTickCheckEnd(this.pawn);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.targetA, job, Toddlers_Mod.televisionMaxParticipants, 0, null, errorOnFailed))
            {
                return false;
            }
            if (!pawn.ReserveSittableOrSpot(job.targetB.Cell, job, errorOnFailed))
            {
                return false;
            }
            if (base.TargetC.HasThing && base.TargetC.Thing is Building_Bed && !pawn.Reserve(job.targetC, job, ((Building_Bed)base.TargetC.Thing).SleepingSlotsCount, 0, null, errorOnFailed))
            {
                return false;
            }
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.EndOnDespawnedOrNull(TargetIndex.A, JobCondition.Incompletable);
            yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
            Toil watch = ToilMaker.MakeToil("MakeNewToils"); ;
            watch.AddPreTickAction(delegate
            {
               WatchTickAction();
            });
            if (TargetB.Cell.GetThingList(pawn.Map).Find(t => t as Building_Bed != null) != null)
                {
                Log.Message("Got into has building_bed");
                watch.AddPreInitAction(delegate ()
                {
                    watch.actor.jobs.posture = PawnPosture.LayingInBed;
                });
            }
            watch.defaultCompleteMode = ToilCompleteMode.Delay;
            watch.defaultDuration = ToddlerPlayUtility.PlayDuration;
            watch.handlingFacing = true; 
            
            if (base.TargetA.Thing.def.building != null && base.TargetA.Thing.def.building.effectWatching != null)
            {
                watch.WithEffect(() => base.TargetA.Thing.def.building.effectWatching, EffectTargetGetter);
            }
            yield return watch;
            LocalTargetInfo EffectTargetGetter()
            {
                return base.TargetA.Thing.OccupiedRect().RandomCell + IntVec3.North.RotatedBy(base.TargetA.Thing.Rotation);
            }
        }

    }
}
