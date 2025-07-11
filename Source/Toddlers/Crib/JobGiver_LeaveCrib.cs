﻿using RimWorld;
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
#if RW_1_5
		public override float GetPriority(Pawn pawn)
		{
			if (pawn.needs == null) return 0f;
			if (HealthAIUtility.ShouldSeekMedicalRest(pawn)) return -1f;
			if (ToddlerLearningUtility.IsCrawler(pawn)) return -1f;
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
#else
        public override float GetPriority(Pawn pawn)
        {
            if (pawn.needs == null) return 0f;
            if (HealthAIUtility.ShouldSeekMedicalRest(pawn)) return -1f;
            if (ToddlerLearningUtility.IsCrawler(pawn)) return -1f;

            if (pawn.needs.food != null)
            {
                if (pawn.needs.food.CurLevelPercentage < pawn.RaceProps.FoodLevelPercentageWantEat && ToddlerLearningUtility.CanFeedSelf(pawn))
				{
					return 9f;	//higher than GetRest at max 8f
				}
            }

            if (!pawn.Awake()) return 0f;

            if (pawn.needs.play != null)
            {
                if (pawn.needs.play.CurLevel < 0.7f) return 5f;	//lower than GetRest if rest needed, higher than PlayInCrib
            }

			return 2f;	//higher than IdleInCrib, lower than PlayInCrib
        }
#endif

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
