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
    class JobDriver_ToddlerRemoveApparel : JobDriver
	{
		private int duration;

		private const TargetIndex ApparelInd = TargetIndex.A;

		private Apparel Apparel => (Apparel)job.GetTarget(ApparelInd).Thing;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref duration, "duration", 0);
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return true;
		}

		public override void Notify_Starting()
		{
			base.Notify_Starting();
			duration = (int)(Apparel.GetStatValue(StatDefOf.EquipDelay) * 60f);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDestroyedOrNull(TargetIndex.A);
			yield return Toils_General.Wait(duration).WithProgressBarToilDelay(TargetIndex.A);
			yield return Toils_General.Do(delegate
			{
				if (pawn.apparel.WornApparel.Contains(Apparel))
				{
					if (pawn.apparel.TryDrop(Apparel, out var _, pawn.PositionHeld,false))
					{
						EndJobWith(JobCondition.Succeeded);
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
		}
	}
}
