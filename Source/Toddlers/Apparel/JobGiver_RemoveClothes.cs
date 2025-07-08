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
    class JobGiver_RemoveClothes : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!(pawn.MentalState is MentalState_RemoveClothes mentalState_RemoveClothes) || mentalState_RemoveClothes.target == null)
			{
				return null;
			}
			Job job = JobMaker.MakeJob(Toddlers_DefOf.ToddlerRemoveApparel, mentalState_RemoveClothes.target);
			job.haulDroppedApparel = false;
			return job;
		}
	}
}
