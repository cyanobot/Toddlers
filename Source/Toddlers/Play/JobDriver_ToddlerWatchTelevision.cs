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
        public bool FromBed => job.GetTarget(TargetIndex.A).Thing is Building_Bed;

#if RW_1_
        protected override void WatchTickAction()
        {
        int delta = 1;
#else
        protected override void WatchTickAction(int delta)
        {
#endif
            if (!((Building)base.TargetA.Thing).TryGetComp<CompPowerTrader>().PowerOn)
            {
                base.EndJobWith(JobCondition.Incompletable);
                return;
            }

            this.pawn.rotationTracker.FaceCell(base.TargetA.Cell);
            ToddlerPlayUtility.ToddlerPlayTickCheckEnd(this.pawn,delta);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.EndOnDespawnedOrNull(TargetIndex.A);
            Toil watch;
            if (base.TargetC.HasThing && base.TargetC.Thing is Building_Bed)
            {
                this.KeepLyingDown(TargetIndex.C);
                yield return Toils_Bed.ClaimBedIfNonMedical(TargetIndex.C);
                yield return Toils_Bed.GotoBed(TargetIndex.C);
                watch = Toils_LayDown.LayDown(TargetIndex.C, hasBed: true, lookForOtherJobs: false);
                watch.AddFailCondition(() => !watch.actor.Awake());
            }
            else
            {
                yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
                watch = ToilMaker.MakeToil("MakeNewToils");
            }
#if RW_1_5
            watch.AddPreTickAction(delegate
            {
               WatchTickAction();
            });
#else
            watch.AddPreTickIntervalAction(WatchTickAction);
#endif
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
