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
    class JobDriver_ToddlerPlayToys : JobDriver
    {
        private const int ToysCount = 5;
        private const float ToyDistanceFactor = 0.5f;
        private static readonly FloatRange ToyRandomAngleOffset = new FloatRange(-5f, 5f);
        private Mote[] motesToMaintain;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(base.TargetA, this.job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(TargetIndex.A);

            //contains checks that are equally applicable to toddlers
            this.FailOnChildLearningConditions<JobDriver_ToddlerPlayToys>();

            yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.Touch);
            yield return this.PlayToil();
            yield break;

        }

        private Toil PlayToil()
        {
            Toil toil = ToilMaker.MakeToil("ToddlerPlayToil");

            toil.initAction = delegate ()
            {
                this.pawn.jobs.posture = PawnPosture.Standing;
            };
            toil.handlingFacing = true;
            toil.tickAction = delegate ()
            {
                if (this.motesToMaintain.NullOrEmpty<Mote>())
                {
                    Vector3 a = this.pawn.TrueCenter();
                    Vector3 v = IntVec3.North.ToVector3();
                    float num = 72f;
                    this.motesToMaintain = new Mote[ToysCount];
                    for (int i = 0; i < ToysCount; i++)
                    {
                        Vector3 loc = a + v.RotatedBy(num * (float)i + ToyRandomAngleOffset.RandomInRange) * ToyDistanceFactor;
                        this.motesToMaintain[i] = MoteMaker.MakeStaticMote(loc, base.Map, ThingDefOf.Mote_Toy, 1f, false);
                    }
                }
                for (int j = 0; j < this.motesToMaintain.Length; j++)
                {
                    this.motesToMaintain[j].Maintain();
                }
                this.pawn.rotationTracker.FaceCell(base.TargetA.Cell);
                ToddlerPlayUtility.ToddlerPlayTickCheckEnd(this.pawn);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = ToddlerPlayUtility.PlayDuration;

            return toil;
        }
    }
}
