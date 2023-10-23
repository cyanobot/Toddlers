using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Toddlers
{
    public class JobDriver_UndressBaby : JobDriver
    {
        int duration;
        const TargetIndex BabyIndex = TargetIndex.A;
        const TargetIndex ApparelIndex = TargetIndex.B;
		const TargetIndex StorageIndex = TargetIndex.C;
        public Pawn Baby => (Pawn)job.GetTarget(BabyIndex).Pawn;
        public Apparel Apparel => (Apparel)job.GetTarget(ApparelIndex).Thing;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref duration, "duration", 0);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(Baby, job, 1, -1, null, errorOnFailed);
		}

		public override void Notify_Starting()
        {
            base.Notify_Starting();
            duration = (int)(Apparel.GetStatValue(StatDefOf.EquipDelay) * 60f);
        }

		protected override IEnumerable<Toil> MakeNewToils()
		{
			AddFailCondition(() => !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation));
			AddFailCondition(() => Baby.DestroyedOrNull() || !Baby.Spawned || Baby.DevelopmentalStage != DevelopmentalStage.Baby || !ChildcareUtility.CanSuckle(Baby, out var _));
			AddFailCondition(() => Apparel.DestroyedOrNull());

			Log.Message("MakeNewToils for UndressBaby, TargetA: " + TargetA + ", TargetB: " + TargetB);

			yield return Toils_Goto.GotoThing(BabyIndex, PathEndMode.ClosestTouch).FailOnForbidden(BabyIndex);
			Toil wait = Toils_General.Wait(duration).WithProgressBarToilDelay(BabyIndex, true);
			wait.AddPreInitAction(delegate
			{
				Log.Message("wait.PreInitAction");
				if (Baby.Awake() && ToddlerUtility.IsLiveToddler(Baby) && !ToddlerUtility.InCrib(Baby))
				{
					Log.Message("Attempting to force BeDressed job on " + Baby);
					Job beDressedJob = JobMaker.MakeJob(Toddlers_DefOf.BeDressed, wait.actor);
					job.count = 1;
					Baby.jobs.StartJob(beDressedJob, JobCondition.InterruptForced);
				}
			});
			wait.AddFinishAction(delegate
			{
				Log.Message("wait.FinishAction");
				if (Baby.CurJobDef == Toddlers_DefOf.BeDressed)
				{
					Baby.jobs.EndCurrentJob(JobCondition.Succeeded);
				}
			});

			Toil strip = new Toil()
			{
				defaultCompleteMode = ToilCompleteMode.Instant,
				initAction = delegate {
					Log.Message("strip.initAction");
					if (Baby.apparel.WornApparel.Contains(Apparel))
					{
						if (Baby.apparel.TryDrop(Apparel, out var resultingAp))
						{
							Log.Message("Success at TryDrop");
							job.targetB = resultingAp;
							if (job.haulDroppedApparel)
							{
								Log.Message("job wants to haul dropped apparel");
								resultingAp.SetForbidden(value: false, warnOnFail: false);
								StoragePriority currentPriority = StoreUtility.CurrentStoragePriorityOf(resultingAp);
								if (StoreUtility.TryFindBestBetterStoreCellFor(resultingAp, pawn, base.Map, currentPriority, pawn.Faction, out var foundCell))
								{
									job.count = resultingAp.stackCount;
									job.targetC = foundCell;
								}
								else
								{
									EndJobWith(JobCondition.Incompletable);
								}
							}
							else
							{
								Log.Message("job doesn't want to haul dropped apparel");
								EndJobWith(JobCondition.Succeeded);
							}
						}
						else
						{
							Log.Message("Fail at TryDrop");
							EndJobWith(JobCondition.Incompletable);
						}
					}
					else
					{
						Log.Message("baby no longer wearing target apparel");
						EndJobWith(JobCondition.Incompletable);
					}

				}
			};
			yield return strip;

			if (job.haulDroppedApparel)
			{
				yield return Toils_Reserve.Reserve(StorageIndex);
				yield return Toils_Reserve.Reserve(ApparelIndex);
				yield return Toils_Haul.StartCarryThing(ApparelIndex).FailOn(() => !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation));
				Toil carryToCell = Toils_Haul.CarryHauledThingToCell(StorageIndex);
				yield return carryToCell;
				yield return Toils_Haul.PlaceHauledThingInCell(StorageIndex, carryToCell, storageMode: true);
			}

			Log.Message("Ending MakeNewToils");
		}
	}
}
