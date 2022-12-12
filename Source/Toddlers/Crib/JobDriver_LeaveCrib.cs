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
    class JobDriver_LeaveCrib : JobDriver
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			pawn.Map.pawnDestinationReservationManager.Reserve(pawn, job, job.targetA.Cell);
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			bool wiggleFirst = true;
			if ((pawn.needs.food != null && pawn.needs.food.CurLevelPercentage < pawn.needs.food.PercentageThreshUrgentlyHungry)
				|| (pawn.needs.play != null && pawn.needs.play.CurLevelPercentage < 0.3f))
				wiggleFirst = false;

			if (wiggleFirst)
            {
				Toil wiggletoil = ToilMaker.MakeToil("WiggleInCrib");
				wiggletoil.defaultCompleteMode = ToilCompleteMode.Delay;
				wiggletoil.defaultDuration = ToddlerPlayUtility.PlayDuration;
				wiggletoil.AddPreInitAction(delegate ()
				{
					pawn.jobs.posture = PawnPosture.InBedMask;
					pawn.Rotation = Rot4.South;
					job.reportStringOverride = "wiggling.";
				});
				wiggletoil.AddFinishAction(delegate ()
				{
					job.reportStringOverride = "climbing out of crib.";
				});
				wiggletoil.handlingFacing = true;
				yield return wiggletoil;
			}

			//copied from JobDriver_Goto and trimmed down
			LocalTargetInfo lookAtTarget = job.GetTarget(TargetIndex.B);
			Toil toil = Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
			if (lookAtTarget.IsValid)
			{
				toil.tickAction = (Action)Delegate.Combine(toil.tickAction, (Action)delegate
				{
					pawn.rotationTracker.FaceCell(lookAtTarget.Cell);
				});
				toil.handlingFacing = true;
			}
			yield return toil;
			Toil toil2 = ToilMaker.MakeToil("MakeNewToils");
			toil2.initAction = delegate
			{
				if (pawn.mindState != null && pawn.mindState.forcedGotoPosition == base.TargetA.Cell)
				{
					pawn.mindState.forcedGotoPosition = IntVec3.Invalid;
				}
			};
			toil2.defaultCompleteMode = ToilCompleteMode.Instant;
			yield return toil2;
		}
	}
}
