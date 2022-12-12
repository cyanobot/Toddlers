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
	//more or less copied from JobDriver_Skydreaming
    class JobDriver_ToddlerSkydreaming : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
			return this.pawn.Reserve(base.TargetA, this.job, 1, -1, null, errorOnFailed);

		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnChildLearningConditions<JobDriver_ToddlerSkydreaming>();
			yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
			Toil toil = ToilMaker.MakeToil("MakeNewToils");
			toil.initAction = delegate ()
			{
				PawnPosture posture = PawnPosture.LayingOnGroundFaceUp;
				if (this.pawn.Position.GetThingList(pawn.Map).Find(t => t as Building_Bed != null) != null)
                {
					posture |= PawnPosture.InBedMask;
                }
				this.pawn.jobs.posture = posture;
			};
			toil.tickAction = delegate ()
			{
				ToddlerPlayUtility.ToddlerPlayTickCheckEnd(this.pawn);
			};
			toil.defaultCompleteMode = ToilCompleteMode.Delay;
			toil.defaultDuration = ToddlerPlayUtility.PlayDuration;
			toil.FailOn(() => this.pawn.Position.Roofed(this.pawn.Map));
			yield return toil;
			yield break;
		}
	}
}
