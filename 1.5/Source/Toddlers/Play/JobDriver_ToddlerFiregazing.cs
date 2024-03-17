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
    class JobDriver_ToddlerFiregazing : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(base.TargetB, this.job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnForbidden(TargetIndex.A);
            this.FailOnForbidden(TargetIndex.B);

            //contains checks that are equally applicable to toddlers
            this.FailOnChildLearningConditions<JobDriver_ToddlerFiregazing>();

            yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
            Toil toil = ToilMaker.MakeToil("MakeNewToils");
            toil.initAction = delegate ()
            {
                this.pawn.jobs.posture = PawnPosture.Standing;
            };
            toil.handlingFacing = true;
            toil.tickAction = delegate ()
            {
                this.pawn.rotationTracker.FaceCell(base.TargetA.Cell);
                ToddlerPlayUtility.ToddlerPlayTickCheckEnd(this.pawn);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = ToddlerPlayUtility.PlayDuration;
            yield return toil;
            yield break;

        }
    }
}
