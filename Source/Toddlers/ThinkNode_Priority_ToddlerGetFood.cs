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
    class ThinkNode_Priority_ToddlerGetFood : ThinkNode_Priority
	{
		private HungerCategory minCategory;
		private float maxLevelPercentage = 1f;

		public override float GetPriority(Pawn pawn)
		{
			if (!ToddlerUtility.CanFeedSelf(pawn)) return 0f;

			//copied from JobGiver_GetFood
			Need_Food food = pawn.needs.food;
			if (food == null)
			{
				return 0f;
			}
			if ((int)pawn.needs.food.CurLevel > 0 && FoodUtility.ShouldBeFedBySomeone(pawn))
			{
				return 0f;
			}
			if ((int)food.CurCategory < (int)minCategory)
			{
				return 0f;
			}
			if (food.CurLevelPercentage > maxLevelPercentage)
			{
				return 0f;
			}
			if (food.CurLevelPercentage < pawn.RaceProps.FoodLevelPercentageWantEat)
			{
				return 9.5f;
			}
			return 0f;
		}
	}
}
