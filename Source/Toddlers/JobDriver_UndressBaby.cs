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
    class JobDriver_UndressBaby : JobDriver
    {
        private int duration;

        private Pawn Baby => TargetA.Pawn;
        private Apparel Apparel => TargetB.Thing as Apparel;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            //Log.Message("Fired UndressBaby.PreToilReservations");
            if (Apparel.Wearer != Baby) return false;
            return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
        }
        public override void Notify_Starting()
        {
            base.Notify_Starting();

            // Job duration based on equip time of target apparel.
            duration = (int)(Apparel.GetStatValue(StatDefOf.EquipDelay) * 60f);
            job.count = 1;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFailCondition(() => !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation));
            AddFailCondition(() => Baby.DestroyedOrNull() || !Baby.Spawned || Baby.DevelopmentalStage != DevelopmentalStage.Baby || !ChildcareUtility.CanSuckle(Baby, out var _));
            AddFailCondition(() => Apparel.DestroyedOrNull() || Apparel.Wearer != Baby || Apparel.IsBurning());

            //Go to baby
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);

            // Wait duration
            Toil wait = new Toil()
            {
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = duration,
            };
            wait.WithProgressBarToilDelay(TargetIndex.A);
            wait.FailOnDespawnedOrNull(TargetIndex.A);
            yield return wait;

			// Unequip apparel - copied from JobDriver_RemoveApparel
			yield return Toils_General.Do(delegate
			{
				if (Baby.apparel.WornApparel.Contains(Apparel))
				{
					if (Baby.apparel.TryDrop(Apparel, out var resultingAp))
					{
						job.targetB = resultingAp;
						if (job.haulDroppedApparel)
						{
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
							EndJobWith(JobCondition.Succeeded);
						}
					}
					else
					{
						EndJobWith(JobCondition.Incompletable);
					}
				}
				else
				{
					EndJobWith(JobCondition.Incompletable);
				}
			});
			if (job.haulDroppedApparel)
			{
				yield return Toils_Reserve.Reserve(TargetIndex.C);
				yield return Toils_Reserve.Reserve(TargetIndex.B);
				yield return Toils_Haul.StartCarryThing(TargetIndex.B).FailOn(() => !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation));
				Toil carryToCell = Toils_Haul.CarryHauledThingToCell(TargetIndex.C);
				yield return carryToCell;
				yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.C, carryToCell, storageMode: true);
			}
		}
    }
}
