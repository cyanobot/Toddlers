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
		public bool FromBed => job.GetTarget(TargetIndex.A).Thing is Building_Bed;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
			return this.pawn.Reserve(base.TargetA, this.job, 1, -1, null, errorOnFailed);

		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnChildLearningConditions();
			//if (pawn.Position != TargetLocA) 
			yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
			Toil toil = ToilMaker.MakeToil("MakeNewToils");
			toil.initAction = delegate ()
			{
				PawnPosture posture = PawnPosture.LayingOnGroundFaceUp;
				if (FromBed)
                {
					posture |= PawnPosture.InBedMask;
					this.KeepLyingDown(TargetIndex.A);
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
