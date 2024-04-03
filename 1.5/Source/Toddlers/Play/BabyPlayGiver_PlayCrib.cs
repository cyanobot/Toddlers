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
    class BabyPlayGiver_PlayCrib : BabyPlayGiver
	{
		private const float MaxCribDistance = 15.9f;

		public override bool CanDo(Pawn pawn, Pawn baby)
		{
			Thing crib = FindCrib(pawn, baby);
			if (crib == null) return false;
			if (!HealthAIUtility.ShouldSeekMedicalRest(baby))
				return false;
			if (!pawn.IsCarryingPawn(baby) && !pawn.CanReserveAndReach(baby, PathEndMode.Touch, Danger.Some))
				return false;
			if (!pawn.CanReach(crib, PathEndMode.Touch, Danger.Some))
				return false;
			if (!baby.CanReserve(crib))
				return false;
			return true;
		}

        public override Job TryGiveJob(Pawn pawn, Pawn baby)
        {
			Thing bed;
			if ((bed = FindCrib(pawn, baby)) != null)
            {
				Job job = JobMaker.MakeJob(def.jobDef, baby, bed);
				job.count = 1;
				return job;
            }
			return null;
        }

        public Thing FindCrib(Pawn pawn, Pawn baby)
        {
			Building_Bed bed = baby.CurrentBed();
			if (bed != null) return bed;
			bed = RestUtility.FindBedFor(baby, pawn, true);
			if (bed == null || bed.IsForbidden(pawn) || bed.IsForbidden(baby)
				|| bed.IsBurning() || baby.Position.DistanceTo(bed.Position) > MaxCribDistance)
			{
				return null;
			}
			else return bed;
        }
	}
}
