using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toddlers
{
    class JobDriver_LetOutOfCrib : JobDriver
    {
        private Pawn Toddler => TargetA.Pawn;
        private Building_Bed Crib => (Building_Bed)TargetB.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Toddler,job,1,-1,null,errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
                .FailOnSomeonePhysicallyInteracting(TargetIndex.A);

            yield return Toils_Haul.StartCarryThing(TargetIndex.A);

            IntVec3 dropSpot;
            RCellFinder.TryFindGoodAdjacentSpotToTouch(pawn, Crib, out dropSpot);
            job.targetC = (LocalTargetInfo)dropSpot;

            yield return Toils_Goto.GotoCell(TargetIndex.C, PathEndMode.ClosestTouch);

            Toil drop = ToilMaker.MakeToil("DropToddler");
            drop.initAction = delegate
            {
                drop.actor.carryTracker.TryDropCarriedThing(drop.actor.jobs.curJob.GetTarget(TargetIndex.C).Cell, ThingPlaceMode.Direct, out var _);
            };
            drop.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return drop;
        }
    }
}
