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
			return base.CanDo(pawn) && JoyUtility.EnjoyableOutsideNow(pawn.Map)
				&& RCellFinder.TryFindSkygazeCell(pawn.Position, pawn, out intVec);
		}

		public override Job TryGiveJob(Pawn pawn)
		{
			IntVec3 c;
			if (!TryFindToddlerSkydreamCell(pawn.Position, pawn, out c))
			{
				return null;
			}
			return JobMaker.MakeJob(this.def.jobDef, c);
		}
		public override bool CanDoFromCrib(Pawn pawn)
		{
			//using TryFindSkygazeCell instead of TryFindSkydreamingSpotOutsideColony
			//because toddlers should be more inclined to want to stick close to people
			return base.CanDoFromCrib(pawn) && !pawn.Position.Roofed(pawn.Map);
		}

		public Job TryGiveJobFromCrib(Pawn pawn)
        {
			if (pawn.Position.Roofed(pawn.Map)) return null;
			if (pawn.InBed()) return JobMaker.MakeJob(this.def.jobDef, pawn.CurrentBed());
			return JobMaker.MakeJob(this.def.jobDef, pawn.Position);
        }

		public static bool TryFindToddlerSkydreamCell(IntVec3 root, Pawn searcher, out IntVec3 result)
		{
			Predicate<IntVec3> cellValidator = (IntVec3 c) => 
				!c.Roofed(searcher.Map) 
				&& !c.GetTerrain(searcher.Map).avoidWander
				&& searcher.SafeTemperatureAtCell(c, searcher.Map)
				;
			IntVec3 result3;
			Predicate<Region> validator = (Region r) => 
				r.Room.PsychologicallyOutdoors 
				&& !r.IsForbiddenEntirely(searcher) 
				&& searcher.SafeTemperatureRange().Includes(r.Room.Temperature)
				&& r.TryFindRandomCellInRegionUnforbidden(searcher, cellValidator, out result3);
			TraverseParms traverseParms = TraverseParms.For(searcher);
			if (!CellFinder.TryFindClosestRegionWith(root.GetRegion(searcher.Map), traverseParms, validator, 45, out var result2))
			{
				result = root;
				return false;
			}
			return CellFinder.RandomRegionNear(result2, 14, traverseParms, validator, searcher).TryFindRandomCellInRegionUnforbidden(searcher, cellValidator, out result);
		}
	}
}
