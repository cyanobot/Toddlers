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

namespace Toddlers
{
	class JobGiver_ToddlerSeekSafeTemperature : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!BabyTemperatureUtility.TemperatureInjury(pawn, out Hediff temperatureInjury, TemperatureInjuryStage.Initial))
			{
				return null;
            }
			IEnumerable<FloatRange> priorityRanges = BabyTemperatureUtility.PriorityRecoveryRanges(pawn, temperatureInjury);
			foreach (FloatRange tempRange in priorityRanges)
			{
				if (tempRange.Includes(pawn.AmbientTemperature) && pawn.Position.IsForbidden(pawn))
				{
					return JobMaker.MakeJob(JobDefOf.Wait_SafeTemperature, 500, checkOverrideOnExpiry: true);
				}

				if (temperatureInjury.CurStageIndex < (int)TemperatureInjuryStage.Serious)
					continue;

				Region region = BabyTemperatureUtility.ClosestAllowedRegionWithinTemperatureRange(pawn, pawn, tempRange);
				if (region != null)
				{
					return JobMaker.MakeJob(JobDefOf.GotoSafeTemperature, region.RandomCell);
				}

				region = BabyTemperatureUtility.ClosestRegionWithinTemperatureRange(pawn, pawn, tempRange);
				if (region != null)
				{
					return JobMaker.MakeJob(JobDefOf.GotoSafeTemperature, region.RandomCell);
				}
			}
			return null;
		}

	}

}