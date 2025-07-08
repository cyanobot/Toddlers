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







[HarmonyPatch]
class TestPatch
{
	static IEnumerable<MethodBase> TargetMethods()
	{
		yield return AccessTools.Method(typeof(PawnRenderer), "RenderPawnInternal");
	}
	static void Prepare(MethodBase original)
	{
		//Log.Message("Prepare, original: " + original);
	}
	static void Postfix(MethodInfo __originalMethod, object[] __args)
	{
		Log.Message(__originalMethod.DeclaringType + "." + __originalMethod.Name + " fired, args: " + __args.ToStringSafeEnumerable());
	}
}


[HarmonyPatch]
class TestPatch_GetHumanlikeMeshSets
{
	static IEnumerable<MethodBase> TargetMethods()
	{
		yield return AccessTools.Method(typeof(HumanlikeMeshPoolUtility), nameof(HumanlikeMeshPoolUtility.GetHumanlikeBodySetForPawn));
		yield return AccessTools.Method(typeof(HumanlikeMeshPoolUtility), nameof(HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn));
		yield return AccessTools.Method(typeof(HumanlikeMeshPoolUtility), nameof(HumanlikeMeshPoolUtility.GetHumanlikeHairSetForPawn));
	}

	static void Postfix(MethodInfo __originalMethod, Pawn pawn, GraphicMeshSet __result)
	{
		Log.Message(__originalMethod.Name + " fired, pawn: " + pawn + ", result: " + __result + ", mesh(south).vertices: " + __result.MeshAt(Rot4.South).vertices.ToStringSafeEnumerable(), false);
	}
}

[HarmonyPatch(typeof(PawnRenderer), "RenderPawnInternal")]
class TestPatch_RenderPawnInternal
{
	static void Postfix(PawnGraphicSet ___graphics)
	{
		List<ApparelGraphicRecord> apparelGraphics = ___graphics.apparelGraphics;
		Log.Message("RenderPawnInternal finished, apparelGraphics: " + apparelGraphics.ToStringSafeEnumerable());
		ApparelGraphicRecord apGrap0 = apparelGraphics[0];
		Log.Message("apparelGraphics[0].sourceApparel: " + apGrap0.sourceApparel + ", .graphic: " + apGrap0.graphic);
		Graphic graphic = apGrap0.graphic;
		Log.Message("graphic.drawSize: " + graphic.drawSize + ", mesh(south).vertices: " + graphic.MeshAt(Rot4.South).vertices.ToStringSafeEnumerable());
	}
}

[HarmonyPatch(typeof(PawnRenderer), "DrawHeadHair")]
//[HarmonyDebug]
class DrawHeadHair_Patch
{
	static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
	{

		//foreach (CodeInstruction instruction in instructions)
		//{
		//    yield return instruction;
		//}
		//yield break;


		//DrawHeadHair(Vector3 rootLoc, Vector3 headOffset, float angle, Rot4 bodyFacing, Rot4 headFacing, RotDrawMode bodyDrawType, PawnRenderFlags flags, bool bodyDrawn)

		string outerName = "DrawHeadHair";
		string innerName = "DrawApparel";

		string nameStart = "<" + outerName + ">g__" + innerName + "|";

		MethodInfo m_DrawApparel = typeof(PawnRenderer).GetNestedTypes(BindingFlags.NonPublic)
			.Where(t => t.GetCustomAttribute<CompilerGeneratedAttribute>() != null)
			.SelectMany(t => t.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
			.Where(x => x.Name.StartsWith(nameStart))
			.Single();
		MethodInfo m_DrawApparelFake = AccessTools.Method(typeof(DrawHeadHair_Patch), nameof(DrawHeadHair_Patch.DrawApparelFake));
		MethodInfo m_IsHumanBaby = AccessTools.Method(typeof(DrawHeadHair_Patch), nameof(DrawHeadHair_Patch.IsHumanBaby));

		bool found = false;
		int i = -1;
		List<Label> origLabels = new List<Label>();
		List<Label> skipLabels = new List<Label>();
		Label origLabel = il.DefineLabel();
		Label skipOrigLabel = il.DefineLabel();

		foreach (CodeInstruction instruction in instructions)
		{
			if (found)
			{
				instruction.labels.Add(skipLabels[i]);
				found = false;
			}

			if (instruction.Calls(m_DrawApparel))
			{
				found = true;
				i++;
				origLabels.Add(il.DefineLabel());
				skipLabels.Add(il.DefineLabel());

				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return new CodeInstruction(OpCodes.Call, m_IsHumanBaby);
				yield return new CodeInstruction(OpCodes.Brfalse, origLabels[i]);

				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return new CodeInstruction(OpCodes.Ldarg_1);
				yield return new CodeInstruction(OpCodes.Ldarg_2);
				yield return new CodeInstruction(OpCodes.Ldarg_3);
				yield return new CodeInstruction(OpCodes.Ldarg, 4);
				yield return new CodeInstruction(OpCodes.Ldarg, 5);
				yield return new CodeInstruction(OpCodes.Ldarg, 6);
				yield return new CodeInstruction(OpCodes.Ldarg, 7);
				yield return new CodeInstruction(OpCodes.Ldarg, 8);

				yield return new CodeInstruction(OpCodes.Call, m_DrawApparelFake);
				yield return new CodeInstruction(OpCodes.Pop);                      //got a stray this on the stack that would be needed by callvirt drawapparell but not by our static fake

				yield return new CodeInstruction(OpCodes.Br, skipLabels[i]);
				instruction.labels.Add(origLabels[i]);
				yield return instruction;

			}
			else
			{
				yield return instruction;
			}
		}
	}

	//duplicate of vanilla method
	static void DrawApparelFake(ApparelGraphicRecord apparelRecord, PawnRenderer instance, Vector3 rootLoc, Vector3 headOffset, float angle, Rot4 bodyFacing, Rot4 headFacing, RotDrawMode bodyDrawType, PawnRenderFlags flags, bool bodyDrawn)
	{
		Log.Message("DrawApparelFake");

		Vector3 onHeadLoc = rootLoc + headOffset;
		Quaternion quat = Quaternion.AngleAxis(angle, Vector3.up);
		Pawn pawn = (Pawn)instance.GetType().GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);

		Mesh mesh3 = instance.graphics.HairMeshSet.MeshAt(headFacing);
		Log.Message("mesh3 vertices: " + mesh3.vertices.ToStringSafeEnumerable());
		if (!apparelRecord.sourceApparel.def.apparel.hatRenderedFrontOfFace)
		{
			onHeadLoc.y += 0.0289575271f;
			Material material3 = apparelRecord.graphic.MatAt(bodyFacing);
			material3 = (flags.FlagSet(PawnRenderFlags.Cache) ? material3 : OverrideMaterialIfNeeded(instance, material3, pawn, flags.FlagSet(PawnRenderFlags.Portrait)));
			GenDraw.DrawMeshNowOrLater(mesh3, onHeadLoc, quat, material3, flags.FlagSet(PawnRenderFlags.DrawNow));
		}
		else
		{
			Material material4 = apparelRecord.graphic.MatAt(bodyFacing);
			material4 = (flags.FlagSet(PawnRenderFlags.Cache) ? material4 : OverrideMaterialIfNeeded(instance, material4, pawn, flags.FlagSet(PawnRenderFlags.Portrait)));
			if (apparelRecord.sourceApparel.def.apparel.hatRenderedBehindHead)
			{
				onHeadLoc.y += 0.0221660212f;
			}
			else
			{
				onHeadLoc.y += ((bodyFacing == Rot4.North && !apparelRecord.sourceApparel.def.apparel.hatRenderedAboveBody) ? 0.00289575267f : 0.03185328f);
			}
			GenDraw.DrawMeshNowOrLater(mesh3, onHeadLoc, quat, material4, flags.FlagSet(PawnRenderFlags.DrawNow));
		}
	}

	static Material OverrideMaterialIfNeeded(PawnRenderer instance, Material original, Pawn pawn, bool portrait = false)
	{
		MethodInfo origMethod = instance.GetType().GetMethod("OverrideMaterialIfNeeded", BindingFlags.Instance | BindingFlags.NonPublic);
		return (Material)origMethod.Invoke(instance, new object[] { original, pawn, portrait });
	}

	static bool IsHumanBaby(PawnRenderer instance)
	{
		return true;

		Pawn pawn = (Pawn)instance.GetType().GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);
		if (pawn.def == ThingDefOf.Human && pawn.DevelopmentalStage == DevelopmentalStage.Baby) return true;
		return false;
	}
}









public static void DoSettingsWindowContents_Old(Rect rect)
{
	Listing_Standard l = new Listing_Standard(GameFont.Small)
	{
		ColumnWidth = rect.width
	};

	l.Begin(rect);

	l.CheckboxLabeled("Custom renderer :", ref customRenderer, "[Default: on] Custom (minor) animations for crawling babies, wiggling, wobbling, etc. Turn off if you are having a compatibility issue with other mods that affect rendering.");

	l.CheckboxLabeled("Can draft toddlers :", ref canDraftToddlers, "[Default: off] Toddlers remain incapable of violence, but with this setting on they can be drafted and given orders.");

	l.Label("Play need fall factor (baby) : " + playFallFactor_Baby.ToString("F1"), tooltip: "[Default: 5.0] How fast the Play need falls");
	playFallFactor_Baby = l.Slider(playFallFactor_Baby, 0f, 50f);

	l.Label("Play need fall factor (toddler) : " + playFallFactor_Toddler.ToString("F1"), tooltip: "[Default: 5.0] How fast the Play need falls");
	playFallFactor_Toddler = l.Slider(playFallFactor_Toddler, 0f, 50f);

	l.Label("Loneliness gain rate factor : " + lonelinessGainFactor.ToString("F1"), tooltip: "[Default: 1.0] Controls how often toddlers need adult attention. Turn it up for more attention needed and down for less.");
	lonelinessGainFactor = l.Slider(lonelinessGainFactor, 0f, 10f);

	l.Label("'No expectations' mood impact : " + expectations.ToString("F0"), tooltip: "[Default: 20]");
	expectations = l.Slider(expectations, 0f, 100f);

	l.CheckboxLabeled("Baby clothes at tribal tech level :", ref tribalBabyClothes, "[Default: off] Toggles whether baby clothes require industrial tech");

	l.Label("Max Comfortable Temperature (baby) : " + maxComfortableTemperature_Baby.ToStringTemperature(), tooltip: "[Default: 30C / 86F]");
	maxComfortableTemperature_Baby = l.Slider(maxComfortableTemperature_Baby, 26f, 50f);

	l.Label("Min Comfortable Temperature (baby) : " + minComfortableTemperature_Baby.ToStringTemperature(), tooltip: "[Default: 20C / 68F]");
	minComfortableTemperature_Baby = l.Slider(minComfortableTemperature_Baby, -30f, 25f);

	l.Label("Max Comfortable Temperature (toddler) : " + maxComfortableTemperature_Toddler.ToStringTemperature(), tooltip: "[Default: 28C / 82F]");
	maxComfortableTemperature_Toddler = l.Slider(maxComfortableTemperature_Toddler, 26f, 50f);

	l.Label("Min Comfortable Temperature (toddler) : " + minComfortableTemperature_Toddler.ToStringTemperature(), tooltip: "[Default: 18C / 64F]");
	minComfortableTemperature_Toddler = l.Slider(minComfortableTemperature_Toddler, -30f, 25f);

	l.GapLine();

	l.Label("Time to fully learn (as a % of the toddler lifestage)");
	l.GapLine();

	l.Label("Walking : " + learningFactor_Walk.ToStringPercent(), tooltip: "[Default: 80%]");
	learningFactor_Walk = l.Slider(learningFactor_Walk, 0.01f, 1f);

	l.Label("Manipulation : " + learningFactor_Manipulation.ToStringPercent(), tooltip: "[Default: 80%]");
	learningFactor_Manipulation = l.Slider(learningFactor_Manipulation, 0.01f, 1f);

	l.GapLine();

	l.CheckboxLabeled("Baby talk for toddlers :", ref toddlerBabyTalk, "[Default: off] Whether toddler thoughts should be translated to goo goo ba gee");

	l.End();

	Toddlers_Init.ApplySettings();
}



public class BabyTemperatureUtility
{
	public enum BabyMoveReason
	{
		None,
		TemperatureDanger,
		TemperatureUnsafe,
		Medical,
		OutsideZone,
		Sleepy
	}

	public static float TemperatureAtBed(Building_Bed bed, Map map)
	{
		return GenTemperature.GetTemperatureForCell(bed?.Position ?? IntVec3.Invalid, map);
	}

	public static bool IsBedSafe(Building_Bed bed, Pawn pawn)
	{
		if (pawn.SafeTemperatureRange().Includes(TemperatureAtBed(bed, pawn.MapHeld))
			|| BestTemperatureForPawn(bed.Position, pawn.PositionHeld, pawn) == bed.Position
			|| !TemperatureInjury(pawn, out var _, TemperatureInjuryStage.Initial))
		{
			return true;
		}
		return false;
	}

	//returns target1 if equivalent
	public static LocalTargetInfo BestTemperatureForPawn(LocalTargetInfo target1, LocalTargetInfo target2, Pawn pawn)
	{
		IntVec3 c1 = target1.Cell;
		IntVec3 c2 = target2.Cell;
		float temp1;
		float temp2;

		if (!GenTemperature.TryGetTemperatureForCell(c1, pawn.MapHeld, out temp1) || !GenTemperature.TryGetTemperatureForCell(c2, pawn.MapHeld, out temp2)) return LocalTargetInfo.Invalid;

		FloatRange comfRange = pawn.ComfortableTemperatureRange();

		if (comfRange.Includes(temp1)) return target1;
		else if (comfRange.Includes(temp2)) return target2;

		FloatRange safeRange = pawn.SafeTemperatureRange();

		if (safeRange.Includes(temp1) && safeRange.Includes(temp2))
		{
			float distFromComf1 = Math.Abs(temp1 >= comfRange.TrueMax ? temp1 - comfRange.TrueMax : temp1 - comfRange.TrueMin);
			float distFromComf2 = Math.Abs(temp2 >= comfRange.TrueMax ? temp2 - comfRange.TrueMax : temp2 - comfRange.TrueMin);

			return distFromComf1 <= distFromComf2 ? target1 : target2;
		}
		else if (safeRange.Includes(temp1)) return target1;
		else if (safeRange.Includes(temp2)) return target2;

		float distFromSafe1 = Math.Abs(temp1 >= safeRange.TrueMax ? temp1 - safeRange.TrueMax : temp1 - safeRange.TrueMin);
		float distFromSafe2 = Math.Abs(temp2 >= safeRange.TrueMax ? temp2 - safeRange.TrueMax : temp2 - safeRange.TrueMin);

		return distFromSafe1 <= distFromSafe2 ? target1 : target2;

	}

	public static bool TemperatureInjury(Pawn pawn, out Hediff hediff, TemperatureInjuryStage minStage)
	{
		//Log.Message("pawn: " + pawn + ", minstage: " + minStage);

		if (pawn.health == null || pawn.health.hediffSet == null)
		{
			hediff = null;
			return false;
		}

		float coldSeverity = 0f;
		float hotSeverity = 0f;

		Hediff cold = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Hypothermia);
		//Log.Message("cold: " + cold);
		if (cold != null && cold.CurStageIndex >= (int)minStage)
		{
			coldSeverity = cold.Severity;
		}

		Hediff hot = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Heatstroke);
		//Log.Message("hot: " + hot);
		if (hot != null && hot.CurStageIndex >= (int)minStage)
		{
			hotSeverity = hot.Severity;
		}
		//Log.Message("coldSeverity = " + coldSeverity + ", hotSeverity = " + hotSeverity);

		if (coldSeverity > hotSeverity)
		{
			hediff = cold;
			return true;
		}
		if (hotSeverity > 0f)
		{
			hediff = hot;
			return true;
		}

		hediff = null;
		return false;
	}

	public static IEnumerable<FloatRange> PriorityRecoveryRanges(Pawn pawn, Hediff hediff)
	{
		if (hediff.def != HediffDefOf.Hypothermia && hediff.def != HediffDefOf.Heatstroke) yield break;

		bool cold = hediff.def == HediffDefOf.Hypothermia;
		FloatRange comfRange = pawn.ComfortableTemperatureRange();
		FloatRange safeRange = pawn.SafeTemperatureRange();

		yield return comfRange;

		FloatRange safeButWarm = new FloatRange(comfRange.TrueMax, safeRange.TrueMax);
		FloatRange safeButCold = new FloatRange(safeRange.TrueMin, comfRange.TrueMin);
		FloatRange aBitTooWarm = new FloatRange(safeRange.TrueMax, safeRange.TrueMax + 10f);
		FloatRange aBitTooCold = new FloatRange(safeRange.TrueMin - 10f, safeRange.TrueMin);

		if (cold)
		{
			yield return safeButWarm;
			yield return safeButCold;
			yield return aBitTooWarm;
		}
		else
		{
			yield return safeButCold;
			yield return safeButWarm;
			yield return aBitTooCold;
		}
	}

	public static FloatRange BetterTemperatureRange(Pawn pawn, float temp)
	{
		FloatRange comfRange = pawn.ComfortableTemperatureRange();
		FloatRange safeRange = pawn.SafeTemperatureRange();

		if (comfRange.Includes(temp)) return new FloatRange(temp, temp);
		if (safeRange.Includes(temp)) return comfRange;

		float distFromComf = Math.Abs(temp >= comfRange.TrueMax ? temp - comfRange.TrueMax : temp - comfRange.TrueMin);

		return new FloatRange(comfRange.TrueMin - distFromComf, comfRange.TrueMax + distFromComf);
	}

	public static Region ClosestAllowedRegion(Pawn pawn, Pawn hauler)
	{
		//if we're already in allowed area, don't need to find one
		if (!pawn.Position.IsForbidden(pawn))
		{
			return null;
		}

		//if we're currently on our way to a job that's in an allowed area, leave it be
		Job curJob = pawn.CurJob;
		if (curJob != null)
		{
			foreach (LocalTargetInfo target in new LocalTargetInfo[] { curJob.targetA, curJob.targetB, curJob.targetC })
			{
				if (target.IsValid
					&& (target.HasThing && target.Thing.Spawned && !target.Thing.Position.IsForbidden(pawn)
					|| (target.Cell.InBounds(pawn.MapHeld) && !target.Cell.IsForbidden(pawn))))
				{
					return null;
				}
			}
		}

		Region startRegion = pawn.GetRegion() ?? hauler.GetRegion();
		if (startRegion == null) return null;    //something funky going on

		TraverseParms traverseParms = TraverseParms.For(hauler);
		RegionEntryPredicate entryCondition = (Region from, Region r)
			=> r.Allows(traverseParms, isDestination: false);

		Region outRegion = null;
		RegionProcessor regionProcessor = delegate (Region r)
		{
			if (r.IsDoorway) return false;
			if (r.IsForbiddenEntirely(pawn)) return false;
			if (r.IsForbiddenEntirely(hauler)) return false;
			outRegion = r;
			return true;
		};
		RegionTraverser.BreadthFirstTraverse(startRegion, entryCondition, regionProcessor, 9999);

		return outRegion;
	}

	public static Region ClosestAllowedRegionWithinTemperatureRange(Pawn pawn, Pawn hauler, FloatRange tempRange)
	{
		Region startRegion = pawn.GetRegion() ?? hauler.GetRegion();
		if (startRegion == null) return null;    //something funky going on

		TraverseParms traverseParms = TraverseParms.For(hauler, maxDanger: Danger.Some);
		RegionEntryPredicate entryCondition = (Region from, Region r)
			=> r.Allows(traverseParms, isDestination: false);

		Region foundReg = null;
		RegionProcessor regionProcessor = delegate (Region r)
		{
			if (r.IsDoorway) return false;
			if (r.IsForbiddenEntirely(pawn)) return false;
			if (r.IsForbiddenEntirely(hauler)) return false;
			if (!tempRange.Includes(r.Room.Temperature)) return false;

			foundReg = r;
			return true;
		};

		RegionTraverser.BreadthFirstTraverse(startRegion, entryCondition, regionProcessor, 9999);
		return foundReg;
	}

	public static Region ClosestRegionWithinTemperatureRange(Pawn pawn, Pawn hauler, FloatRange tempRange, bool respectHaulerZone = true)
	{
		Region startRegion = pawn.GetRegion() ?? hauler.GetRegion();
		if (startRegion == null) return null;    //something funky going on

		TraverseParms traverseParms = TraverseParms.For(hauler, maxDanger: Danger.Some);
		RegionEntryPredicate entryCondition = (Region from, Region r)
			=> r.Allows(traverseParms, isDestination: false);

		Region foundReg = null;
		RegionProcessor regionProcessor = delegate (Region r)
		{
			if (r.IsDoorway) return false;
			if (respectHaulerZone && r.IsForbiddenEntirely(hauler)) return false;
			if (!tempRange.Includes(r.Room.Temperature)) return false;

			foundReg = r;
			return true;
		};

		RegionTraverser.BreadthFirstTraverse(startRegion, entryCondition, regionProcessor, 9999);
		return foundReg;
	}

	//near-copy of RCellFinder.SpotToStandDuringJobInRegion
	//and allows for pawn being carried
	public static IntVec3 SpotForBabyInRegion(Region region, Pawn pawn, float maxDistance, bool desperate = false, bool ignoreDanger = false, Predicate<IntVec3> extraValidator = null)
	{
		Predicate<IntVec3> validator = delegate (IntVec3 c)
		{
			//Log.Message("Testing cell: " + c);
			if ((float)(pawn.PositionHeld - c).LengthHorizontalSquared > maxDistance * maxDistance)
			{
				return false;
			}
			if (!desperate)
			{
				if (!c.Standable(pawn.MapHeld))
				{
					return false;
				}
				if (GenPlace.HaulPlaceBlockerIn(null, c, pawn.MapHeld, checkBlueprintsAndFrames: false) != null)
				{
					return false;
				}
				if (c.GetRegion(pawn.MapHeld).type == RegionType.Portal)
				{
					return false;
				}
			}
			if (!ignoreDanger && c.GetDangerFor(pawn, pawn.MapHeld) != Danger.None)
			{
				return false;
			}
			if (c.ContainsStaticFire(pawn.MapHeld) || c.ContainsTrap(pawn.MapHeld))
			{
				return false;
			}
			if (!pawn.MapHeld.pawnDestinationReservationManager.CanReserve(c, pawn))
			{
				return false;
			}
			return (extraValidator == null || extraValidator(c)) ? true : false;
		};
		region.TryFindRandomCellInRegion(validator, out var result);
		return result;
	}

	public static IntVec3 SpotForBabyInRegionUnforbidden(Region region, Pawn pawn, Pawn hauler, float maxDistance, bool desperate = false, bool ignoreDanger = false, Predicate<IntVec3> extraValidator = null)
	{
		Predicate<IntVec3> validator = delegate (IntVec3 c)
		{
			//Log.Message("Testing cell " + c + ", desperate: " + desperate + ", Standable = " + c.Standable(pawn.MapHeld));
			if ((float)(hauler.Position - c).LengthHorizontalSquared > maxDistance * maxDistance)
			{
				//Log.Message("Returning false because too far");
				return false;
			}
			if (hauler.HostFaction != null && c.GetRoom(hauler.MapHeld) != hauler.GetRoom())
			{
				//Log.Message("Returning false because wrong room");
				return false;
			}
			if (!desperate)
			{
				if (!c.Standable(pawn.MapHeld))
				{
					//Log.Message("Returning false because not standable");
					return false;
				}
				if (GenPlace.HaulPlaceBlockerIn(null, c, pawn.MapHeld, checkBlueprintsAndFrames: false) != null)
				{
					//Log.Message("Returning false because HaulPlaceBlockerIn");
					return false;
				}
				if (c.GetRegion(pawn.MapHeld).type == RegionType.Portal)
				{
					//Log.Message("Returning false because Portal");
					return false;
				}
			}
			if (!ignoreDanger && c.GetDangerFor(pawn, pawn.MapHeld) != Danger.None)
			{
				//Log.Message("Returning false because danger");
				return false;
			}
			if (c.ContainsStaticFire(pawn.MapHeld) || c.ContainsTrap(pawn.MapHeld))
			{
				//Log.Message("Returning false because on fire");
				return false;
			}
			if (!pawn.MapHeld.pawnDestinationReservationManager.CanReserve(c, pawn))
			{
				//Log.Message("Returning because cannot reserve");
				return false;
			}
			return (extraValidator == null || extraValidator(c)) ? true : false;
		};
		if (region.IsForbiddenEntirely(pawn)) return IntVec3.Invalid;
		region.TryFindRandomCellInRegionUnforbidden(pawn, validator, out var result);
		return result;
	}


	public static bool BabyNeedsMovingByHauler(Pawn baby, Pawn hauler, out Region preferredRegion, out BabyMoveReason babyMoveReason, IntVec3? positionOverride = null)
	{
		//Log.Message("Fired BabyNeedsMovingByHauler: " + baby + ", " + hauler);
		if (!ChildcareUtility.CanSuckle(baby, out var reason))
		{
			//Log.Message("!CanSuckle");
			preferredRegion = null;
			babyMoveReason = BabyMoveReason.None;
			return false;
		}
		if (!CanHaulBabyNow(hauler, baby, ignoreOtherReservations: false, out reason))
		{
			//Log.Message("!CanHaulBabyNow");
			preferredRegion = null;
			babyMoveReason = BabyMoveReason.None;
			return false;
		}
		if (baby.Drafted)
		{
			preferredRegion = null;
			babyMoveReason = BabyMoveReason.None;
			return false;
		}
		if (Find.TickManager.TicksGame < baby.mindState.lastBroughtToSafeTemperatureTick + 2500)
		{
			preferredRegion = null;
			babyMoveReason = BabyMoveReason.None;
			return false;
		}

		FloatRange comfRange = baby.ComfortableTemperatureRange();
		FloatRange safeRange = baby.SafeTemperatureRange();
		FloatRange closeToSafeRange = new FloatRange(safeRange.TrueMin - 10f, safeRange.TrueMax + 10f);

		FloatRange[] priorityTempRanges = new FloatRange[]
		{
					comfRange,
					safeRange,
					closeToSafeRange,
					new FloatRange(-100,150)    //no one ought to be putting a baby outside these values
		};

		IntVec3 rootPos = positionOverride ?? baby.PositionHeld;
		float temp = ((!positionOverride.HasValue) ? baby.AmbientTemperature : GenTemperature.GetTemperatureForCell(positionOverride.Value, baby.MapHeld));
		Region region;

		Hediff hediff;

		//if the baby is in serious danger from heatstroke/hypothermia
		//ignore zoning and prioritise opposite temperatures
		if (TemperatureInjury(baby, out hediff, TemperatureInjuryStage.Serious))
		{
			//Log.Message("Found temperature injury");
			foreach (FloatRange tempRange in PriorityRecoveryRanges(baby, hediff))
			{
				//Log.Message("Checking temperature range " + tempRange);
				//if the baby's already in the temperature band we're checking, don't move them
				if (tempRange.Includes(temp))
				{
					//Log.Message("Baby already in best temperature range");
					preferredRegion = null;
					babyMoveReason = BabyMoveReason.None;
					return false;
				}

				//otherwise look for a region in that temperature band to put the baby
				region = ClosestRegionWithinTemperatureRange(baby, hauler, tempRange, false);
				if (region != null)
				{
					//Log.Message("Found appropriate region, returning");
					preferredRegion = region;
					babyMoveReason = BabyMoveReason.TemperatureDanger;
					return true;
				}
				//Log.Message("Found no region for range");
			}

			//if there's nowhere better to put the baby, consider moving them back to their allowed zone
			//if they aren't already in it and it's not a worse temperature
			if (!ForbidUtility.InAllowedArea(rootPos, baby))
			{
				//Log.Message("Baby outside allowed area, better temperature range would be " + BetterTemperatureRange(baby,temp));
				region = ClosestAllowedRegionWithinTemperatureRange(baby, hauler, BetterTemperatureRange(baby, temp));
				if (region != null && region != baby.GetRegionHeld())
				{
					//Log.Message("Found better/equivalent region within allowed zone");
					preferredRegion = region;
					babyMoveReason = BabyMoveReason.OutsideZone;
					return true;
				}
			}

			//Log.Message("Couldn't solve temperature injury, not moving baby");
			//otherwise leave them where they are (don't move them to a worse temperature just because it's not forbidden)
			preferredRegion = null;
			babyMoveReason = BabyMoveReason.None;
			return false;
		}

		//if the baby is downed for medical reasons
		//ignore zoning to get them to a medical bed
		if (baby.Downed && HealthAIUtility.ShouldSeekMedicalRest(baby))
		{
			//Log.Message("Found baby in need of medical rest");
			Building_Bed currentBed = baby.CurrentBed();
			Building_Bed foundBed;
			if (currentBed == null || !IsBedSafe(currentBed, baby))
			{
				foundBed = RestUtility.FindBedFor(baby, hauler, true);
				//Log.Message("currentBed: " + currentBed + ", foundBed: " + foundBed + ", currentBed is null or unsafe");
				if (foundBed != null && IsBedSafe(foundBed, baby))
				{
					//Log.Message("foundBed is temperature-safe, returning it");
					preferredRegion = foundBed.GetRegion();
					babyMoveReason = BabyMoveReason.Medical;
					return true;
				}
				//Log.Message("found no temperature-safe alternative");
			}
			else
			{
				if (currentBed.Medical)
				{
					//Log.Message("baby is already in a medical bed, no need to move");
					preferredRegion = null;
					babyMoveReason = BabyMoveReason.None;
					return false;
				}
				foundBed = RestUtility.FindBedFor(baby, hauler, true);
				//Log.Message("foundBed: " + foundBed + ", currentBed is nonmedical");
				if (foundBed != null && foundBed.Medical && IsBedSafe(foundBed, baby))
				{
					//Log.Message("foundBed is medical and temperature-safe, returning it");
					preferredRegion = foundBed.GetRegion();
					babyMoveReason = BabyMoveReason.Medical;
					return true;
				}
			}
			//Log.Message("Could not find an appropriate bed for medical rest");
		}

		//if it's not urgent, check if the baby is forbidden to the pawn
		if (baby.IsForbidden(hauler))
		{
			//Log.Message("Baby is forbidden, not moving baby");
			preferredRegion = null;
			babyMoveReason = BabyMoveReason.None;
			return false;
		}

		//if it's not urgent, check if the baby is busy with something they shouldn't be removed from
		//like a ceremony 
		if (baby.GetLord() != null)
		{
			//Log.Message("Baby has LordJob, not moving baby");
			preferredRegion = null;
			babyMoveReason = BabyMoveReason.None;
			return false;
		}

		//if there is no immediate health concern
		//get the baby back into their allowed area
		//prioritising by temperature
		if (!ForbidUtility.InAllowedArea(rootPos, baby))
		{
			//Log.Message("Found baby outside allowed area");
			//if the child is outside the allowed zone because they are recovering from a heat injury
			//don't move them back inside if the environment wouldn't be suitable for continued recovery
			if (TemperatureInjury(baby, out hediff, TemperatureInjuryStage.Initial))
			{
				//Log.Message("Baby has temperature injury, checking temp before returning to allowed area");
				foreach (FloatRange tempRange in PriorityRecoveryRanges(baby, hediff).Take(4))
				{
					//Log.Message("Checking tempRange " + tempRange);
					region = ClosestAllowedRegionWithinTemperatureRange(baby, hauler, tempRange);
					if (region != null)
					{
						//Log.Message("Found an allowed zone in temperature range");
						preferredRegion = region;
						babyMoveReason = BabyMoveReason.OutsideZone;
						return true;
					}
				}

				//if there's no appropriate place, don't take them back into an unsuitable temperature
				//Log.Message("Found no appropriate temperature for recovery in allowed area, not moving baby");
				preferredRegion = null;
				babyMoveReason = BabyMoveReason.None;
				return false;
			}

			//Log.Message("Trying to pick an allowed zone by temperature");
			foreach (FloatRange tempRange in priorityTempRanges)
			{
				//Log.Message("Checking tempRange " + tempRange);
				region = ClosestAllowedRegionWithinTemperatureRange(baby, hauler, tempRange);
				if (region != null)
				{
					//Log.Message("Found an allowed zone in temperature range");
					preferredRegion = region;
					babyMoveReason = BabyMoveReason.OutsideZone;
					return true;
				}
			}
			//Log.Message("Could not find an allowed zone to move baby to");

			//if we can't move them to their allowed zone
			//check if they should be moved to a better temperature anyway
			if (!safeRange.Includes(temp))
			{
				//Log.Message("Baby outside allowed zone needs moving for temperature reasons");
				foreach (FloatRange tempRange in priorityTempRanges)
				{
					//Log.Message("Checking tempRange " + tempRange);

					if (tempRange.Includes(temp))
					{
						//Log.Message("Baby already in this tempRange, no use moving them");
						break;
					}

					region = ClosestAllowedRegionWithinTemperatureRange(baby, hauler, tempRange);
					if (region != null)
					{
						//Log.Message("Found an allowed zone in temperature range");
						preferredRegion = region;
						babyMoveReason = BabyMoveReason.TemperatureUnsafe;
						return true;
					}
				}
				//Log.Message("Could not resolve unsafe temperature");
			}
		}

		//if the baby is inside their allowed area
		else
		{
			//if they're not at a safe temperature
			//or they're a non-toddler baby not at a comf temperature and not in bed
			if (!safeRange.Includes(temp) || (baby.ageTracker.CurLifeStage == LifeStageDefOf.HumanlikeBaby && !comfRange.Includes(temp) && !baby.InBed()))
			{
				//Log.Message("Found baby inside allowed zone at unsuitable temperature");
				foreach (FloatRange tempRange in priorityTempRanges)
				{
					//Log.Message("Checking tempRange " + tempRange);

					if (tempRange.Includes(temp))
					{
						//Log.Message("Baby already in this tempRange, no use moving them");
						break;
					}

					region = ClosestAllowedRegionWithinTemperatureRange(baby, hauler, tempRange);
					if (region != null)
					{
						//Log.Message("Found an allowed zone in temperature range");
						preferredRegion = region;
						babyMoveReason = BabyMoveReason.TemperatureUnsafe;
						return true;
					}
				}
				//Log.Message("Could not resolve suboptimal temperature");
			}
		}

		//if the baby is sleepy or an infant and not in bed
		if (!baby.InBed() && (baby.needs.rest.CurLevelPercentage < 0.4f || baby.Downed))
		{
			//Log.Message("Found baby or sleepy toddler not in bed");
			//if there is a bed and it's a reasonable place to put them
			Thing bed = RestUtility.FindBedFor(baby, hauler, true);
			if (bed != null && !bed.IsForbidden(baby) && GenTemperature.SafeTemperatureAtCell(baby, bed.Position, baby.MapHeld))
			{
				//Log.Message("Found bed");
				preferredRegion = bed.GetRegion();
				babyMoveReason = BabyMoveReason.Sleepy;
				return true;
			}
			//Log.Message("Found no suitable bed");
		}

		//Log.Message("No reason to move baby");
		//otherwise no reason to move baby
		preferredRegion = null;
		babyMoveReason = BabyMoveReason.None;
		return false;

	}

	public static bool CanHaulBaby(Pawn hauler, Pawn baby, out ChildcareUtility.BreastfeedFailReason? reason, bool allowForbidden = false)
	{
		reason = null;
		if (hauler == null)
		{
			reason = ChildcareUtility.BreastfeedFailReason.HaulerNull;
		}
		else if (baby == null)
		{
			reason = ChildcareUtility.BreastfeedFailReason.BabyNull;
		}
		else if (hauler.Dead)
		{
			reason = ChildcareUtility.BreastfeedFailReason.HaulerDead;
		}
		else if (hauler.Downed)
		{
			reason = ChildcareUtility.BreastfeedFailReason.HaulerDowned;
		}
		else if (hauler.Map == null)
		{
			reason = ChildcareUtility.BreastfeedFailReason.HaulerNotOnMap;
		}
		else if (baby.MapHeld == null)
		{
			reason = ChildcareUtility.BreastfeedFailReason.BabyNotOnMap;
		}
		else if (hauler.Map != baby.MapHeld)
		{
			reason = ChildcareUtility.BreastfeedFailReason.HaulerNotOnBabyMap;
		}
		else if (baby.IsForbidden(hauler) && !allowForbidden)
		{
			reason = ChildcareUtility.BreastfeedFailReason.BabyForbiddenToHauler;
		}
		else if (!ChildcareUtility.HasBreastfeedCompatibleFactions(hauler, baby))
		{
			if (!ChildcareUtility.BabyHasFeederInCompatibleFaction(hauler.Faction, baby))
			{
				reason = ChildcareUtility.BreastfeedFailReason.BabyInIncompatibleFactionToHauler;
			}
		}
		else if (!hauler.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
		{
			reason = ChildcareUtility.BreastfeedFailReason.HaulerIncapableOfManipulation;
		}
		return !reason.HasValue;
	}

	public static bool CanHaulBabyNow(Pawn hauler, Pawn baby, bool ignoreOtherReservations, out ChildcareUtility.BreastfeedFailReason? reason, bool allowForbidden = false)
	{
		if (!CanHaulBaby(hauler, baby, out reason, true))
		{
			return false;
		}
		if (!hauler.CanReserve(baby, 1, -1, null, ignoreOtherReservations))
		{
			reason = ChildcareUtility.BreastfeedFailReason.HaulerCannotReserveBaby;
		}
		else if (!hauler.CanReach(baby, PathEndMode.Touch, Danger.Deadly))
		{
			reason = ChildcareUtility.BreastfeedFailReason.HaulerCannotReachBaby;
		}
		return !reason.HasValue;
	}

	public static LocalTargetInfo SafePlaceForBaby(Pawn baby, Pawn hauler, out BabyMoveReason moveReason)
	{
		//Log.Message("Fired SafePlaceForBaby");
		if (!ChildcareUtility.CanSuckle(baby, out var _) || !CanHaulBabyNow(hauler, baby, false, out var _, true))
		{
			//Log.Message("CanSuckle: " + ChildcareUtility.CanSuckle(baby, out var _) + ", CanHaulBabyNow: " + CanHaulBabyNow(hauler, baby, false, out var _));
			moveReason = BabyMoveReason.None;
			return LocalTargetInfo.Invalid;
		}

		IntVec3 currentCell = baby.PositionHeld;

		if (!BabyNeedsMovingByHauler(baby, hauler, out Region preferredRegion, out moveReason))
		{
			return currentCell;
		}

		//Log.Message("BabyNeedsMovingByHauler: true, preferredRegion: " + preferredRegion + ", babyMoveReason: " + moveReason);

		Thing bed = RestUtility.FindBedFor(baby, hauler, true);

		//these two reasons are only generated if there's a bed to take the baby to, so find it
		if (moveReason == BabyMoveReason.Medical || moveReason == BabyMoveReason.Sleepy)
		{
			return bed;
		}

		//if the baby's already in the right place
		if (preferredRegion == baby.GetRegionHeld())
		{
			//otherwise if the toddler isn't spawned (ie being carried), try to pick a spot near where they are currently (ie just put them down)
			if (!baby.Spawned)
			{
				//Log.Message("preferredRegion = current region, attempting to put baby down");
				return SpotForBabyInRegionUnforbidden(preferredRegion, baby, hauler, 9999f, ignoreDanger: true);
			}

			//if none of the above, just leave them where they are
			else
			{
				//Log.Message("preferredRegion = current region, returning currentCell");
				return currentCell;
			}
		}

		//if their bed is in the selected region, take them to it
		if (bed != null && preferredRegion == bed.GetRegion())
		{
			//Log.Message("found bed in preferredRegion, returning bed");
			return bed;
		}

		//find a non-forbidden spot in the selected region if possible
		IntVec3 cell = SpotForBabyInRegionUnforbidden(preferredRegion, baby, hauler, 9999f, ignoreDanger: true);
		if (cell != null && cell.IsValid)
		{
			//Log.Message("found unforbidden cell, returning: " + cell);
			return cell;
		}

		//otherwise find any spot
		cell = SpotForBabyInRegion(preferredRegion, baby, 9999f, ignoreDanger: true);
		if (cell != null && cell.IsValid)
		{
			//Log.Message("found random cell, returning");
			return cell;
		}

		//Log.Message("fell through, returning invalid");
		//fall-through
		return LocalTargetInfo.Invalid;
	}

	public static Pawn FindUnsafeBaby(Pawn hauler, AutofeedMode autofeedMode, out BabyMoveReason moveReason)
	{
		//Log.Message("Firing FindUnsafeBaby for " + hauler + ", autofeedMode: " + autofeedMode);
		foreach (Pawn pawn in hauler.MapHeld.mapPawns.FreeHumanlikesSpawnedOfFaction(hauler.Faction))
		{
			//only consider babies, that aren't being taken to a caravan
			if (!ChildcareUtility.CanSuckle(pawn, out var _)
				|| CaravanFormingUtility.IsFormingCaravanOrDownedPawnToBeTakenByCaravan(pawn))
			{
				continue;
			}

			//find out if the baby needs to go somewhere 
			//and that it isn't where they already are
			LocalTargetInfo localTargetInfo = SafePlaceForBaby(pawn, hauler, out moveReason);
			//Log.Message("moveReason: " + moveReason);
			if (moveReason == BabyMoveReason.None) continue;
			if (!localTargetInfo.IsValid)
			{
				//Log.Message("Invalid localTargetInfo");
				continue;
			}
			if (pawn.Spawned && pawn.Position == localTargetInfo.Cell)
			{
				//Log.Message("Position == localTargetInfo");
				continue;
			}
			else if (localTargetInfo.Thing is Building_Bed building_Bed)
			{
				if (pawn.CurrentBed() == building_Bed)
				{
					//Log.Message("Already in target bed");
					continue;
				}
			}

			//Log.Message("FindUnsafeBaby returning hauler: "  + hauler + ", baby: " + pawn + ", localTargetInfo: " + localTargetInfo + ", moveReason: " + moveReason);
			return pawn;
		}
		moveReason = BabyMoveReason.None;
		return null;
	}

}

class TemperatureUtility
{
	public const TemperatureInjuryStage SEEK_SAFE_MIN = TemperatureInjuryStage.Serious;
	public const TemperatureInjuryStage WAIT_SAFE_MAX = TemperatureInjuryStage.Initial;

	public enum TemperatureDirection
	{
		Cold,
		Hot
	}

	public static bool TemperatureInjury(Pawn pawn, out TemperatureDirection? direction, out TemperatureInjuryStage? stage)
	{
		//Log.Message("pawn: " + pawn + ", minstage: " + minStage);

		direction = null;
		stage = null;

		if (pawn.health == null || pawn.health.hediffSet == null)
		{
			return false;
		}

		float coldSeverity = 0f;
		float hotSeverity = 0f;

		Hediff cold = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Hypothermia);
		//Log.Message("cold: " + cold);
		if (cold != null)
		{
			coldSeverity = cold.Severity;
		}

		Hediff hot = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Heatstroke);
		//Log.Message("hot: " + hot);
		if (hot != null)
		{
			hotSeverity = hot.Severity;
		}
		//Log.Message("coldSeverity = " + coldSeverity + ", hotSeverity = " + hotSeverity);

		if (coldSeverity > hotSeverity)
		{
			direction = TemperatureDirection.Cold;
			stage = (TemperatureInjuryStage)cold.CurStageIndex;
			return true;
		}
		if (hotSeverity > 0f)
		{
			direction = TemperatureDirection.Hot;
			stage = (TemperatureInjuryStage)hot.CurStageIndex;
			return true;
		}

		return false;
	}

	public static bool ShouldSeekSafeTemperature(Pawn pawn, out TemperatureDirection? direction)
	{
		if (!TemperatureInjury(pawn, out direction, out TemperatureInjuryStage? stage))
			return false;
		if (stage >= SEEK_SAFE_MIN)
			return true;
		return false;
	}

	public static bool ShouldWaitSafeTemperature(Pawn pawn, out TemperatureDirection? direction)
	{
		if (!TemperatureInjury(pawn, out direction, out TemperatureInjuryStage? stage))
			return false;
		if (stage >= SEEK_SAFE_MIN)
			return true;
		return false;
	}

}

class TemperatureUtility_1
{
	public const float OFFSET_FOR_TEMPERATURE_INJURY = 10f;

	public enum TemperatureDirection
	{
		Null,
		Colder,
		Hotter
	}

	public static TemperatureDirection TemperatureDirectionForPawn(Pawn pawn, float temp)
	{
		FloatRange comfRange = pawn.ComfortableTemperatureRange();
		if (comfRange.Includes(temp)) return TemperatureDirection.Null;         //temperature is comfortable for pawn
		if (temp < comfRange.TrueMin) return TemperatureDirection.Colder;       //temperature is colder than comfortable
		return TemperatureDirection.Hotter;                                     //temperature is hotter than comfortable
	}

	public static float TemperatureDanger(Pawn pawn, float temp, out TemperatureDirection tempDirec, bool adjustForInjury = true)
	{
		FloatRange comfRange = pawn.ComfortableTemperatureRange();
		if (comfRange.Includes(temp))
		{
			tempDirec = TemperatureDirection.Null;
			return 0f;
		}
		float distFromComf;
		float danger;
		if (temp <= comfRange.min)
		{
			tempDirec = TemperatureDirection.Colder;
			distFromComf = Mathf.Abs(comfRange.min - temp);

			danger = distFromComf - 10f;
			if (adjustForInjury && TemperatureInjury(pawn, out Hediff hediff, TemperatureInjuryStage.Minor))
			{
				if (hediff.def == HediffDefOf.Hypothermia) danger += OFFSET_FOR_TEMPERATURE_INJURY;
				if (hediff.def == HediffDefOf.Heatstroke) danger -= OFFSET_FOR_TEMPERATURE_INJURY;
			}
			danger = Mathf.Max(danger, 0f);

			return danger;
		}
		if (temp >= comfRange.max)
		{
			tempDirec = TemperatureDirection.Hotter;
			distFromComf = Mathf.Abs(temp - comfRange.max);

			danger = distFromComf - 10f;
			if (adjustForInjury && TemperatureInjury(pawn, out Hediff hediff, TemperatureInjuryStage.Minor))
			{
				if (hediff.def == HediffDefOf.Heatstroke) danger += OFFSET_FOR_TEMPERATURE_INJURY;
				if (hediff.def == HediffDefOf.Hypothermia) danger -= OFFSET_FOR_TEMPERATURE_INJURY;
			}
			danger = Mathf.Max(danger, 0f);

			return danger;
		}

		//fall-through should not happen
		Log.Error("Toddlers.TemperatureUtility.TemperatureDanger fell through, this should not happen. pawn: " + pawn + ", temp: " + temp + ", adjustForInjury: " + adjustForInjury);
		tempDirec = TemperatureDirection.Null;
		return 0f;
	}


	public static bool TemperatureInjury(Pawn pawn, out Hediff hediff, TemperatureInjuryStage minStage)
	{
		//Log.Message("pawn: " + pawn + ", minstage: " + minStage);

		if (pawn.health == null || pawn.health.hediffSet == null)
		{
			hediff = null;
			return false;
		}

		float coldSeverity = 0f;
		float hotSeverity = 0f;

		Hediff cold = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Hypothermia);
		//Log.Message("cold: " + cold);
		if (cold != null && cold.CurStageIndex >= (int)minStage)
		{
			coldSeverity = cold.Severity;
		}

		Hediff hot = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Heatstroke);
		//Log.Message("hot: " + hot);
		if (hot != null && hot.CurStageIndex >= (int)minStage)
		{
			hotSeverity = hot.Severity;
		}
		//Log.Message("coldSeverity = " + coldSeverity + ", hotSeverity = " + hotSeverity);

		if (coldSeverity > hotSeverity)
		{
			hediff = cold;
			return true;
		}
		if (hotSeverity > 0f)
		{
			hediff = hot;
			return true;
		}

		hediff = null;
		return false;
	}

	//returns target1 if equivalent
	public static LocalTargetInfo BestTemperatureOf(LocalTargetInfo target1, LocalTargetInfo target2, Pawn pawn, out float difference, bool adjustForInjury = true)
	{
		IntVec3 c1 = target1.Cell;
		IntVec3 c2 = target2.Cell;
		float temp1;
		float temp2;

		if (!GenTemperature.TryGetTemperatureForCell(c1, pawn.MapHeld, out temp1) || !GenTemperature.TryGetTemperatureForCell(c2, pawn.MapHeld, out temp2))
		{
			Log.Error("Toddlers.TemperatureUtility.TemperatureInjury failed to get a temperature for one of its targets. target1: " + target1 + ", target2: " + target2 + ", pawn: " + pawn);
			difference = 0f;
			return LocalTargetInfo.Invalid;
		}

		float danger1 = TemperatureDanger(pawn, temp1, out var _, adjustForInjury);
		float danger2 = TemperatureDanger(pawn, temp2, out var _, adjustForInjury);

		difference = Mathf.Abs(danger1 - danger2);

		if (danger1 <= danger2) return target1;
		return target2;
	}

	public static FloatRange TemperatureRangeFromMaxDanger(Pawn pawn, float maxDanger, bool adjustForInjury = true)
	{
		FloatRange comfRange = pawn.ComfortableTemperatureRange();
		float min = comfRange.min - maxDanger;
		float max = comfRange.max + maxDanger;

		if (adjustForInjury && TemperatureInjury(pawn, out Hediff hediff, TemperatureInjuryStage.Minor))
		{
			if (hediff.def == HediffDefOf.Hypothermia)
			{
				min = Mathf.Min(comfRange.min, min + OFFSET_FOR_TEMPERATURE_INJURY);
				max += OFFSET_FOR_TEMPERATURE_INJURY;
			}
			if (hediff.def == HediffDefOf.Heatstroke)
			{
				min -= OFFSET_FOR_TEMPERATURE_INJURY;
				max = Mathf.Max(comfRange.max, max - OFFSET_FOR_TEMPERATURE_INJURY);
			}
		}

		return new FloatRange(min, max);
	}

	public static FloatRange BetterTemperatureRange(Pawn pawn, float temp, bool adjustForInjury = true)
	{
		float danger = TemperatureDanger(pawn, temp, out var _, adjustForInjury);
		if (danger == 0f) return new FloatRange(-9999f, -9999f);

		return TemperatureRangeFromMaxDanger(pawn, danger, adjustForInjury);
	}

	public static float DangerDifference(Pawn pawn, float temp1, float temp2, bool adjustForInjury = true)
	{
		float danger1 = TemperatureDanger(pawn, temp1, out var _, adjustForInjury);
		float danger2 = TemperatureDanger(pawn, temp2, out var _, adjustForInjury);

		return Mathf.Abs(danger1 - danger2);
	}

	public static Region ClosestRegionWithinTemperatureRange(Pawn pawn, Pawn hauler, FloatRange tempRange, bool respectPawnZone = true, bool respectHaulerZone = true)
	{
		Region startRegion = pawn.GetRegion() ?? hauler.GetRegion();
		if (startRegion == null) return null;    //something funky going on

		TraverseParms traverseParms = TraverseParms.For(hauler, maxDanger: Danger.Deadly);
		RegionEntryPredicate entryCondition = (Region from, Region r)
			=> r.Allows(traverseParms, isDestination: false);

		Region foundReg = null;
		RegionProcessor regionProcessor = delegate (Region r)
		{
			if (r.IsDoorway) return false;
			if (!tempRange.Includes(r.Room.Temperature)) return false;
			if (respectHaulerZone && r.IsForbiddenEntirely(hauler)) return false;
			if (respectPawnZone && r.IsForbiddenEntirely(pawn)) return false;

			foundReg = r;
			return true;
		};

		RegionTraverser.BreadthFirstTraverse(startRegion, entryCondition, regionProcessor);
		return foundReg;
	}

	public static Region BestTemperatureRegion(Pawn pawn, bool respectZone = true)
	{
		Region startRegion = pawn.GetRegionHeld();
		if (startRegion == null) return null;       //something funky going on

		TraverseParms traverseParms = TraverseParms.For(pawn, maxDanger: Danger.Deadly);
		RegionEntryPredicate entryCondition = (Region from, Region r)
			=> r.Allows(traverseParms, isDestination: false);

		Region bestRegion = startRegion;
		float bestDanger = TemperatureDanger(pawn, startRegion.Room.Temperature, out var _);

		RegionProcessor regionProcessor = delegate (Region r)
		{
			if (r.IsDoorway) return false;
			if (respectZone && r.IsForbiddenEntirely(pawn)) return false;

			float temp = r.Room.Temperature;
			float danger = TemperatureDanger(pawn, temp, out var _);

			if (danger < bestDanger)
			{
				bestDanger = danger;
				bestRegion = r;
			}
			return false;
		};

		RegionTraverser.BreadthFirstTraverse(startRegion, entryCondition, regionProcessor);
		return bestRegion;
	}
}


public partial class AlienRace
{
	public bool bodyAccountedFor_Baby = false;
	public bool bodyAccountedFor_Child = false;
	public BodyTypeDef bodyType_Baby = null;
	public BodyTypeDef bodyType_Child = null;


	public object bodyGraphic_obj;

	public void InitBodyTypes()
	{
		Log.Message("Starting InitBodyTypes");
		//if both child- and baby-specific bodies are defined using the HAR ageGraphics format
		//then we don't need to worry about bodyTypes for them
		if (bodyAccountedFor_Baby && bodyAccountedFor_Child) return;

		object bodyTypes_obj = alienPartGenerator.GetType().GetField("bodyTypes", BindingFlags.Public | BindingFlags.Instance).GetValue(alienPartGenerator);
		Log.Message("bodyTypes_obj: " + (bodyTypes_obj as IEnumerable).ToStringSafeEnumerable());

		List<BodyTypeDef> bodyTypes = bodyTypes_obj as List<BodyTypeDef>;

		foreach (BodyTypeDef bodyTypeDef in bodyTypes)
		{
			if (bodyTypeDef == BodyTypeDefOf.Baby) bodyType_Baby = BodyTypeDefOf.Baby;
			else if (bodyTypeDef.defName.Contains("baby") || bodyTypeDef.defName.Contains("Baby")) bodyType_Baby = bodyTypeDef;

			if (bodyTypeDef == BodyTypeDefOf.Child) bodyType_Child = BodyTypeDefOf.Child;
			else if (bodyTypeDef.defName.Contains("child") || bodyTypeDef.defName.Contains("Child")) bodyType_Child = bodyTypeDef;

			if (bodyType_Baby != null && bodyType_Child != null) break;
		}
		Log.Message("Body type search complete, bodyType_Baby: " + bodyType_Baby + ", bodyType_Child: " + bodyType_Child);

		//List<BodyTypeDef> vanillaBodies = new List<BodyTypeDef> { BodyTypeDefOf.Fat, BodyTypeDefOf.Female, BodyTypeDefOf.Hulk, BodyTypeDefOf.Male, BodyTypeDefOf.Thin};
		//if the race has not been allowed the baby/child body types, add those in
		if (bodyType_Baby == null)
		{
			bodyTypes.Add(BodyTypeDefOf.Baby);
			bodyType_Baby = BodyTypeDefOf.Baby;
		}
		if (bodyType_Child == null)
		{
			bodyTypes.Add(BodyTypeDefOf.Child);
			bodyType_Child = BodyTypeDefOf.Child;
		}
		Log.Message("new bodyTypes_obj: " + (bodyTypes_obj as IEnumerable).ToStringSafeEnumerable());

		//Note: bodyGraphic_obj is identified earlier in InitGraphicField when it loops through graphicPaths
		if (bodyGraphic_obj == null || !Patch_HAR.class_AbstractExtendedGraphic.IsAssignableFrom(bodyGraphic_obj.GetType()))
		{
			Log.Error("Toddlers HAR Patch failed to find extended body graphic for race: " + def.defName);
			return;
		}

		string path = (string)bodyGraphic_obj.GetType().GetField("path").GetValue(bodyGraphic_obj);
		Log.Message("path: " + path);

		//nested loop to find all nested bodyTypeGraphics objects that will need updating
		List<object> bodyTypeGraphicses = new List<object>();

		FindNestedBodyTypeVariants(bodyGraphic_obj, ref bodyTypeGraphicses);
		Log.Message("bodyTypeGraphicses: " + bodyTypeGraphicses.ToStringSafeEnumerable());

		foreach (object bodyTypeGraphics_obj in bodyTypeGraphicses)
		{
			Log.Message("bodyTypeGraphics_obj: " + bodyTypeGraphics_obj);
			IEnumerable bodyTypeGraphics_ienum = bodyTypeGraphics_obj as IEnumerable;
			Log.Message("bodyTypeGraphics_ienum: " + bodyTypeGraphics_ienum.ToStringSafeEnumerable());

			//if there aren't any graphics listed per body type, skip over this
			if (bodyTypeGraphics_ienum == null || bodyTypeGraphics_ienum.EnumerableCount() <= 0) continue;

			//establish whether there are already working graphics for baby/child
			object graphicBaby_obj = null;
			object graphicChild_obj = null;

			Log.Message("Looping through bodyTypeGraphics, first pass");
			foreach (object bodyTypeGraphic in bodyTypeGraphics_ienum)
			{
				BodyTypeDef bodyTypeDef = (BodyTypeDef)Patch_HAR.field_BodyTypeGraphic_bodyType.GetValue(bodyTypeGraphic);
				string typePath = (string)Patch_HAR.class_BodyTypeGraphic.GetMethod("GetPath", new Type[] { }).Invoke(bodyTypeGraphic, new object[] { });
				Log.Message("bodyTypeDef: " + bodyTypeDef + ", typePath: " + typePath);

				//if we've found the entry for the baby or child body type
				if (!bodyAccountedFor_Baby && bodyTypeDef == bodyType_Baby)
				{
					graphicBaby_obj = bodyTypeGraphic;
					if (!CheckTextures(path))
					{
						//Log.Message("Found broken graphic entry at " + path + " for baby");
						continue;
					}
					else
					{
						//Log.Message("Found a graphic for bodyType_Baby, no need to make a new one.");
						bodyAccountedFor_Baby = true;
					}
				}
				if (!bodyAccountedFor_Child && bodyTypeDef == bodyType_Child)
				{
					graphicChild_obj = bodyTypeGraphic;
					if (!CheckTextures(path))
					{
						//Log.Message("Found broken graphic entry at " + path + " for child");
						continue;
					}
					else
					{
						//Log.Message("Found a graphic for bodyType_Child, no need to make a new one.");
						bodyAccountedFor_Child = true;
					}
				}
				//if we've found working graphics for both babies and children, we're done here
				if (bodyAccountedFor_Baby && bodyAccountedFor_Child) continue;
			}
			Log.Message("graphicBaby_obj: " + graphicBaby_obj + ", graphicChild_obj: " + graphicChild_obj);


			//if we haven't found an existing graphic for babies/children
			//make a new one
			if (!bodyAccountedFor_Baby && graphicBaby_obj == null)
			{
				graphicBaby_obj = Activator.CreateInstance(Patch_HAR.class_BodyTypeGraphic);
				bodyTypeGraphics_obj.GetType().GetMethod("Add", new Type[] { Patch_HAR.class_BodyTypeGraphic })
						.Invoke(bodyTypeGraphics_obj, new object[] { graphicBaby_obj });
				Patch_HAR.field_BodyTypeGraphic_bodyType.SetValue(graphicBaby_obj, bodyType_Baby);
			}
			if (!bodyAccountedFor_Child && graphicChild_obj == null)
			{
				graphicChild_obj = Activator.CreateInstance(Patch_HAR.class_BodyTypeGraphic);
				bodyTypeGraphics_obj.GetType().GetMethod("Add", new Type[] { Patch_HAR.class_BodyTypeGraphic })
						.Invoke(bodyTypeGraphics_obj, new object[] { graphicChild_obj });
				Patch_HAR.field_BodyTypeGraphic_bodyType.SetValue(graphicChild_obj, bodyType_Child);
			}

			//if the race uses vanilla textures
			//then we can very easily set the baby/child graphics to the correct ones
			//Log.Message("path: " + path + ", vanilla: " + VANILLA_BODY_PATH + ", eq: " + (path == VANILLA_BODY_PATH));
			if (path == VANILLA_BODY_PATH)
			{
				Log.Message("path == VANILLA_BODY_PATH");
				if (!bodyAccountedFor_Baby)
				{
					Patch_HAR.field_BodyTypeGraphic_path.SetValue(graphicBaby_obj, VANILLA_BODY_PATH + "Naked_Child");
				}
				if (!bodyAccountedFor_Child)
				{
					Patch_HAR.field_BodyTypeGraphic_path.SetValue(graphicChild_obj, VANILLA_BODY_PATH + "Naked_Child");
				}
				continue;
			}

			//if we're not using vanilla textures it gets a little more complicated
			//loop through the available body type graphics defined in this subgraphic for the race
			//and try to pick one that will be the least strange as a baby/child body
			int priority = 0;
			int bestPriority_Baby = 0;
			object bestGraphic_Baby = null;
			int bestPriority_Child = 0;
			object bestGraphic_Child = null;

			//Log.Message("Looping through bodyTypeGraphics, second pass");
			foreach (object bodyTypeGraphic in bodyTypeGraphics_ienum)
			{
				BodyTypeDef bodyTypeDef = (BodyTypeDef)Patch_HAR.field_BodyTypeGraphic_bodyType.GetValue(bodyTypeGraphic);
				path = (string)Patch_HAR.class_BodyTypeGraphic.GetMethod("GetPath", new Type[] { }).Invoke(bodyTypeGraphic, new object[] { });
				//Log.Message("bodyTypeDef: " + bodyTypeDef + ", path: " + path);

				//if the graphic path doesn't resolve, can't use this graphic for anything
				if (!CheckTextures(path))
				{
					//Log.Message("Found no graphic at " + path + ", continuing");
					continue;
				}

				//if we're still looking for a good substitute for baby or child
				//consider the current graphic
				//and compare to the last best one we've found
				if (!bodyAccountedFor_Baby)
				{
					priority = BodyTypePriority_Baby(bodyTypeDef);
					if (priority > bestPriority_Baby)
					{
						bestPriority_Baby = priority;
						bestGraphic_Baby = bodyTypeGraphic;
					}
				}
				if (!bodyAccountedFor_Child)
				{
					priority = BodyTypePriority_Child(bodyTypeDef);
					if (priority > bestPriority_Child)
					{
						bestPriority_Child = priority;
						bestGraphic_Child = bodyTypeGraphic;
					}
				}
			}
			//Log.Message("bestGraphic_Baby: " + bestGraphic_Baby + ", priority: " + bestPriority_Baby
			//    + ", bestGraphic_Child: " + bestGraphic_Child + ", priority: " + bestPriority_Child);

			if (!bodyAccountedFor_Baby)
			{
				//if we've identified a candidate from the race-specific graphics
				//use that
				if (bestGraphic_Baby != null)
				{
					//Log.Message("Copying from bestGraphic");
					//copy all fields
					foreach (FieldInfo fieldInfo in bestGraphic_Baby.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
					{
						fieldInfo.SetValue(graphicBaby_obj, fieldInfo.GetValue(bestGraphic_Baby));
					}

					//then set bodyType (back) to what it should be
					Patch_HAR.field_BodyTypeGraphic_bodyType.SetValue(graphicBaby_obj, bodyType_Baby);
				}
				//if we haven't, default back to vanilla
				else
				{
					//Log.Message("Copying from vanilla");
					Patch_HAR.field_BodyTypeGraphic_bodyType.SetValue(graphicBaby_obj, bodyType_Baby);
					graphicBaby_obj.GetType().GetField("path").SetValue(graphicBaby_obj, VANILLA_BODY_PATH);
				}
			}

			if (!bodyAccountedFor_Child)
			{
				//if we've identified a candidate from the race-specific graphics
				//use that
				if (bestGraphic_Child != null)
				{
					//Log.Message("Copying from bestGraphic");
					//copy all fields
					foreach (FieldInfo fieldInfo in bestGraphic_Child.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
					{
						fieldInfo.SetValue(graphicChild_obj, fieldInfo.GetValue(bestGraphic_Child));
					}

					//then set bodyType (back) to what it should be
					Patch_HAR.field_BodyTypeGraphic_bodyType.SetValue(graphicChild_obj, bodyType_Child);
				}
				//if we haven't, default back to vanilla
				else
				{
					//Log.Message("Copying from vanilla");
					Patch_HAR.field_BodyTypeGraphic_bodyType.SetValue(graphicChild_obj, bodyType_Child);
					graphicChild_obj.GetType().GetField("path").SetValue(graphicChild_obj, VANILLA_BODY_PATH);
				}
			}

		}
	}

	public void FindNestedBodyTypeVariants(object graphic, ref List<object> collector)
	{
		Log.Message("FindNestedBodyTypeVariants, graphic: " + graphic);
		if (!Patch_HAR.class_AbstractExtendedGraphic.IsAssignableFrom(graphic.GetType())) return;

		IEnumerable<object> subgraphics = Patch_HAR.fields_subgraphics.Select<FieldInfo, object>(x => x.GetValue(graphic));
		Log.Message("fields_subgraphics: " + Patch_HAR.fields_subgraphics.ToStringSafeEnumerable());
		Log.Message("subgraphics: " + subgraphics.ToStringSafeEnumerable());
		if (subgraphics.EnumerableNullOrEmpty()) return;

		foreach (object subgraphic in subgraphics)
		{
			Log.Message("subgraphic: " + subgraphic + ", " + (subgraphic as IEnumerable).ToStringSafeEnumerable());
			//if the list is not empty we will need to iterate through it
			if (!((subgraphic as IEnumerable).EnumerableCount() == 0))
			{
				//if the type of the list is List<AlienPartGenerator.ExtendedBodytypeGraphic>
				//we want to "return" it as one of the objects we're looking for
				Type[] types = subgraphic.GetType().GetGenericArguments();
				if (types.Length == 1 && types[0] == Patch_HAR.class_BodyTypeGraphic)
				{
					collector.Add(subgraphic);
				}
				FindNestedBodyTypeVariants(subgraphic, ref collector);
			}
		}
	}

	

	public int BodyTypePriority_Baby(BodyTypeDef def)
	{
		if (def == BodyTypeDefOf.Baby || def.defName.Contains("Baby") || def.defName.Contains("baby")) return 100;
		if (def == BodyTypeDefOf.Child || def.defName.Contains("Child") || def.defName.Contains("child")) return 10;
		if (def == BodyTypeDefOf.Thin || def.defName.Contains("Thin") || def.defName.Contains("thin")) return 9;
		if (def.defName.Contains("Main") || def.defName.Contains("main")
			|| def.defName.Contains("Norm") || def.defName.Contains("norm")
			|| def.defName.Contains("Stand") || def.defName.Contains("stand")
			|| def.defName.Contains("Std") || def.defName.Contains("std")
			|| def.defName.Contains("Default") || def.defName.Contains("default")
			|| def.defName.Contains("Base") || def.defName.Contains("base")
			|| def.defName.Contains("Basic") || def.defName.Contains("basic")
			) return 8;
		if (def == BodyTypeDefOf.Male || def.defName.Contains("Male") || def.defName.Contains("male")) return 7;
		return 1;
	}
	public int BodyTypePriority_Child(BodyTypeDef def)
	{
		if (def == BodyTypeDefOf.Child || def.defName.Contains("Child") || def.defName.Contains("child")) return 100;
		if (def == BodyTypeDefOf.Baby || def.defName.Contains("Baby") || def.defName.Contains("baby")) return 10;
		if (def == BodyTypeDefOf.Thin || def.defName.Contains("Thin") || def.defName.Contains("thin")) return 9;
		if (def.defName.Contains("Main") || def.defName.Contains("main")
			|| def.defName.Contains("Norm") || def.defName.Contains("norm")
			|| def.defName.Contains("Stand") || def.defName.Contains("stand")
			|| def.defName.Contains("Std") || def.defName.Contains("std")
			|| def.defName.Contains("Default") || def.defName.Contains("default")
			|| def.defName.Contains("Base") || def.defName.Contains("base")
			|| def.defName.Contains("Basic") || def.defName.Contains("basic")
			) return 8;
		if (def == BodyTypeDefOf.Male || def.defName.Contains("Male") || def.defName.Contains("male")) return 7;
		return 1;
	}

}



public partial class AlienRace
{

	public Dictionary<object, BodyAddon> bodyAddons = new Dictionary<object, BodyAddon>();
	public Dictionary<string, string> babyAgeGraphics = new Dictionary<string, string>();
	public Dictionary<string, string> bodyAddonAgeGraphics = new Dictionary<string, string>();

	public void InitGraphicField(object graphic, string key, ref Dictionary<string, string> outputDict)
	{
		if (key == "body")
		{
			bodyGraphic_obj = graphic;
		}

		//if there are no age graphic variants we don't need to touch this graphic
		FieldInfo ageGraphics_field = graphic.GetType().GetField("ageGraphics", BindingFlags.Instance | BindingFlags.Public);
		if (ageGraphics_field == null) return;

		object ageGraphics_obj = ageGraphics_field.GetValue(graphic);
		IEnumerable ageGraphics_ienum = ageGraphics_obj as IEnumerable;
		if (ageGraphics_ienum.EnumerableCount() == 0) return;

		string path = "";
		//loop over the life stages registered in the ageGraphic
		foreach (object ageGraphic in ageGraphics_ienum)
		{
			path = (string)ageGraphic.GetType().GetMethod("GetPath", types: new Type[] { }).Invoke(ageGraphic, new object[] { });
			LifeStageDef lifeStageDef = (LifeStageDef)ageGraphic.GetType().GetField("age", BindingFlags.Instance | BindingFlags.Public).GetValue(ageGraphic);

			//if we've found a child-specific body graphic, make a note of it
			if (lifeStageDef == lifeStageChild.def && key == "body") bodyAccountedFor_Child = true;
			if (lifeStageDef == lifeStageBaby.def)
			{
				//if we've found a child-specific body graphic, make a note of it
				if (key == "body") bodyAccountedFor_Baby = true;
				//found the most relevant path: the one for baby
				break;
			}
		}
		//if we found no baby-specific path, we'll just make a note of the last path we looked at

		Log.Message("Final path for " + key + ": " + path);
		if (path.NullOrEmpty()) return;

		outputDict.Add(key, path);
	}

	public void InitGraphicFields()
	{
		Log.Message("Firing InitGraphicFields, def: " + def);

			if (Patch_HAR.class_ThingDef_AlienRace.IsAssignableFrom(def.GetType()))
			{
				if (Patch_HAR.class_GraphicPaths.IsAssignableFrom(graphicPaths.GetType()))
				{
					List<FieldInfo> graphicFields = (from field
													in graphicPaths.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)
													 where Patch_HAR.class_AbstractExtendedGraphic.IsAssignableFrom(field.FieldType)
													 select field).ToList();
					//Log.Message("graphicFields: " + graphicFields);
					foreach (FieldInfo field in graphicFields)
					{
						string fieldName = field.Name;
						object graphic = field.GetValue(graphicPaths);

						InitGraphicField(graphic, fieldName, ref babyAgeGraphics);

					}
				}
				//else Log.Message("Failed GraphicPaths.IsAssignableFrom(graphicPaths)");

				foreach (KeyValuePair<object, BodyAddon> kvp in bodyAddons)
				{
					object orig = kvp.Key;
					BodyAddon wrapper = kvp.Value;

					string name = wrapper.name;
					InitGraphicField(orig, name, ref bodyAddonAgeGraphics);

					//Log.Message("orig: " + orig.ToString());
				}
			}
		//else Log.Message("Failed ThingDef_AlienRace.IsAssignableFrom(def)");
	}

	public void UpdateAgeGraphics()
	{
		//Log.Message("Started UpdateAgeGraphics");
		//Log.Message("babyAgeGraphics: " + babyAgeGraphics + ", Count: " + babyAgeGraphics.Count);
		//Log.Message("bodyAddons: " + bodyAddons + ", Count: " + bodyAddons.Count);

		foreach (KeyValuePair<string, string> kvp in babyAgeGraphics)
		{
			Log.Message("UpdateAgeGraphics: " + kvp);

			string fieldName = kvp.Key;
			string path = kvp.Value;

			object graphic = graphicPaths.GetType().GetField(fieldName).GetValue(graphicPaths);

			FieldInfo ageGraphics_field = graphic.GetType().GetField("ageGraphics", BindingFlags.Instance | BindingFlags.Public);
			if (ageGraphics_field == null)
			{
				Log.Message("Graphic field " + fieldName + " has no ageGraphics");
				continue;
			}
			object ageGraphics_obj = ageGraphics_field.GetValue(graphic);

			object ageGraphic_toddler = Activator.CreateInstance(Patch_HAR.class_ExtendedAgeGraphic);
			ageGraphic_toddler.GetType().GetField("age").SetValue(ageGraphic_toddler, lifeStageToddler.def);
			ageGraphic_toddler.GetType().GetField("path").SetValue(ageGraphic_toddler, path);
			ageGraphic_toddler.GetType().GetField("variantCount").SetValue(ageGraphic_toddler, 1);

			ageGraphics_obj.GetType().GetMethod("Add").Invoke(ageGraphics_obj, new object[] { ageGraphic_toddler });

			/*
			Log.Message("ageGraphics_obj: " + ageGraphics_obj);
			IEnumerable ageGraphics_ienum = ageGraphics_obj as IEnumerable;
			foreach (object item in ageGraphics_ienum)
			{
				Log.Message("age: " + item.GetType().GetField("age").GetValue(item)
					+ ", path: " + item.GetType().GetField("path").GetValue(item)
					);
				;
			}
			*/
		}

		foreach (KeyValuePair<object, BodyAddon> kvp in bodyAddons as IEnumerable)
		{
			//Log.Message("UpdateAgeGraphics: " + kvp);

			object orig = kvp.Key;
			BodyAddon wrapper = kvp.Value;

			string name = wrapper.name;
			string path;

			//Log.Message("name: " + name);

			if (bodyAddonAgeGraphics.ContainsKey(name))
			{
				path = bodyAddonAgeGraphics[name];
				//Log.Message("path: " + path);

				//FieldInfo ageGraphics_field = bodyAddon.GetType().GetField("ageGraphics", BindingFlags.Instance | BindingFlags.Public);
				if (wrapper.ageGraphics == null)
				{
					Log.Message("BodyAddon " + wrapper.name + " has no ageGraphics");
					continue;
				}
				//object ageGraphics_obj = ageGraphics_field.GetValue(bodyAddon);

				object ageGraphic_toddler = Activator.CreateInstance(Patch_HAR.class_ExtendedAgeGraphic);
				ageGraphic_toddler.GetType().GetField("age").SetValue(ageGraphic_toddler, lifeStageToddler.def);
				ageGraphic_toddler.GetType().GetField("path").SetValue(ageGraphic_toddler, path);
				ageGraphic_toddler.GetType().GetField("variantCount").SetValue(ageGraphic_toddler, 1);

				wrapper.ageGraphics.GetType().GetMethod("Add").Invoke(wrapper.ageGraphics, new object[] { ageGraphic_toddler });

				//Log.Message("ageGraphics: " + wrapper.ageGraphics);
				/*
				IEnumerable ageGraphics_ienum = wrapper.ageGraphics as IEnumerable;

				foreach (object item in ageGraphics_ienum)
				{
					Log.Message("age: " + item.GetType().GetField("age").GetValue(item)
						+ ", path: " + item.GetType().GetField("path").GetValue(item)
						);
					;
				}
				*/
			}
		}

		//Log.Message("Finished UpdateAgeGraphics");
	}

	public void InitBodyAddons()
	{
		object bodyAddons_obj = alienPartGenerator.GetType().GetField("bodyAddons", BindingFlags.Public | BindingFlags.Instance).GetValue(alienPartGenerator);
		//Log.Message("bodyAddons_obj: " + bodyAddons_obj + ", Count: " + (bodyAddons_obj as IEnumerable).EnumerableCount());

		foreach (object bodyAddon_origType in bodyAddons_obj as IEnumerable)
		{
			//Log.Message("bodyAddon_origType: " + bodyAddon_origType + ", Type: " + bodyAddon_origType.GetType());
			BodyAddon bodyAddon = new BodyAddon(bodyAddon_origType);
			bodyAddons.Add(bodyAddon_origType, bodyAddon);
			Log.Message("Adding BodyAddon " + bodyAddon.name + " to list for " + def.defName);
		}
	}

}







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


	public static LocalTargetInfo Old_SafePlaceForBaby(Pawn baby, Pawn hauler)
	{
		//Log.Message("Fired SafePlaceForBaby");
		if (!ChildcareUtility.CanSuckle(baby, out var _) || !ChildcareUtility.CanHaulBabyNow(hauler, baby, false, out var _))
			return LocalTargetInfo.Invalid;

		Building_Bed bed = baby.CurrentBed() ?? RestUtility.FindBedFor(baby, hauler, checkSocialProperness: true, ignoreOtherReservations: false, baby.GuestStatus);
		//Log.Message("bed: " + bed);

		IntVec3 currentCell = baby.PositionHeld;
		LocalTargetInfo target = LocalTargetInfo.Invalid;

		//if the baby has a serious temperature injury
		//look for somewhere they can recover
		//ignoring zoning
		if (baby.health != null && baby.health.hediffSet != null
			&& baby.health.hediffSet.HasTemperatureInjury(TemperatureInjuryStage.Serious))
		{
			//if the best available bed is at a safe temperature, pick that
			if (bed != null && baby.SafeTemperatureRange().Includes(TemperatureAtBed(bed, baby.MapHeld)))
				return bed;


		}

		//sick babies and toddlers should both be taken to medical beds if possible
		//requiring safe temperature but not necessarily comfortable
		if (HealthAIUtility.ShouldSeekMedicalRest(baby))
		{
			//Log.Message("Baby " + baby + " is sick");

			//if the best available bed is at a safe temperature, pick that
			if (bed != null && baby.SafeTemperatureRange().Includes(TemperatureAtBed(bed, baby.MapHeld)))
				return bed;

			//if we didn't find a good bed, look for just a spot that's a good temperature 
			target = ChildcareUtility.BabyNeedsMovingForTemperatureReasons(baby, hauler, out var preferredRegion)
				? ((LocalTargetInfo)RCellFinder.SpotToStandDuringJob(hauler, null, preferredRegion))
				: ((!baby.Spawned)
					? ((LocalTargetInfo)RCellFinder.SpotToStandDuringJob(hauler, null, baby.GetRegionHeld()))
					: ((LocalTargetInfo)baby.Position));

			return target;
		}

		//more relaxed logic for healthy toddlers
		if (!baby.Downed)
		{
			//Log.Message("Baby " + baby + " is a toddler");

			//if the toddler is in a non-optimal temperature, try to pick a spot that would be better
			if (ChildcareUtility.BabyNeedsMovingForTemperatureReasons(baby, hauler, out var preferredRegion))
			{
				//Log.Message("Baby " + baby + " needs moving for temperature reasons");

				//if their bed would be comfortable, take them there
				if (bed != null && baby.ComfortableTemperatureRange().Includes(TemperatureAtBed(bed, baby.MapHeld)))
					return bed;
				//otherwise pick a spot in the region indicated by BabyNeedsMovingForTemperatureReasons
				else target = (LocalTargetInfo)RCellFinder.SpotToStandDuringJob(hauler, null, preferredRegion);
				//if they have a bed check which of the bed and the target spot is the better temperature, preferring the bed if they're equivalent
				if (bed != null) return BestTemperatureForPawn(bed, target, baby);
				return target;
			}

			//if the toddler is tired and they have a bed to go to, consider moving them to it
			else if (bed != null && baby.needs.rest.CurLevelPercentage < 0.28)
			{
				//Log.Message("Baby " + baby + " is tired");

				//if the bed is a comfortable temperature, just do it
				if (baby.ComfortableTemperatureRange().Includes(TemperatureAtBed(bed, baby.MapHeld))) return bed;

				//otherwise if the bed is a better temperature than where the toddller is now, do it
				return BestTemperatureForPawn(bed, currentCell, baby);
			}

			//otherwise if the toddler isn't spawned (ie being carried), try to pick a spot near where they are currently (ie just put them down)
			else if (!baby.Spawned) return RCellFinder.SpotToStandDuringJob(hauler, null, baby.GetRegionHeld());

			//if none of the above, just leave them where they are
			else return currentCell;
		}

		//mostly-vanilla logic for babies that prefers taking them to bed even if they aren't tired
		else
		{
			//if the bed's a comfortable temperature, take them straight there
			if (baby.ComfortableTemperatureRange().Includes(TemperatureAtBed(bed, baby.MapHeld)))
			{
				//Log.Message("bed is fine");
				return bed;
			}

			//if they need moving for temperature reasons
			if (ChildcareUtility.BabyNeedsMovingForTemperatureReasons(baby, hauler, out var preferredRegion))
			{
				//Log.Message("needs moving for temperature reasons");
				//pick a spot in the region indicated by BabyNeedsMovingForTemperatureReasons
				target = (LocalTargetInfo)RCellFinder.SpotToStandDuringJob(hauler, null, preferredRegion);
				//if they have a bed check which of the bed and the target spot is the better temperature, preferring the bed if they're equivalent
				//Log.Message("bed temperature: " + GenTemperature.GetTemperatureForCell(bed.Position, hauler.MapHeld));
				//Log.Message("target: " + target.Cell.ToString() + ", temperature: " + GenTemperature.GetTemperatureForCell(target.Cell, hauler.MapHeld));
				if (bed != null) return BestTemperatureForPawn(bed, target, baby);
				return target;
			}
			//otherwise
			else
			{
				//take them to their bed so long as it's not a worse temperature than where they are now
				if (bed != null) return BestTemperatureForPawn(bed, currentCell, baby);

				//otherwise if the toddler isn't spawned (ie being carried), try to pick a spot near where they are currently (ie just put them down)
				else if (!baby.Spawned) return RCellFinder.SpotToStandDuringJob(hauler, null, baby.GetRegionHeld());

				//if none of the above, just leave them where they are
				else return currentCell;
			}
		}

		//fall-through, shouldn't happen
		return LocalTargetInfo.Invalid;
	}

	public static bool NeedsRescue(Pawn baby, Pawn hauler)
	{
		//Log.Message("Fired NeedsRescue");
		if (baby == null || hauler == null) return false;
		if (CaravanFormingUtility.IsFormingCaravanOrDownedPawnToBeTakenByCaravan(baby)) return false;
		LocalTargetInfo safePlace = SafePlaceForBaby(baby, hauler, out BabyMoveReason moveReason);
		//Log.Message("safePlace: " + safePlace.ToString());
		//Log.Message("baby.PositionHeld: " + safePlace.ToString());
		if (safePlace.IsValid && safePlace.Cell != baby.PositionHeld) return true;
		return false;
	}

}