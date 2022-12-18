using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//code that did not work or was otherwise removed
//here so that I can reuse parts of it in future



class JobGiver_LeaveCrib : ThinkNode_JobGiver
{
	public override float GetPriority(Pawn pawn)
	{
		if (pawn.needs == null) return 0f;
		if (ToddlerUtility.IsCrawler(pawn)) return -99f;
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
		Building_Bed crib = pawn.CurrentBed();
		if (crib == null) return null;

		IntVec3 exitCell;
		if (!TryFindExitCell(pawn, out exitCell))
		{
			return null;
		}
		return JobMaker.MakeJob(Toddlers_DefOf.LeaveCrib, crib, exitCell);
	}

	private bool TryFindExitCell(Pawn pawn, out IntVec3 exitCell)
	{
		foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(pawn).InRandomOrder())
		{
			if (pawn.CanReserveAndReach(cell, PathEndMode.OnCell, Danger.Some))
			{
				exitCell = cell;
				return true;
			}
		}
		exitCell = IntVec3.Invalid;
		return false;
	}
}



class JobDriver_GetIntoCrib : JobDriver
{
	public Building_Bed Bed => TargetA.Thing as Building_Bed;

	public virtual bool CanSleep => true;

	public virtual bool CanRest => true;

	public virtual bool LookForOtherJobs => true;

	public Vector3 fullVector;
	public int ticksRequired = 180;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		if (Bed != null && !pawn.Reserve(Bed, job, Bed.SleepingSlotsCount, 0, null, errorOnFailed))
		{
			return false;
		}
		return true;
	}

	public override Vector3 ForcedBodyOffset
	{
		get
		{
			if (this.CurToilString != "ClimbIn") return Vector3.zero;
			float percentDone = (float)(ticksRequired - ticksLeftThisToil) / (float)ticksRequired;
			Vector3 outVector = percentDone * fullVector;
			return outVector;
		}
	}
	protected override IEnumerable<Toil> MakeNewToils()
	{
		AddFailCondition(() => pawn.Downed || !(pawn.ParentHolder is Map));

		Building_Bed crib = TargetA.Thing as Building_Bed;
		AddFailCondition(() => crib.DestroyedOrNull() || !crib.Spawned || !pawn.CanReach(crib, PathEndMode.OnCell, Danger.Deadly));

		yield return Toils_Bed.ClaimBedIfNonMedical(TargetIndex.A);

		IntVec3 entryCell;
		RCellFinder.TryFindGoodAdjacentSpotToTouch(pawn, crib, out entryCell);
		AddFailCondition(() => entryCell == null || !pawn.CanReach(entryCell, PathEndMode.OnCell, Danger.Some));
		yield return Toils_Goto.GotoCell(entryCell, PathEndMode.OnCell);

		fullVector = crib.Position.ToVector3() - entryCell.ToVector3();

		Toil climbIn = ToilMaker.MakeToil("ClimbIn");
		climbIn.defaultCompleteMode = ToilCompleteMode.Delay;
		climbIn.defaultDuration = ticksRequired;
		climbIn.handlingFacing = true;
		climbIn.initAction = delegate
		{
		};
		climbIn.tickAction = delegate
		{
			this.pawn.rotationTracker.FaceCell(crib.Position);
		};
		climbIn.AddFinishAction(delegate
		{
			pawn.SetPositionDirect(crib.Position);
			this.pawn.jobs.posture = PawnPosture.LayingInBed;
			pawn.Drawer.tweener.ResetTweenedPosToRoot();
		});
		yield return climbIn;

		//yield return Toils_LayDown.LayDown(TargetIndex.A, true, true);
	}
}

class JobDriver_LeaveCrib : JobDriver
{
	public Vector3 fullVector;
	public int ticksRequired = 180;

	public override Vector3 ForcedBodyOffset
	{
		get
		{
			float percentDone = (float)(ticksRequired - ticksLeftThisToil) / (float)ticksRequired;
			Vector3 outVector = percentDone * fullVector;
			return outVector;
		}
	}

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return true;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		//Log.Message("Fired LeaveCrib MakeNewToils");

		Building_Bed crib = TargetA.Thing as Building_Bed;
		IntVec3 exitCell = TargetB.Cell;
		AddFailCondition(() => pawn.Downed || !(pawn.ParentHolder is Map));
		AddFailCondition(() => crib.DestroyedOrNull() || !crib.Spawned);
		AddFailCondition(() => !pawn.CanReach(exitCell, PathEndMode.OnCell, Danger.Deadly));

		//Log.Message("crib : " + crib.ToString());

		fullVector = exitCell.ToVector3() - crib.Position.ToVector3();

		Toil climbOut = ToilMaker.MakeToil("LeaveCrib");
		climbOut.defaultCompleteMode = ToilCompleteMode.Delay;
		climbOut.defaultDuration = ticksRequired;
		climbOut.handlingFacing = true;
		climbOut.initAction = delegate
		{
		};
		climbOut.tickAction = delegate
		{
			this.pawn.rotationTracker.FaceCell(exitCell);
		};
		climbOut.AddFinishAction(delegate
		{
			this.pawn.jobs.posture = PawnPosture.Standing;
			pawn.SetPositionDirect(exitCell);
			pawn.Drawer.tweener.ResetTweenedPosToRoot();
		});
		yield return climbOut;

		yield break;
	}


}

			   //must be capable of manipulation to do anything to a toddler
			   if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
			   {
				   foreach (Thing t in c.GetThingList(pawn.Map))
				   {
					   Pawn toddler = t as Pawn;
					   if (toddler == null || !ToddlerUtility.IsLiveToddler(toddler)) continue;

					   //drafted pawns can pick babies up
					   if (pawn.Drafted)
					   {
						   FloatMenuOption pickUp = (pawn.CanReach(toddler, PathEndMode.ClosestTouch, Danger.Deadly) ? 
							   new FloatMenuOption("Carry".Translate(toddler), delegate {
								   Job job = JobMaker.MakeJob(Toddlers_DefOf.CarryToddler, toddler);
								   job.count = 1;
								   pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
							   },MenuOptionPriority.RescueOrCapture) :
							   new FloatMenuOption("CannotCarry".Translate(toddler) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
						   opts.Add(pickUp);
					   }

					   //pawns can babies and toddlers to their cribs
					   if (toddler.InBed() || !pawn.CanReserveAndReach(toddler, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, ignoreOtherReservations: true))
					   {
						   continue;
					   }
					   //check for is already rescuable goes here
					   Building_Bed crib = RestUtility.FindBedFor(toddler, pawn, checkSocialProperness: false);
					   //if (crib == null) crib = RestUtility.FindBedFor(toddler, pawn, checkSocialProperness: false, ignoreOtherReservations: true);

					   FloatMenuOption putInCrib = new FloatMenuOption("Put " + toddler.NameShortColored + " in crib", delegate {
						   Job job = JobMaker.MakeJob(Toddlers_DefOf.PutInCrib, toddler, crib);
						   job.count = 1;
						   pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
					   }, MenuOptionPriority.RescueOrCapture, null, toddler);
					   if (crib == null)
					   {
						   putInCrib.Label += " : No crib available";
						   putInCrib.Disabled = true;
					   }
					   if (!pawn.CanReach(toddler,PathEndMode.ClosestTouch,Danger.Deadly) || !pawn.CanReach(crib, PathEndMode.Touch, Danger.Deadly))
					   {
						   putInCrib.Label += ": " + "NoPath".Translate().CapitalizeFirst();
						   putInCrib.Disabled = true;
					   }
					   opts.Add(putInCrib);

				   }
			   }



class JobDriver_UndressBaby : JobDriver
{
	private Pawn Baby => TargetA.Pawn;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		//Log.Message("Fired UndressBaby.PreToilReservations");
		return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
	}
	public override void Notify_Starting()
	{
		base.Notify_Starting();
		job.count = 1;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		AddFailCondition(() => !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation));
		AddFailCondition(() => Baby.DestroyedOrNull() || !Baby.Spawned || Baby.DevelopmentalStage != DevelopmentalStage.Baby || !ChildcareUtility.CanSuckle(Baby, out var _));
		AddFailCondition(() => Baby.apparel == null);

		//Go to baby
		Toil goToBaby = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
		goToBaby.AddFinishAction(delegate
		{
			Pawn baby = (Pawn)goToBaby.actor.jobs.curJob.GetTarget(TargetIndex.A).Thing;
			if (baby.Awake() && ToddlerUtility.IsLiveToddler(baby) && !ToddlerUtility.InCrib(baby))
			{
				Job beDressedJob = JobMaker.MakeJob(Toddlers_DefOf.BeDressed, goToBaby.actor);
				job.count = 1;
				baby.jobs.StartJob(beDressedJob, JobCondition.InterruptForced);
			}
		});
		yield return goToBaby;

		foreach (Apparel apparel in Baby.apparel.WornApparel)
		{
			Toil wait = new Toil()
			{
				defaultCompleteMode = ToilCompleteMode.Delay,
				defaultDuration = (int)(apparel.GetStatValue(StatDefOf.EquipDelay) * 60f),
			};
			wait.WithProgressBarToilDelay(TargetIndex.A);
			wait.FailOnDespawnedOrNull(TargetIndex.A);
			yield return wait;

			yield return Toils_General.Do(delegate
			{
				if (Baby.apparel.WornApparel.Contains(apparel))
				{
					if (Baby.apparel.TryDrop(apparel, out var resultingAp))
					{
					}
					else
					{
						EndJobWith(JobCondition.Incompletable);
					}
				}
				else
				{
					EndJobWith(JobCondition.Incompletable);
				}
			});

		}

		Toil finish = new Toil();
		finish.defaultCompleteMode = ToilCompleteMode.Instant;
		finish.AddFinishAction(delegate
		{
			Pawn baby = (Pawn)finish.actor.jobs.curJob.GetTarget(TargetIndex.A).Thing;
			if (baby.CurJobDef == Toddlers_DefOf.BeDressed)
			{
				baby.jobs.EndCurrentJob(JobCondition.Succeeded);
			}
		});
		yield return finish;

	}
}


/*
//for logging purposes
[HarmonyPatch(typeof(SkillDef), nameof(SkillDef.IsDisabled))]
class IsDisabled_Patch
{
	static bool Prefix(ref bool __result, SkillDef __instance, WorkTags combinedDisabledWorkTags, IEnumerable<WorkTypeDef> disabledWorkTypes, WorkTags ___disablingWorkTags, bool ___neverDisabledBasedOnWorkTypes)
	{
		bool logFlag = false;

		if (__instance == SkillDefOf.Social)
		{
			Log.Message("Fired IsDisabled for Social");
			logFlag = true;
		}

		if (logFlag) Log.Message("combinedDisabledWorkTags: " + combinedDisabledWorkTags.ToString());
		if (logFlag) Log.Message("disablingWorkTags: " + ___disablingWorkTags.ToString());

		if ((combinedDisabledWorkTags & ___disablingWorkTags) != 0)
		{
			__result = true;
			return false;
		}

		if (logFlag) Log.Message("neverDisabledBasedOnWorkTypes: " + ___neverDisabledBasedOnWorkTypes.ToString());
		if (___neverDisabledBasedOnWorkTypes)
		{
			__result = false;
			return false;
		}

		if (logFlag) Log.Message("disabledWorkTypes: " + String.Concat(disabledWorkTypes.ToList()));
		List<WorkTypeDef> allDefsListForReading = DefDatabase<WorkTypeDef>.AllDefsListForReading;
		bool flag = false;
		for (int i = 0; i < allDefsListForReading.Count; i++)
		{
			WorkTypeDef workTypeDef = allDefsListForReading[i];
			for (int j = 0; j < workTypeDef.relevantSkills.Count; j++)
			{
				if (workTypeDef.relevantSkills[j] == __instance)
				{
					if (logFlag) Log.Message("relevant workType: " + workTypeDef.defName);
					if (!disabledWorkTypes.Contains(workTypeDef))
					{
						__result = false;
						return false;
					}
					flag = true;
				}
			}
		}
		if (!flag)
		{
			__result = false;
			return false;
		}
		__result = true;
		return false;
	}
}

//for logging purposes
[HarmonyPatch(typeof(SkillRecord))]
class TotallyDisabled_Patch
{
	public static MethodBase TargetMethod()
	{
		return typeof(SkillRecord).GetProperty(nameof(SkillRecord.TotallyDisabled), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(false);
	}

	static void Postfix(SkillRecord __instance, Pawn ___pawn, SkillDef ___def, BoolUnknown ___cachedTotallyDisabled)
	{
		if (___def == SkillDefOf.Social && ToddlerUtility.IsToddler(___pawn))
		{
			Log.Message("Fired TotallyDisabled for Social for toddler: " + ___pawn);
			Log.Message("cachedTotallyDisabled: " + ___cachedTotallyDisabled);
		}
	}
}


//for logging purposes
[HarmonyPatch(typeof(SkillRecord), "CalculateTotallyDisabled")]
class CalculateTotallyDisabled_Patch
{
	static void Postfix(SkillRecord __instance, Pawn ___pawn, SkillDef ___def, bool __result)
	{
		if (___def == SkillDefOf.Social && ToddlerUtility.IsToddler(___pawn))
		{
			Log.Message("Fired CalculateTotallyDisabled for Social for toddler: " + ___pawn);
			Log.Message("result: " + __result);
		}
	}
}
*/
