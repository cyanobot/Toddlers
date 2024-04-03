using HarmonyLib;
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

namespace Toddlers
{
	class JobGiver_PlayWithSadBaby : ThinkNode_JobGiver
	{
		public override float GetPriority(Pawn pawn)
		{
			List<Thought> thoughts = new List<Thought>();
			if (pawn.needs != null && pawn.needs.mood != null && pawn.needs.mood.thoughts != null)
				pawn.needs.mood.thoughts.GetAllMoodThoughts(thoughts);
			if (thoughts.Select(x => x.def == ThoughtDefOf.CryingBaby || x.def == ThoughtDefOf.MyCryingBaby).Count() >= 1)
            {
				return 8.5f;
            }
			return 7.3f;
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!pawn.CanReserve(pawn))
				return null;
			if (pawn.WorkTagIsDisabled(WorkTags.Caring) || !pawn.workSettings.WorkIsActive(WorkTypeDefOf.Childcare))
				return null;
			Pawn baby = FindSadBaby(pawn);
			if (baby == null)
				return null;

			foreach (BabyPlayDef item in DefDatabase<BabyPlayDef>.AllDefs.InRandomOrder())
			{
				if (item.Worker.CanDo(pawn, baby))
				{
					return item.Worker.TryGiveJob(pawn, baby);
				}
			}
			return null;
		}

		private Pawn FindSadBaby(Pawn adult)
		{
			foreach (Pawn baby in adult.MapHeld.mapPawns.FreeHumanlikesOfFaction(adult.Faction))
			{
				if (!baby.Suspended && ChildcareUtility.CanSuckle(baby, out var _)
					&& (baby.Spawned || baby.CarriedBy == adult)
					&& adult.CanReserveAndReach(baby, PathEndMode.ClosestTouch, adult.NormalMaxDanger())
					&& !baby.IsForbidden(adult)
					&& baby.needs != null && baby.needs.play != null
					&& baby.needs.play.CurLevelPercentage < 0.3
					&& ((baby.needs.mood != null && baby.needs.mood.CurLevelPercentage < 0.4)
					|| (baby.MentalState != null && baby.MentalStateDef == DefDatabase<MentalStateDef>.GetNamed("Crying"))))
				{
					return baby;
				}
			}
			return null;
		}
	}
}
