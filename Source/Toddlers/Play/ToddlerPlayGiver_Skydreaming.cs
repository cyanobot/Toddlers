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
    //more or less copied from LearningGiver_Skydreaming
    class ToddlerPlayGiver_Skydreaming : ToddlerPlayGiver
	{
		public override bool CanDo(Pawn pawn)
		{
			IntVec3 intVec;
			//using TryFindSkygazeCell instead of TryFindSkydreamingSpotOutsideColony
			//because toddlers should be more inclined to want to stick close to people
			return base.CanDo(pawn) && RCellFinder.TryFindSkygazeCell(pawn.Position, pawn, out intVec);
		}

		public override Job TryGiveJob(Pawn pawn)
		{
			IntVec3 c;
			if (!RCellFinder.TryFindSkygazeCell(pawn.Position, pawn, out c))
			{
				return null;
			}
			return JobMaker.MakeJob(this.def.jobDef, c);
		}
		public override bool CanDoWhileDowned(Pawn pawn)
		{
			IntVec3 intVec;
			//using TryFindSkygazeCell instead of TryFindSkydreamingSpotOutsideColony
			//because toddlers should be more inclined to want to stick close to people
			return base.CanDoWhileDowned(pawn) && !pawn.Position.Roofed(pawn.Map);
		}

		public Job TryGiveJobWhileDowned(Pawn pawn)
        {
			if (pawn.Position.Roofed(pawn.Map)) return null;
			return JobMaker.MakeJob(this.def.jobDef, pawn.Position);
        }
	}
}
