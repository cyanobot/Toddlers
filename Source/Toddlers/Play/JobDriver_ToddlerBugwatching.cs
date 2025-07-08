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
                pawn.Drawer.renderer.SetAnimation(Toddlers_AnimationDefOf.Bugwatch);
                //pawn.Drawer.renderer.SetAllGraphicsDirty();
            };
            toil.handlingFacing = true;
            toil.tickAction = delegate ()
            {
                if (pawn.Drawer.renderer.CurAnimation != Toddlers_AnimationDefOf.Bugwatch) pawn.Drawer.renderer.SetAnimation(Toddlers_AnimationDefOf.Bugwatch);
                ToddlerPlayUtility.ToddlerPlayTickCheckEnd(this.pawn);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = ToddlerPlayUtility.PlayDuration;
            toil.FailOn(() => !this.pawn.Position.GetRoom(this.pawn.Map).PsychologicallyOutdoors);
            toil.AddFinishAction(() => pawn.Drawer.renderer.SetAnimation(null));         //pawn.Drawer.renderer.SetAllGraphicsDirty()); 
            yield return toil;
            yield break;
        }

    }
}
