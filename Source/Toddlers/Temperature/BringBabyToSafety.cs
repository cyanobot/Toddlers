using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Toddlers
{
	public class WorkGiver_BringBabyToSafety : WorkGiver
	{
		public override Job NonScanJob(Pawn pawn)
		{
			Pawn baby;
			if ((baby = BabyTemperatureUtility.FindUnsafeBaby(pawn, AutofeedMode.Childcare, out BabyTemperatureUtility.BabyMoveReason moveReason)) == null)
			{
				return null;
			}
			AutofeedMode autofeedMode = baby.mindState.AutofeedSetting(pawn);
			//Log.Message("WorkGiver_BringBabyToSafety for " + pawn + " (" + autofeedMode + ") found unsafe baby " + baby + ", moveReason: " + moveReason);

			//pawns set to Urgent don't need this WorkGiver because the JobGiver will give them tasks
			//pawns set to Never don't need it because they don't do childcare (and if it's urgent the JobGiver will give them tasks)
			if (autofeedMode != AutofeedMode.Childcare) return null;

			Job job = JobMaker.MakeJob(JobDefOf.BringBabyToSafetyUnforced, baby);
			job.count = 1;
			//JobDriver_BringBabyToSafety driver = (JobDriver_BringBabyToSafety)job.GetCachedDriver(pawn);
			//driver.moveReason = moveReason;

			return job;
		}
	}

	public class JobGiver_BringBabyToSafety : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			Pawn baby;
			if ((baby = BabyTemperatureUtility.FindUnsafeBaby(pawn, AutofeedMode.Urgent, out BabyTemperatureUtility.BabyMoveReason moveReason)) == null)
			{
				return null;
			}
			AutofeedMode autofeedMode = baby.mindState.AutofeedSetting(pawn);
			//Log.Message("JobGiver_BringBabyToSafety for " + pawn + " (" + autofeedMode + ") found unsafe baby " + baby + ", moveReason: " + moveReason);

			//if we wouldn't usually do childcare work as a top priority
			//still respond to children in urgent danger
			//but otherwise no
			if ((autofeedMode != AutofeedMode.Urgent) && !(moveReason == BabyTemperatureUtility.BabyMoveReason.TemperatureDanger
				|| moveReason == BabyTemperatureUtility.BabyMoveReason.Medical))
			{
				return null;
			}

			Job job = JobMaker.MakeJob(JobDefOf.BringBabyToSafetyUnforced, baby);
			job.count = 1;

			//Log.Message("Fired JobGiver_BringBabyToSafety, moveReason: " + moveReason);
			return job;
		}
	}


	public class JobDriver_BringBabyToSafety : JobDriver
	{
		private const TargetIndex BabyInd = TargetIndex.A;

		private const TargetIndex SafePlaceInd = TargetIndex.B;

		private Pawn Baby => (Pawn)base.TargetThingA;

		//public BabyTemperatureUtility.BabyMoveReason moveReason;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(Baby, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			
			AddFailCondition(() => !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation));
			this.FailOnDestroyedOrNull(TargetIndex.A);
			AddFailCondition(() => !ChildcareUtility.CanSuckle(Baby, out var _));
			
			LocalTargetInfo tempSafePlace = BabyTemperatureUtility.SafePlaceForBaby(Baby, pawn, out BabyTemperatureUtility.BabyMoveReason moveReason);
			if (tempSafePlace == Baby.PositionHeld && !pawn.IsCarryingPawn(Baby))
            {
				yield break;
				this.EndJobWith(JobCondition.Succeeded);
            }
			if (moveReason != BabyTemperatureUtility.BabyMoveReason.TemperatureDanger)
			{
				this.FailOnForbidden(TargetIndex.A);
			}
			//Log.Message("Firing JobDriver_BringBabyToSafety_MakeNewToils, hauler: " + pawn + ", baby: " + Baby + ", moveReason: " + moveReason);

			if (!job.playerForced && Baby.Spawned && (PawnUtility.ShouldSendNotificationAbout(pawn) || PawnUtility.ShouldSendNotificationAbout(Baby)))
			{
				string text = null;
                switch (moveReason)
                {
                    case BabyTemperatureUtility.BabyMoveReason.TemperatureDanger:
						text = "{ADULT_labelShort} is moving {BABY_labelShort} away from life-threatening temperature.";
						break;
                    case BabyTemperatureUtility.BabyMoveReason.TemperatureUnsafe:
						text = "{ADULT_labelShort} is moving {BABY_labelShort} to a safer temperature.";
						break;
                    case BabyTemperatureUtility.BabyMoveReason.Medical:
						text = "{ADULT_labelShort} is moving {BABY_labelShort} to a medical bed.";
						break;
                    case BabyTemperatureUtility.BabyMoveReason.OutsideZone:
						//text = "{ADULT_labelShort} is moving {BABY_labelShort} back to {BABY_possessive} allowed zone.";
						break;
                    case BabyTemperatureUtility.BabyMoveReason.Sleepy:
						//text = "{ADULT_labelShort} is putting {BABY_labelShort} to bed.";
						break;
                    default:
						//text = "{ADULT_labelShort} is trying to move {BABY_labelShort} for an unknown reason.";
						break;
                }

				Toil toil_Message = Toils_General.Do(delegate
				{
					if (text != null)
						Messages.Message(text.Formatted(pawn.Named("ADULT"), Baby.Named("BABY")), new LookTargets(pawn, Baby), MessageTypeDefOf.NeutralEvent);
				});
				//toil_Message.AddPreInitAction(() => Log.Message("PreInit for toil_Message"));
				yield return toil_Message;

			}

			
			Toil toil_FindBabyDestination = FindBabyDestination();
			//toil_FindBabyDestination.AddPreInitAction(() => Log.Message("PreInit for toil_FindBabyDestination"));

			yield return Toils_Jump.JumpIf(toil_FindBabyDestination, () => pawn.IsCarryingPawn(Baby)).FailOn(() => !pawn.IsCarryingPawn(Baby) && (pawn.Downed || pawn.Drafted));
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
			yield return Toils_Haul.StartCarryThing(TargetIndex.A, putRemainderInQueue: false);

			
			yield return toil_FindBabyDestination;
			yield return Toils_Reserve.ReserveDestinationOrThing(TargetIndex.B);
			
			Toil goTo = Toils_Goto.Goto(TargetIndex.B, PathEndMode.OnCell)	
				.FailOnInvalidOrDestroyed(TargetIndex.B)
				.FailOnSomeonePhysicallyInteracting(TargetIndex.B)
				.FailOnDestroyedOrNull(TargetIndex.A)
				.FailOn(() => !pawn.IsCarryingPawn(Baby) || pawn.Downed || pawn.Drafted);
			if (moveReason != BabyTemperatureUtility.BabyMoveReason.TemperatureDanger)
            {
				goTo.FailOnForbidden(TargetIndex.B);
			}
			
			yield return goTo;

			yield return Toils_Reserve.ReleaseDestinationOrThing(TargetIndex.B);
			yield return Toils_Bed.TuckIntoBed(TargetIndex.B, TargetIndex.A);
			
			//Log.Message("Finished MakeToils");
		}

		private Toil FindBabyDestination()
		{
			//Log.Message("Toil FindBabyDestination firing");
			Toil toil = ToilMaker.MakeToil("FindBabyDestination");
			toil.initAction = delegate
			{
				//Log.Message("Toil FindBabyDestination initAction firing");
				LocalTargetInfo dest_default = BabyTemperatureUtility.SafePlaceForBaby(Baby, pawn, out var _);
				LocalTargetInfo dest_caravan = LocalTargetInfo.Invalid;
				if (CaravanFormingUtility.IsFormingCaravanOrDownedPawnToBeTakenByCaravan(Baby))
				{
					dest_caravan = JobGiver_PrepareCaravan_GatherDownedPawns.FindRandomDropCell(pawn, Baby);
				}
				if (dest_default.IsValid)
				{
					if (dest_caravan.IsValid && dest_caravan.Cell.DistanceTo(pawn.Position) < dest_default.Cell.DistanceTo(pawn.Position))
					{
						toil.GetActor().CurJob.SetTarget(TargetIndex.B, dest_caravan);
					}
					else
					{
						toil.GetActor().CurJob.SetTarget(TargetIndex.B, dest_default);
					}
				}
				else if (dest_caravan.IsValid)
				{
					toil.GetActor().CurJob.SetTarget(TargetIndex.B, dest_caravan);
				}
				else
				{
					//Log.Message("Toil FindBabyDestination initAction failed to find valid destination");
					pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
				}
			};
			toil.FailOn(() => !pawn.IsCarryingPawn(Baby) || pawn.Downed || pawn.Drafted);
			toil.defaultCompleteMode = ToilCompleteMode.Instant;
			return toil;
		}
	}
}
