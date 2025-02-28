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
    class JobDriver_PlayCrib : JobDriver_BabyPlay
    {
        private const TargetIndex CribInd = TargetIndex.B;
        private Thing crib => base.TargetThingB;

        protected override StartingConditions StartingCondition =>
            StartingConditions.GotoBaby;

        protected override IEnumerable<Toil> Play()
        {
            //Log.Message("Firing Play() for JobDriver_PlayCrib");

            this.FailOnDestroyedNullOrForbidden(BabyInd);
            this.FailOnDestroyedNullOrForbidden(CribInd);

            Toil play = ToilMaker.MakeToil("Play");
            play.WithEffect(EffecterDefOf.PlayStatic, BabyInd);
            play.handlingFacing = true;
            play.tickAction = delegate
            {
                pawn.rotationTracker.FaceTarget(Baby);
                if (Find.TickManager.TicksGame % 1250 == 0)
                {
                    pawn.interactions.TryInteractWith(Baby, InteractionDefOf.BabyPlay);
                }
                if (roomPlayGainFactor < 0f)
                {
                    roomPlayGainFactor = BabyPlayUtility.GetRoomPlayGainFactors(base.Baby);
                }
                if (BabyPlayUtility.PlayTickCheckEnd(base.Baby, pawn, roomPlayGainFactor))
                {
                    pawn.jobs.curDriver.EndJobWith(JobCondition.Succeeded);
                }
            };
            play.AddFinishAction(delegate
            {
                if (Baby.Position == crib.Position && RestUtility.CanUseBedNow(crib, Baby, true))
                {
                    Baby.jobs.Notify_TuckedIntoBed((Building_Bed)crib);
                    Baby.mindState.Notify_TuckedIntoBed();
                }
            });
            play.defaultCompleteMode = ToilCompleteMode.Never;
            ChildcareUtility.MakeBabyPlayAsLongAsToilIsActive(play, TargetIndex.A);
            
            Toil goToBed = Toils_Goto.GotoThing(CribInd, PathEndMode.Touch);
            goToBed.FailOn(() => !pawn.IsCarryingPawn(Baby));
            goToBed.FailOnBedNoLongerUsable(CribInd, BabyInd);

            RCellFinder.TryFindGoodAdjacentSpotToTouch(pawn, Baby, out IntVec3 adjacentSpot);
            Toil goAdj = Toils_Goto.GotoCell(adjacentSpot, PathEndMode.OnCell);

            yield return Toils_Jump.JumpIf(goAdj, () => crib == Baby.CurrentBed());
            yield return Toils_Jump.JumpIf(goToBed, () => pawn.IsCarryingPawn(Baby));
            
            Toil pickup = Toils_Haul.StartCarryThing(BabyInd);
            pickup.FailOnBedNoLongerUsable(CribInd, BabyInd);
            yield return pickup;

            yield return goToBed;
            yield return Toils_Bed.TuckIntoBed(CribInd, BabyInd);
           
            yield return goAdj;
            yield return play;

        }
    }
}
