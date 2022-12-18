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
    class JobDriver_BeDressed : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Log.Message("Started BeDressed job");
            this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
            AddFailCondition(() => TargetA.Pawn == null || TargetA.Pawn.jobs.curJob.targetA.Pawn != this.pawn);
            Toil toil = Toils_General.Wait(0);
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            yield return toil;
            yield break;
        }
    }
}
