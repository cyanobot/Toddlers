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
    class JobDriver_ToddlerPlayDecor : JobDriver
    {
        private bool atToy = false;
        private Thing Decor => TargetThingA;
        private bool PlayOnCell => ToddlerPlayUtility.PlayOnCell(Decor);
        private Vector3 VecToCell
        {
            get
            {
                if (!atToy) return Vector3.zero;
                if (PlayOnCell) return Vector3.zero;
                return Decor.Position.ToVector3() - pawn.Position.ToVector3();
            }
        }

        public override Vector3 ForcedBodyOffset
        {
            get
            {
                if (!atToy) return Vector3.zero;
                if (PlayOnCell) return new Vector3(-0.1f, 0f, 0.2f);
                else return 0.6f * VecToCell;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {            
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

            //contains checks that are equally applicable to toddlers
            this.FailOnChildLearningConditions<JobDriver_ToddlerPlayDecor>();

            if (PlayOnCell)
            {
                yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.OnCell);
            }
            else
            {
                yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.Touch);
            }
            yield return this.PlayToil(PlayOnCell);
            yield break;

        }

        private Toil PlayToil(bool playOnCell)
        {
            Toil toil = ToilMaker.MakeToil("ToddlerPlayToil");

            toil.initAction = delegate ()
            {
                this.pawn.jobs.posture = PawnPosture.Standing;
                atToy = true;
            };
            toil.handlingFacing = true;
            if (playOnCell)
            {
                toil.tickAction = delegate ()
                {
                    pawn.Rotation = Rot4.East;
                    ToddlerPlayUtility.ToddlerPlayTickCheckEnd(this.pawn);
                };
            }
            else
            {
                toil.tickAction = delegate ()
                {
                    this.pawn.rotationTracker.FaceCell(base.TargetA.Cell);
                    ToddlerPlayUtility.ToddlerPlayTickCheckEnd(this.pawn);
                };
            }
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = ToddlerPlayUtility.PlayDuration;

            return toil;
        }
    }
}
