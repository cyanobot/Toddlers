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
    class JobGiver_LeaveCrib : ThinkNode_JobGiver
    {
		public override float GetPriority(Pawn pawn)
		{
			if (pawn.needs == null) return 0f;
			if (ToddlerUtility.IsCrawler(pawn)) return -1f;
			float priority = 1f;
			if (pawn.needs.food != null)
			{
				if (pawn.needs.food.CurLevelPercentage < pawn.needs.food.PercentageThreshUrgentlyHungry) priority += 9f;
				else if (pawn.needs.food.CurLevelPercentage < pawn.needs.food.PercentageThreshHungry) priority += 6f;
			}
			if (pawn.needs.play != null)
			{
				if (pawn.needs.play.CurLevel < 0.7f) priority += 5f;
			}
			return priority;
		}

        protected override Job TryGiveJob(Pawn pawn)
        {

			IntVec3 exitCell = IntVec3.Invalid;
			foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(pawn).InRandomOrder())
			{
				if (pawn.CanReserveAndReach(cell, PathEndMode.OnCell, Danger.None))
				{
					exitCell = cell;
				}
			}
			if (!exitCell.IsValid) return null;
			return JobMaker.MakeJob(Toddlers_DefOf.LeaveCrib, exitCell);
		}
    }
}
