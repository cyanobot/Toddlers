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
using Verse.AI.Group;
using static Toddlers.BabyMoveUtility;

namespace Toddlers
{
	class JobGiver_ToddlerSeekSafeTemperature : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!pawn.health.hediffSet.HasTemperatureInjury(TemperatureInjuryStage.Minor))
            {
				return null;
            }

			FloatRange safeRange = pawn.SafeTemperatureRange();
			IntVec3 pos = pawn.Position;
			Map map = pawn.Map;

			if (pos == null || map == null)
				return null;

			if (safeRange.Includes(pos.GetTemperature(map)))
			{
				//if we're outside our allowed area, it's probably in order to recover
				//so wait here
				if (pos.IsForbidden(pawn))
                {
					return JobMaker.MakeJob(JobDefOf.Wait_SafeTemperature, 500, checkOverrideOnExpiry: true);
				}

				//otherwise get on with something else
				return null;
			}

			//if we're not at a safe temperature, look for one
			//check for allowed first
			Region region = ClosestRegionWithinTemperatureRange(pawn, pawn, safeRange);
			
			//failing that, somewhere safe and disallowed
			if (region == null)
            {
				region = ClosestRegionWithinTemperatureRange(pawn, pawn, safeRange, true, true);
            }

			//failing that, somewhere better than current location
			if (region == null)
            {
				region = BestTemperatureRegion(pawn, pawn, true, true);
            }

			if (region != null)
            {
				return JobMaker.MakeJob(JobDefOf.GotoSafeTemperature, region.RandomCell);
			}

			return null;
		}

	}

}