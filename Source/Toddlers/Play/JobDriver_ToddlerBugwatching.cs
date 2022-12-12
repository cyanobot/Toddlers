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
    class JobDriver_ToddlerBugwatching : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(base.TargetA, this.job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnChildLearningConditions<JobDriver_ToddlerBugwatching>();
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
            Toil toil = ToilMaker.MakeToil("Bugwatching");
            toil.initAction = delegate ()
            {
                this.pawn.jobs.posture = PawnPosture.Standing;
                List<Rot4> rots = new List<Rot4> { Rot4.East, Rot4.West };
                pawn.Rotation = rots.RandomElement<Rot4>();
            };
            toil.handlingFacing = true;
            toil.tickAction = delegate ()
            {
                ToddlerPlayUtility.ToddlerPlayTickCheckEnd(this.pawn);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = ToddlerPlayUtility.PlayDuration;
            toil.FailOn(() => !this.pawn.Position.GetRoom(this.pawn.Map).PsychologicallyOutdoors);
            yield return toil;
            yield break;
        }

    }
}
