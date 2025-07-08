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
    //more or less copied  from JobDriver_Floordrawing
    class JobDriver_ToddlerFloordrawing : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(base.TargetA, this.job, 1, -1, null, errorOnFailed) && this.pawn.Reserve(base.TargetB, this.job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnForbidden(TargetIndex.A);
            this.FailOnForbidden(TargetIndex.B);

            //contains checks that are equally applicable to toddlers
            this.FailOnChildLearningConditions<JobDriver_ToddlerFloordrawing>();

            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
            Toil toil = ToilMaker.MakeToil("MakeNewToils");
            toil.initAction = delegate ()
            {
                this.pawn.jobs.posture = PawnPosture.Standing;
            };
            toil.handlingFacing = true;
            toil.tickAction = delegate ()
            {
                this.pawn.rotationTracker.FaceCell(base.TargetB.Cell);
                ToddlerPlayUtility.ToddlerPlayTickCheckEnd(this.pawn);
                if (this.drawingTicks % DrawingIntervalTicks == 0)
                {
                    if (!FilthMaker.TryMakeFilth(base.TargetB.Cell, this.pawn.Map, ThingDefOf.Filth_Floordrawing, 1, FilthSourceFlags.None, true))
                    {
                        this.pawn.jobs.EndCurrentJob(JobCondition.Succeeded, true, true);
                    }
                    else
                    {
                        List<Thing> thingList = base.TargetB.Cell.GetThingList(this.pawn.Map);
                        for (int i = 0; i < thingList.Count; i++)
                        {
                            if (thingList[i].def == ThingDefOf.Filth_Floordrawing)
                            {
                                this.pawn.Reserve(thingList[i], this.job, 1, -1, null, true);
                            }
                        }
                    }
                }
                this.drawingTicks++;
            };
            toil.WithEffect(EffecterDefOf.Floordrawing, TargetIndex.A, null);
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = ToddlerPlayUtility.PlayDuration;
            yield return toil;
            yield break;

        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.drawingTicks, "drawingTicks", 0, false);
        }

        private const int DrawingIntervalTicks = 2500;
        private int drawingTicks;
    }
}
