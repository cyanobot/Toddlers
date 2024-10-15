using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using static Toddlers.BabyMoveUtility;

namespace Toddlers
{
    public class JobDriver_BringBabyToSafety : JobDriver
    {
        public Pawn Baby => base.TargetPawnA;

        public BabyMoveReason moveReason = BabyMoveReason.Undetermined;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Baby, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFailCondition(() => 
                pawn.Downed
                || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)
                );
            this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
            AddFailCondition(() => 
                !ChildcareUtility.CanSuckle(Baby, out var _)
                || Baby.Drafted
                );

            //determine why we're moving the baby
            Toil findMoveReason = ToilMaker.MakeToil("FindMoveReason");
            findMoveReason.initAction = delegate
            {
                LocalTargetInfo bestPlace = BestPlaceForBaby(Baby, pawn, ref moveReason);
                if (moveReason == BabyMoveReason.None || AlreadyAtTarget(bestPlace,Baby))
                {
                    if (job.playerForced) Messages.Message("MessageBabySafetyAlreadyBestLocation".Translate(Baby.Named("BABY")), new LookTargets(Baby), MessageTypeDefOf.NeutralEvent);
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
            };
            findMoveReason.defaultCompleteMode = ToilCompleteMode.Instant;
            findMoveReason.FailOn(() => pawn.Drafted && !job.playerForced);
            yield return findMoveReason;

            //send message if appropriate
            if (!job.playerForced
                 && (PawnUtility.ShouldSendNotificationAbout(pawn) || PawnUtility.ShouldSendNotificationAbout(Baby)))
            {
                yield return Toils_General.Do(delegate
                {
                    string messageKey = MessageKeyForMoveReason(moveReason);
                    if (!messageKey.NullOrEmpty())
                    {
                        Messages.Message(messageKey.Translate(pawn.Named("ADULT"), Baby.Named("BABY")), new LookTargets(pawn, Baby), MessageTypeDefOf.NeutralEvent);
                    }
                });
            }

            //create find destination toil so that we can jumpif to it
            Toil findDestination = FindDestination();

            //jump ahead if we're already holding the baby
            yield return Toils_Jump.JumpIf(findDestination,
                () => pawn.IsCarryingPawn(Baby));
                //.FailOn(() => !pawn.IsCarryingPawn(Baby) && (pawn.Downed || pawn.Drafted));
            
            //go and pick up the baby
            foreach (Toil item in JobDriver_PickupToHold.Toils(this))
            {
                yield return item;
            }

            //actually find the destination
            yield return findDestination;

            yield return Toils_Reserve.ReserveDestinationOrThing(TargetIndex.B);
            yield return Toils_Goto.Goto(TargetIndex.B, PathEndMode.OnCell).FailOnInvalidOrDestroyed(TargetIndex.B).FailOnForbidden(TargetIndex.B)
                .FailOnSomeonePhysicallyInteracting(TargetIndex.B)
                .FailOnDestroyedNullOrForbidden(TargetIndex.A)
                .FailOn(() => !pawn.IsCarryingPawn(Baby) || pawn.Downed || (pawn.Drafted && !job.playerForced));
            yield return Toils_Reserve.ReleaseDestinationOrThing(TargetIndex.B);
            yield return Toils_Bed.TuckIntoBed(TargetIndex.B, TargetIndex.A);
        }

        private Toil FindDestination()
        {
            Toil toil = ToilMaker.MakeToil("FindDestination");

            toil.initAction = delegate
            {
                LocalTargetInfo primarySpot = BestPlaceForBaby(Baby, pawn, ref moveReason);
                LocalTargetInfo caravanSpot = LocalTargetInfo.Invalid;
                if (CaravanFormingUtility.IsFormingCaravanOrDownedPawnToBeTakenByCaravan(Baby))
                {
                    caravanSpot = JobGiver_PrepareCaravan_GatherDownedPawns.FindRandomDropCell(pawn, Baby);
                }

                if (primarySpot.IsValid)
                {
                    if (caravanSpot.IsValid && caravanSpot.Cell.DistanceTo(pawn.Position)
                        < primarySpot.Cell.DistanceTo(pawn.Position))
                    {
                        toil.GetActor().CurJob.SetTarget(TargetIndex.B, caravanSpot);
                    }
                    else
                    {
                        toil.GetActor().CurJob.SetTarget(TargetIndex.B, primarySpot);
                    }
                }

                else if (caravanSpot.IsValid)
                {
                    toil.GetActor().CurJob.SetTarget(TargetIndex.B, caravanSpot);
                }
                else
                {
                    toil.GetActor().jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
            };

            toil.FailOn(() => !pawn.IsCarryingPawn(Baby) || pawn.Downed || (pawn.Drafted && !job.playerForced));

            toil.defaultCompleteMode = ToilCompleteMode.Instant;

            return toil;
        }
    }

}
