﻿using HarmonyLib;
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
using static Toddlers.ToddlerUtility;
using static Toddlers.BabyMoveUtility;

namespace Toddlers
{
	public class WorkGiver_BringBabyToSafety : WorkGiver
	{
		public override Job NonScanJob(Pawn pawn)
		{
			Pawn baby;
			if ((baby = FindUnsafeBaby(pawn, AutofeedMode.Childcare, out BabyMoveReason moveReason)) == null)
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

			return job;
		}
	}

	public class JobGiver_BringBabyToSafety : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			Pawn baby;
			if ((baby = FindUnsafeBaby(pawn, AutofeedMode.Urgent, out BabyMoveReason moveReason)) == null)
			{
				return null;
			}

			//don't interrupt other things we're doing to the baby
			if (pawn.CurJob != null && pawn.CurJob.AnyTargetIs(baby))
			{
				return null;
			}

			AutofeedMode autofeedMode = baby.mindState.AutofeedSetting(pawn);
			//Log.Message("JobGiver_BringBabyToSafety for " + pawn + " (" + autofeedMode + ") found unsafe baby " + baby + ", moveReason: " + moveReason);

			//if we wouldn't usually do childcare work as a top priority
			//still respond to children in urgent danger
			//but otherwise no
			if ((autofeedMode != AutofeedMode.Urgent) && !(moveReason == BabyMoveReason.TemperatureDanger))
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
			//Log.Message("job.def: " + job.def + ", pI: " + job.def.playerInterruptible + ", driver: " + this + ", pI: " + this.PlayerInterruptable);
			//Log.Message("pawn.CurJob" + pawn.CurJob +
			//	"IsCurrentJobPlayerInterruptible(): " + pawn.jobs.IsCurrentJobPlayerInterruptible()
			//	+ ", forceCompleteBeforeNextJob: " + pawn.CurJob.def.forceCompleteBeforeNextJob);
			
			AddFailCondition(() => !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation));
			this.FailOnDestroyedOrNull(TargetIndex.A);
			AddFailCondition(() => !ChildcareUtility.CanSuckle(Baby, out var _));
			AddFailCondition(() => pawn.Downed);
			
			LocalTargetInfo tempSafePlace = SafePlaceForBaby(Baby, pawn, out BabyMoveReason moveReason);
			//Log.Message("tempSafePlace: " + tempSafePlace);

			if (tempSafePlace == Baby.PositionHeld && !pawn.IsCarryingPawn(Baby))
			{
				if (job.playerForced)
                {
					Messages.Message("MessageBabySafetyAlreadyBestLocation".Translate(Baby.Named("BABY")), 
						new LookTargets(pawn, Baby), MessageTypeDefOf.NeutralEvent);
				}
				this.EndJobWith(JobCondition.Succeeded);
				yield break;
            }
			if (moveReason != BabyMoveReason.TemperatureDanger)
			{
				this.FailOnForbidden(TargetIndex.A);
			}
			//Log.Message("Firing JobDriver_BringBabyToSafety_MakeNewToils, hauler: " + pawn + ", baby: " + Baby + ", moveReason: " + moveReason);

			if (!job.playerForced && Baby.Spawned && (PawnUtility.ShouldSendNotificationAbout(pawn) || PawnUtility.ShouldSendNotificationAbout(Baby)))
			{
				string text = null;
                switch (moveReason)
                {
                    case BabyMoveReason.TemperatureDanger:
						text = "MessageBabySafetyTemperatureDanger".Translate(pawn.Named("ADULT"), Baby.Named("BABY"));
						break;
                    case BabyMoveReason.TemperatureNonUrgent:
						text = "MessageBabySafetyTemperatureNonUrgent".Translate(pawn.Named("ADULT"), Baby.Named("BABY"));
						break;
                    case BabyMoveReason.Medical:
						text = "MessageBabySafetyMedical".Translate(pawn.Named("ADULT"), Baby.Named("BABY"));
						break;
                    case BabyMoveReason.OutsideZone:
						text = "MessageBabySafetyOutsideZone".Translate(pawn.Named("ADULT"), Baby.Named("BABY"));
						break;
                    case BabyMoveReason.ReturnToBed:
						text = "MessageBabySafetyReturnToBed".Translate(pawn.Named("ADULT"), Baby.Named("BABY"));
						break;
					case BabyMoveReason.Held:
						//text = "MessageBabySafetyHeld".Translate(pawn.Named("ADULT"), Baby.Named("BABY");
						text = null;
						break;
                    default:
						//text = "MesssageBabySafetyUnknown".Translate(pawn.Named("ADULT"), Baby.Named("BABY");
						text = null;
						break;
                }

				Toil toil_Message = Toils_General.Do(delegate
				{
					if (text != null)
						Messages.Message(text, new LookTargets(pawn, Baby), MessageTypeDefOf.NeutralEvent);
				});
				//toil_Message.AddPreInitAction(() => Log.Message("PreInit for toil_Message"));
				yield return toil_Message;

			}
		
			Toil toil_FindBabyDestination = FindBabyDestination();
			//toil_FindBabyDestination.AddPreInitAction(() => Log.Message("PreInit for toil_FindBabyDestination"));

			Toil toil_GoToDest = Toils_Goto.Goto(TargetIndex.B, PathEndMode.OnCell)
				.FailOnInvalidOrDestroyed(TargetIndex.B)
				.FailOnSomeonePhysicallyInteracting(TargetIndex.B)
				.FailOnDestroyedOrNull(TargetIndex.A)
				.FailOn(() => !pawn.IsCarryingPawn(Baby));
			if (moveReason != BabyMoveReason.TemperatureDanger)
			{
				toil_GoToDest.FailOnForbidden(TargetIndex.B);
			}

			Toil toil_interruptBaby = ToilMaker.MakeToil("InterruptBaby")
				.FailOnInvalidOrDestroyed(TargetIndex.A);
			toil_interruptBaby.initAction = delegate
			{
				Pawn baby = this.job.targetA.Pawn;
				if (baby.Awake())
				{
					baby.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture, null, 600), JobCondition.InterruptForced);
				}
			};


			yield return Toils_Jump.JumpIf(toil_FindBabyDestination, () => pawn.IsCarryingPawn(Baby));
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(TargetIndex.A);

			yield return toil_interruptBaby;
			yield return toil_FindBabyDestination;
			yield return Toils_Reserve.ReserveDestinationOrThing(TargetIndex.B);


			yield return Toils_Jump.JumpIf(toil_GoToDest, () => pawn.IsCarryingPawn(Baby));
			yield return Toils_Haul.StartCarryThing(TargetIndex.A, putRemainderInQueue: false);

			yield return toil_GoToDest;

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
				LocalTargetInfo dest_default = SafePlaceForBaby(Baby, pawn, out BabyMoveReason reason);
				//Log.Message("dest_default: " + dest_default + ", reason: " + reason);

				LocalTargetInfo dest_caravan = LocalTargetInfo.Invalid;
				if (CaravanFormingUtility.IsFormingCaravanOrDownedPawnToBeTakenByCaravan(Baby))
				{
					dest_caravan = JobGiver_PrepareCaravan_GatherDownedPawns.FindRandomDropCell(pawn, Baby);
				}

				if (dest_caravan.IsValid)
                {
					toil.GetActor().CurJob.SetTarget(TargetIndex.B, dest_caravan);
				}
				else
                {
					if ((reason == BabyMoveReason.None && !pawn.jobs.curJob.playerForced)
						|| dest_default.Thing == GetCurrentCrib(Baby))
                    {
						pawn.jobs.EndCurrentJob(JobCondition.Succeeded);
					}
					else if (dest_default.IsValid)
                    {
						toil.GetActor().CurJob.SetTarget(TargetIndex.B, dest_default);
					}
                    else
                    {
						//Log.Message("Toil FindBabyDestination initAction failed to find valid destination");
						pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
					}
                }
			};
			toil.defaultCompleteMode = ToilCompleteMode.Instant;
			//Log.Message("returning toil: " + toil);
			return toil;
		}
	}
}
