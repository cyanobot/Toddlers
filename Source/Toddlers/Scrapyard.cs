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
			if (pawn.CanReserveAndReach(cell, PathEndMode.OnCell, Danger.None))
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





[HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
class FloatMenu_Patch
{
	//copied from Injured Carry with modifications
	private static TargetingParameters ForToddler(Pawn pawn)
	{
		return new TargetingParameters
		{
			canTargetPawns = true,
			canTargetBuildings = false,
			neverTargetIncapacitated = true,
			neverTargetHostileFaction = true,
			mapObjectTargetsMustBeAutoAttackable = false,
			validator = delegate (TargetInfo targ)
			{
				if (!targ.HasThing) return false;
				Pawn toddler = targ.Thing as Pawn;
				if (toddler == null || toddler == pawn) return false;
				return ToddlerUtility.IsLiveToddler(toddler);
			}
		};
	}

	//copied from Dress Patient with modifications
	private static TargetingParameters ForBaby(Pawn pawn)
	{
		return new TargetingParameters
		{
			canTargetPawns = true,
			canTargetBuildings = false,
			neverTargetIncapacitated = false,
			neverTargetHostileFaction = true,
			mapObjectTargetsMustBeAutoAttackable = false,
			validator = delegate (TargetInfo targ)
			{
				if (!targ.HasThing) return false;
				Pawn baby = targ.Thing as Pawn;
				if (baby == null || baby == pawn) return false;
				return baby.DevelopmentalStage == DevelopmentalStage.Baby && ChildcareUtility.CanSuckle(baby, out var _);
			}
		};
	}

	//copied from Dress Patient with modifications
	private static TargetingParameters ForApparel(LocalTargetInfo targetBaby)
	{
		return new TargetingParameters
		{
			canTargetItems = true,
			mapObjectTargetsMustBeAutoAttackable = false,
			validator = delegate (TargetInfo targ)
			{
				if (!targ.HasThing) return false;
				Apparel apparel = targ.Thing as Apparel;
				//Log.Message("apparel : " + apparel.Label);
				if (apparel == null) return false;
				if (!targetBaby.HasThing) return false;
				Pawn baby = targetBaby.Thing as Pawn;
				//Log.Message("baby : " + baby.Name);
				if (baby == null) return false;
				//Log.Message("HasPartsToWear : " + ApparelUtility.HasPartsToWear(baby, apparel.def));
				if (!apparel.PawnCanWear(baby) || !ApparelUtility.HasPartsToWear(baby, apparel.def)) return false;
				return true;
			}
		};
	}

	static void Postfix(ref List<FloatMenuOption> opts, Pawn pawn, Vector3 clickPos)
	{
		Log.Message("opts.Count = " + opts.Count);
		IntVec3 c = IntVec3.FromVector3(clickPos);
		//for non-toddlers
		if (!ToddlerUtility.IsLiveToddler(pawn))
		{
			//have to be able to manipulate to do anything to a baby
			if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
			{
				foreach (LocalTargetInfo localTargetInfo1 in GenUI.TargetsAt(clickPos, ForToddler(pawn), thingsOnly: true))
				{
					Pawn toddler = (Pawn)localTargetInfo1.Thing;

					//option to let crawlers out of their cribs
					if (ToddlerUtility.InCrib(toddler) && ToddlerUtility.IsCrawler(toddler))
					{
						FloatMenuOption letOutOfCrib = new FloatMenuOption("Let " + toddler.Label + " out of crib", delegate
						{
							Building_Bed crib = ToddlerUtility.GetCurrentCrib(toddler);
							if (crib == null) return;
							Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("LetOutOfCrib"), toddler, crib);
							job.count = 1;
							pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
						}, MenuOptionPriority.Default);
						opts.Add(FloatMenuUtility.DecoratePrioritizedTask(letOutOfCrib, pawn, toddler));
					}

					//option to pick up toddlers and take them to their bed

					//patch for Injured Carry to avoid duplicate menu options
					//checks the same logic as Injured Carry
					if (Toddlers_Mod.injuredCarryLoaded)
					{
						if (HealthAIUtility.ShouldSeekMedicalRest(toddler)
							&& !toddler.IsPrisonerOfColony && !toddler.IsSlaveOfColony
							&& (!toddler.InMentalState || toddler.health.hediffSet.HasHediff(HediffDefOf.Scaria))
							&& !toddler.IsColonyMech
							&& (toddler.Faction == Faction.OfPlayer || toddler.Faction == null || !toddler.Faction.HostileTo(Faction.OfPlayer)))
							continue;
					}

					if (!toddler.InBed()
						&& pawn.CanReserveAndReach(toddler, PathEndMode.OnCell, Danger.None, 1, -1, null, ignoreOtherReservations: true)
						&& !toddler.mindState.WillJoinColonyIfRescued
					)
					{
						FloatMenuOption putInCrib = new FloatMenuOption("Put " + toddler.Label + " in crib", delegate
						{
							Building_Bed building_Bed = RestUtility.FindBedFor(toddler, pawn, checkSocialProperness: false);
							if (building_Bed == null)
							{
								building_Bed = RestUtility.FindBedFor(toddler, pawn, checkSocialProperness: false, ignoreOtherReservations: true);
							}
							if (building_Bed == null)
							{
								string t = (!toddler.RaceProps.Animal) ? ((string)"NoNonPrisonerBed".Translate()) : ((string)"NoAnimalBed".Translate());
								Messages.Message("CannotRescue".Translate() + ": " + "No bed", toddler, MessageTypeDefOf.RejectInput, historical: false);
							}
							else
							{
								Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("PutInCrib"), toddler, building_Bed);
								job.count = 1;
								pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
							}
						}, MenuOptionPriority.RescueOrCapture, null, toddler);
						if (RestUtility.FindBedFor(toddler, pawn, checkSocialProperness: false, ignoreOtherReservations: true) == null)
						{
							putInCrib.Label += " : No crib available";
							putInCrib.Disabled = true;
						}
						opts.Add(FloatMenuUtility.DecoratePrioritizedTask(putInCrib, pawn, toddler));
					}
				}
				//options for dressing and undressing babies and toddlers
				foreach (LocalTargetInfo localTargetInfo1 in GenUI.TargetsAt(clickPos, ForBaby(pawn), thingsOnly: true))
				{
					Pawn baby = (Pawn)localTargetInfo1.Thing;

					//patch for Dress Patients to avoid duplicate menu options
					//check the same logic as Dress Patients to figure out if that mod will be generating a menu option 
					if (Toddlers_Mod.dressPatientsLoaded)
					{
						if (baby.InBed()
							 && (baby.Faction == Faction.OfPlayer || baby.HostFaction == Faction.OfPlayer)
							 && (baby.guest != null ? pawn.guest.interactionMode != PrisonerInteractionModeDefOf.Execution : true)
							 && HealthAIUtility.ShouldSeekMedicalRest(baby))
							continue;
					}

					if (!pawn.CanReserveAndReach(baby, PathEndMode.ClosestTouch, Danger.None, 1, -1, null, ignoreOtherReservations: true))
						continue;

					//option to dress baby
					FloatMenuOption dressBaby = new FloatMenuOption("Dress " + baby.Label, delegate ()
					{
						Find.Targeter.BeginTargeting(ForApparel(baby), (LocalTargetInfo targetApparel) =>
						{
							//Log.Message("pawn : " + pawn.Name);
							//Log.Message("baby : " + baby.Name);
							//Log.Message("apparel : " + targetApparel.Label);
							Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("DressBaby"), baby, targetApparel);
							targetApparel.Thing.SetForbidden(false);
							pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
						});
					}, MenuOptionPriority.High);
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(dressBaby, pawn, baby));
				}
			}

		}

		//for toddlers, mostly disabling/removing options for things they can't do
		else
		{
			foreach (FloatMenuOption test in opts)
			{
				Log.Message("opt Label: " + test.Label);
			}
			int n = opts.RemoveAll(x => x.Label.Contains(pawn.LabelShort));
			Log.Message("n: " + n);
			//opts.RemoveAll(x => x.revalidateClickTarget == pawn || x.Label.Contains(pawn.LabelShort) || x.Label.Contains(pawn.LabelShortCap));

			foreach (Thing t in c.GetThingList(pawn.Map))
			{
				if (t.def.IsApparel && !ToddlerUtility.CanDressSelf(pawn))
				{
					//copied directly from source
					//this will allow us to identify the menu options related to wearing this object
					string key = "ForceWear";
					if (t.def.apparel.LastLayer.IsUtilityLayer)
					{
						key = "ForceEquipApparel";
					}
					string text = key.Translate(t.Label, t);
					//Log.Message("text = " + text);

					//disable the float menu option and tell the player why
					foreach (FloatMenuOption wear in opts.FindAll(x => x.Label.Contains(text)))
					{
						//if it's already disabled, leave it alone
						if (wear.Disabled) continue;

						wear.Label = text += " : Not old enough to dress self";
						wear.Disabled = true;
					}
				}

				if (t.def.ingestible != null && !ToddlerUtility.CanFeedSelf(pawn))
				{
					//copied directly from source
					//this will allow us to identify the menu options related to consuming this object
					string text;
					if (t.def.ingestible.ingestCommandString.NullOrEmpty())
					{
						text = "ConsumeThing".Translate(t.LabelShort, t);
					}
					else
					{
						text = t.def.ingestible.ingestCommandString.Formatted(t.LabelShort);
					}

					//disable the float menu option and tell the player why
					foreach (FloatMenuOption consume in opts.FindAll(x => x.Label.Contains(text)))
					{
						//if it's already disabled, leave it alone
						if (consume.Disabled) continue;

						consume.Label = text += " : Not old enough to feed self";
						consume.Disabled = true;
					}
				}
			}
		}
	}
}