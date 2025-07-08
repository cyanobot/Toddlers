using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Toddlers
{
    class JobGiver_OptimizeBabyApparel : ThinkNode_JobGiver
    {
		private static NeededWarmth neededWarmth;

		private static StringBuilder debugSb;

		private static List<float> wornApparelScores = new List<float>();

		private const int ApparelOptimizeCheckIntervalMin = 6000;

		private const int ApparelOptimizeCheckIntervalMax = 9000;

		private const float MinScoreGainToCare = 0.05f;

		private const float ScoreFactorIfNotReplacing = 10f;

		private static SimpleCurve InsulationColdScoreFactorCurve_NeedWarm => (SimpleCurve)typeof(JobGiver_OptimizeApparel).GetProperty("InsulationColdScoreFactorCurve_NeedWarm", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

		private static SimpleCurve HitPointsPercentScoreFactorCurve => (SimpleCurve)typeof(JobGiver_OptimizeApparel).GetProperty("HitPointsPercentScoreFactorCurve", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

		private static void SetNextOptimizeTick(Pawn pawn) => typeof(JobGiver_OptimizeApparel).GetMethod("SetNextOptimizeTick", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { pawn });

		public Pawn FindBabyToDress(Pawn hauler, AutofeedMode priorityLevel)
		{
			//Log.Message("Fired FindBabyToDress");
			if (priorityLevel == AutofeedMode.Never)
				return null;
			
			foreach (Pawn baby in hauler.MapHeld.mapPawns.FreeHumanlikesSpawnedOfFaction(hauler.Faction))
			{
				//Log.Message("Checking: " + baby);
				if (!ChildcareUtility.CanSuckle(baby, out var _) || baby.mindState.AutofeedSetting(hauler) != priorityLevel || CaravanFormingUtility.IsFormingCaravanOrDownedPawnToBeTakenByCaravan(baby))
                {
					//Log.Message(baby + " not appropriate");
					continue;
				}
					
				if (Find.TickManager.TicksGame < baby.mindState.nextApparelOptimizeTick)
				{
					//Log.Message(baby + " still on cooldown");
					continue;
				}

				//Log.Message("Found " + baby);
				return baby;
			}
			return null;

		}

		protected override Job TryGiveJob(Pawn hauler)
		{
			//Log.Message("Fired OptimizeBabyApparel.TryGiveJob");
			if (hauler.WorkTagIsDisabled(WorkTags.Caring) || !hauler.workSettings.WorkIsActive(WorkTypeDefOf.Childcare))
				return null;

			Pawn baby;
			if ((baby = FindBabyToDress(hauler, AutofeedMode.Childcare)) == null) 
				return null;

			if (baby.outfits == null)
			{
				Log.Error(string.Concat(hauler, " tried to run JobGiver_OptimizeApparel on ", baby, " who has no outfit tracker"));
				return null;
			}
			if (hauler.Faction != Faction.OfPlayer || baby.Faction != Faction.OfPlayer)
			{
				Log.Error(string.Concat(hauler, " tried to optimize apparel for baby ", baby, " but they are not both of the player faction"));
				return null;
			}
			if (!DebugViewSettings.debugApparelOptimize)
			{
				if (Find.TickManager.TicksGame < baby.mindState.nextApparelOptimizeTick)
				{
					return null;
				}
			}
			else
			{
				debugSb = new StringBuilder();
				debugSb.AppendLine(string.Concat("Scanning for ", baby, " at ", baby.Position));
			}

			ApparelPolicy curApparelPolicy = baby.outfits.CurrentApparelPolicy;
			List<Apparel> wornApparel = baby.apparel.WornApparel;
			//Log.Message("currentOutfit: " + baby.outfits.CurrentOutfit.ToString());
			//Log.Message("wornApparel: " + wornApparel.ToString());
			for (int i = 0; i < wornApparel.Count; i++)
			{
				wornApparelScores.Add(JobGiver_OptimizeApparel.ApparelScoreRaw(baby, wornApparel[i]));
				if (!curApparelPolicy.filter.Allows(wornApparel[i]) && baby.outfits.forcedHandler.AllowedToAutomaticallyDrop(wornApparel[i]) && !baby.apparel.IsLocked(wornApparel[i]))
				{
					Job job2 = JobMaker.MakeJob(JobDefOf.Strip, baby, wornApparel[i]);
					job2.haulDroppedApparel = true;
					return job2;
				}
			}

			Thing thing = null;
			float num2 = 0f;
			List<Thing> list = hauler.Map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel);
			if (list.Count == 0)
			{
				//Log.Message("Found no apparel on map");
				SetNextOptimizeTick(baby);
				return null;
			}
			neededWarmth = RimWorld.PawnApparelGenerator.CalculateNeededWarmth(baby, baby.MapHeld.Tile, GenLocalDate.Twelfth(baby));
			//Log.Message("neededWarmth:" + neededWarmth.ToString());
			wornApparelScores.Clear();
			for (int i = 0; i < wornApparel.Count; i++)
			{
				wornApparelScores.Add(JobGiver_OptimizeApparel.ApparelScoreRaw(baby, wornApparel[i]));
			}
			for (int j = 0; j < list.Count; j++)
			{
				Apparel apparel = (Apparel)list[j];
				//Log.Message("Contemplating apparel: " + apparel.ToString());
				//Log.Message("currentOutfit.filter.Allows(apparel): "+ currentOutfit.filter.Allows(apparel));
				//Log.Message("apparel.IsInAnyStorage(): " + apparel.IsInAnyStorage());
				//Log.Message("apparel.IsForbidden(hauler): " + apparel.IsForbidden(hauler));
				//Log.Message("apparel.IsForbidden(baby): " + apparel.IsForbidden(baby));
				if (curApparelPolicy.filter.Allows(apparel)
					&& apparel.IsInAnyStorage() 
					&& !apparel.IsForbidden(hauler) && !apparel.IsForbidden(baby)
					&& !apparel.IsBurning())
				{
					float num3 = JobGiver_OptimizeApparel.ApparelScoreGain(baby, apparel, wornApparelScores);
					//Log.Message("Apparel score gain:" + num3);
					if (DebugViewSettings.debugApparelOptimize)
					{
						debugSb.AppendLine(apparel.LabelCap + ": " + num3.ToString("F2"));
					}
					if (!(num3 < 0.05f) && !(num3 < num2) 
						&& (!CompBiocodable.IsBiocoded(apparel) || CompBiocodable.IsBiocodedFor(apparel, baby)) 
						&& ApparelUtility.HasPartsToWear(baby, apparel.def) 
						&& hauler.CanReserveAndReach(apparel, PathEndMode.OnCell, hauler.NormalMaxDanger()) 
						&& hauler.CanReserveAndReach(baby, PathEndMode.OnCell, hauler.NormalMaxDanger()) 
						&& apparel.def.apparel.developmentalStageFilter.Has(baby.DevelopmentalStage))
					{
						//Log.Message("picked " + apparel.ToString() + "as an option");
						thing = apparel;
						num2 = num3;
					}
				}
			}
			if (DebugViewSettings.debugApparelOptimize)
			{
				debugSb.AppendLine("BEST: " + thing);
				Log.Message(debugSb.ToString());
				debugSb = null;
			}
			if (thing == null)
			{
				SetNextOptimizeTick(baby);
				return null;
			}
			//Log.Message("Got all the way to the end");
			return JobMaker.MakeJob(Toddlers_DefOf.DressBaby, baby, thing);
		}
	}
}
