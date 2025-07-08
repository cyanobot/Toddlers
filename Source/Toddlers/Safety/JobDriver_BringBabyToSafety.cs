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
                || (pawn.Drafted && !job.playerForced)
                );
            this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
            AddFailCondition(() => 
                !ChildcareUtility.CanSuckle(Baby, out var _)
                || (Baby.Drafted && !job.playerForced)
                );

            //determine why we're moving the baby           
            yield return FindMoveReason();                //index 0

            //send message if appropriate
            Toil messageToil = MessageToil();
            messageToil.debugName = "SendMessage";
            yield return messageToil;                   //index 1

            //create find destination toil so that we can jumpif to it
            Toil findDestination = FindDestination();

            //jump ahead if we're already holding the baby
            yield return Toils_Jump.JumpIf(findDestination,
                () => pawn.IsCarryingPawn(Baby));                               //index 2
                   
            //go to the baby
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(TargetIndex.A);   //index 3

            //check if the baby still needs moving
            yield return FindMoveReason();                                        //index 4

            //pick  up the baby
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, putRemainderInQueue: false, false);      //index 5

            //actually find the destination
            yield return findDestination;                                   //index 6

            yield return Toils_Reserve.ReserveDestinationOrThing(TargetIndex.B);                                                                        //index 7
            yield return Toils_Goto.Goto(TargetIndex.B, PathEndMode.OnCell).FailOnInvalidOrDestroyed(TargetIndex.B).FailOnForbidden(TargetIndex.B)      //index 8
                .FailOnSomeonePhysicallyInteracting(TargetIndex.B)
                .FailOnDestroyedNullOrForbidden(TargetIndex.A)
                .FailOn(() => !pawn.IsCarryingPawn(Baby) || pawn.Downed || (pawn.Drafted && !job.playerForced));
            yield return Toils_Reserve.ReleaseDestinationOrThing(TargetIndex.B);                                                                        //index 9
            yield return Toils_Bed.TuckIntoBed(TargetIndex.B, TargetIndex.A);                                                                           //index 10
        }

        private Toil FindMoveReason()
        {
            Toil findMoveReason = ToilMaker.MakeToil("FindMoveReason");
            findMoveReason.initAction = delegate
            {
                LocalTargetInfo bestPlace = BestPlaceForBaby(Baby, pawn, ref moveReason);
                BabyMoveLog("Toil findMoveReason - "
                    + "actor: " + pawn + ", baby: " + Baby
                    + ", bestPlace: " + bestPlace + ", moveReason: " + moveReason);
                if (moveReason == BabyMoveReason.None || !bestPlace.IsValid || AlreadyAtTarget(bestPlace, Baby))
                {
                    if (job.playerForced)
                    { Messages.Message("MessageBabySafetyAlreadyBestLocation".Translate(Baby.Named("BABY")), new LookTargets(Baby), MessageTypeDefOf.NeutralEvent); }
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
            };
            findMoveReason.defaultCompleteMode = ToilCompleteMode.Instant;

            return findMoveReason;
        }

        private Toil MessageToil()
        {
            if (!job.playerForced && (PawnUtility.ShouldSendNotificationAbout(pawn) || PawnUtility.ShouldSendNotificationAbout(Baby)))
            {
                return Toils_General.Do(delegate
                {
                    bool sendMessage;

                    switch (Toddlers_Settings.moveMessageSetting)
                    {
                        case MoveMessageSetting.None:
                            sendMessage = false;
                            break;
                        case MoveMessageSetting.Danger:
                            sendMessage = (moveReason == BabyMoveReason.Medical || moveReason == BabyMoveReason.UnsafeTemperature);
                            break;
                        case MoveMessageSetting.DangerAndUnknown:
                            sendMessage = (moveReason == BabyMoveReason.Medical || moveReason == BabyMoveReason.UnsafeTemperature
                                            || moveReason == BabyMoveReason.Undetermined || moveReason == BabyMoveReason.None);
                            break;
                        case MoveMessageSetting.Unknown:
                            sendMessage = (moveReason == BabyMoveReason.Undetermined || moveReason == BabyMoveReason.None);
                            break;
                        default:
                            sendMessage = !(moveReason == BabyMoveReason.Held);
                            break;
                    }

                    string messageKey = MessageKeyForMoveReason(moveReason);
                    if (sendMessage && !messageKey.NullOrEmpty())
                    {
                        Messages.Message(messageKey.Translate(pawn.Named("ADULT"), Baby.Named("BABY")), new LookTargets(pawn, Baby), MessageTypeDefOf.NeutralEvent);
                    }
                });
            }
            else
            {
                return ToilMaker.MakeToil();
            }
            
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

                BabyMoveLog("Toil findDestination - "
                    + "actor: " + toil.GetActor()
                    + ", baby: " + Baby
                    + "primarySpot: " + primarySpot
                    );
            };

            toil.FailOn(() => !pawn.IsCarryingPawn(Baby) || pawn.Downed || (pawn.Drafted && !job.playerForced));

            toil.defaultCompleteMode = ToilCompleteMode.Instant;

            return toil;
        }
    }

}
