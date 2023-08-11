using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Toddlers
{
    static class ToddlerRenderer
	{
		private const float MaxWobbleMagnitude = 10f;
		private const int MaxWobblePeriod = 70;
		private const int MinWobblePeriod = 40;
		public const float CrawlAngle = 40f;

		public static float GetWobbleMagnitude(Pawn pawn)
        {
			Hediff_LearningToWalk hediff = pawn.health.hediffSet.GetFirstHediffOfDef(Toddlers_DefOf.LearningToWalk) as Hediff_LearningToWalk;
			return (1 - hediff.Progress) * MaxWobbleMagnitude;
		}

		public static int GetWobblePeriod(Pawn pawn)
        {
			Hediff_LearningToWalk hediff = pawn.health.hediffSet.GetFirstHediffOfDef(Toddlers_DefOf.LearningToWalk) as Hediff_LearningToWalk;
			return MinWobblePeriod + (int)((1f - hediff.Progress) * ((float)MaxWobblePeriod - (float)MinWobblePeriod));
		}

		public enum WiggleFunc
		{
			Sine,
			Triangle,
			Toddle
		}

		public static float Waveform(Func<float,float> quarterform, float phase)
        {
			float phaseSign = 1f;
			float innerPhase = 0f;
			if (phase <= 0.25f)
			{
				phaseSign = 1f;
				innerPhase = 4f * phase;
			}
			if (phase > 0.25f && phase <= 0.5f)
			{
				phaseSign = 1f;
				innerPhase = 4f * (0.5f - phase);
			}
			if (phase > 0.5f && phase <= 0.75f)
			{
				phaseSign = -1f;
				innerPhase = 4f * (phase - 0.5f);
			}
			if (phase > 0.75f)
			{
				phaseSign = -1f;
				innerPhase = 4f * (1f - phase);
			}
			return quarterform(innerPhase) * phaseSign;
		}
		
		public static float Toddle(float innerPhase)
        {
			float br_x = 0.4f;
			float br_y = 0.7f;
			float result;

			if (innerPhase <= br_x) result = br_y * innerPhase / br_x;
			else result = br_y + (1 - br_y) * (innerPhase - br_x) / (1 - br_x);
			return result;
		}

		public static float WiggleAngle(Pawn pawn, float angleMagnitude, int period, WiggleFunc waveform)
		{
			int phaseModulus = (Find.TickManager.TicksGame + Mathf.Abs(pawn.HashOffset())) % period;
			float phase = (float)phaseModulus / (float)period;
			float angle = 0f;

			switch (waveform)
			{
				case (WiggleFunc.Sine):
					angle = (float)Math.Sin(phase * (2 * Math.PI));
					break;
				case (WiggleFunc.Triangle):
					angle = Waveform(x => x, phase);
					break;
				case (WiggleFunc.Toddle):
					angle = Waveform(x => Toddle(x), phase);
					break;
				default:
					break;
			}
			angle *= angleMagnitude;
			return angle;
		}

		public enum ToddlerRenderMode
		{
			Base,
			Carried,
			Crawling,
			Toddling,
			WigglingInCrib,
			LayingAngle,
			Bugwatching
		}

		public static ToddlerRenderMode GetToddlerRenderMode(Pawn pawn)
        {
			//Log.Message("parentHolder: " + ___pawn.ParentHolder.ToString());
			IThingHolder parentHolder = pawn.ParentHolder;

			if (!(parentHolder is Map || parentHolder is Pawn_CarryTracker)) 
				return ToddlerRenderMode.Base;

			if (parentHolder is Pawn_CarryTracker)
            {
				//if not "standing" and carried then the default render logic will work fine, so only care about this case
				if (pawn.GetPosture() == PawnPosture.Standing)
					return ToddlerRenderMode.Carried;

				else return ToddlerRenderMode.Base;
			}

			if (parentHolder is Map)
            {
				if (pawn.jobs != null && pawn.jobs.curJob != null
				&& pawn.CurJobDef == Toddlers_DefOf.ToddlerBugwatching && pawn.jobs.curJob.targetA.Cell == pawn.Position)
					return ToddlerRenderMode.Bugwatching;

				if (pawn.GetPosture() == PawnPosture.Standing && pawn.pather.Moving)
				{
					if (ToddlerUtility.IsCrawler(pawn))
						return ToddlerRenderMode.Crawling;

					if (ToddlerUtility.IsWobbly(pawn))
						return ToddlerRenderMode.Toddling;
				}

				if (pawn.CurJobDef == Toddlers_DefOf.LayAngleInCrib)
					return ToddlerRenderMode.LayingAngle;

				if (pawn.CurJob != null
				&& ((pawn.CurJobDef.reportString != null && pawn.CurJobDef.reportString.Contains("wiggling"))
				|| (pawn.CurJob.reportStringOverride != null && pawn.CurJob.reportStringOverride.Contains("wiggling"))))
					return ToddlerRenderMode.WigglingInCrib;
			}

			return ToddlerRenderMode.Base;
		}
 
		public static void RenderToddlerInternal(PawnRenderer pawnRenderer, ToddlerRenderMode renderMode, Pawn pawn, Vector3 rootLoc, float angleIn, bool renderBody, Rot4 bodyFacing, RotDrawMode bodyDrawType, PawnRenderFlags flags)
		{
			if (!pawnRenderer.graphics.AllResolved)
			{
				pawnRenderer.graphics.ResolveAllGraphics();
			}

			//establish rotations
			float bodyAngle = angleIn;
			float headAngle = angleIn;

			Rot4 effectiveBodyFacing = bodyFacing;

			//if we're crawling we rotate around the body centre
			//so the body only gets its angles changed
			if (renderMode == ToddlerRenderMode.Crawling)
			{
				if (bodyFacing == Rot4.East)
				{
					bodyAngle = CrawlAngle;
					headAngle = bodyAngle / 2;
				}
				if (bodyFacing == Rot4.West)
				{
					bodyAngle = -1 * CrawlAngle;
					headAngle = bodyAngle / 2;
				}
				if (bodyFacing == Rot4.South)
				{
					headAngle = 0f;
					bodyAngle = 180f;
					//should see the toddler's back not their front
					effectiveBodyFacing = Rot4.North;
				}
			}

			Vector3 bodyOffset = Vector3.zero;
			if (pawn.ageTracker.CurLifeStage.bodyDrawOffset.HasValue)
			{
				bodyOffset = pawn.ageTracker.CurLifeStage.bodyDrawOffset.Value;
			}
			bodyOffset = bodyOffset.RotatedBy(bodyAngle);
			//Log.Message("bodyOffset: " + bodyOffset);

			Vector3 bodyLoc = rootLoc;
			bodyLoc += bodyOffset;

			Vector3 headOffset = Vector3.zero;

			//set the y-axis (depth) element 
			headOffset.y += bodyFacing == Rot4.North ? 3f / 148f : 0.0231660213f;

			//get the base head offset
			headOffset += pawnRenderer.BaseHeadOffsetAt(effectiveBodyFacing);
			//Log.Message("headOffset: " + headOffset);
			
			//if we're toddling we rotate around a separate rotation centre
			//so we also need to apply offsets to the body
			if (renderMode == ToddlerRenderMode.Toddling && !flags.HasFlag(PawnRenderFlags.Cache))
			{
				float wiggleAngle = WiggleAngle(pawn, GetWobbleMagnitude(pawn), GetWobblePeriod(pawn), WiggleFunc.Toddle);

				Vector3 rotateAbout = bodyLoc;
				rotateAbout.z -= 0.2f;

				bodyAngle = wiggleAngle;
				headAngle = wiggleAngle;

				Vector3 bodyRel = bodyLoc - rotateAbout;
				bodyRel = bodyRel.RotatedBy(wiggleAngle);
				bodyLoc = rotateAbout + bodyRel;
			}

			if (Toddlers_Mod.HARLoaded)
			{
				AlienRace alienRace = Patch_HAR.GetAlienRaceWrapper(pawn);
				//Log.Message("alienRace: " + alienRace + ", tweak: " + alienRace.crawlingTweak);
				if (alienRace.crawlingTweak != null)
				{
					Vector2 tweakOffset = alienRace.crawlingTweak.HeadOffset(bodyFacing);
					//Log.Message("tweakOffset(" + bodyFacing + "): " + tweakOffset);
					headOffset.x += tweakOffset.x;
					headOffset.z += tweakOffset.y;
				}
			}
			Vector3 headOffset_actual = headOffset.RotatedBy(bodyAngle);

			Vector3 headLoc = bodyLoc + headOffset_actual;
			//Log.Message("headOffset_actual: " + headOffset_actual + ", headLoc: " + headLoc);

			Quaternion bodyQuat = Quaternion.AngleAxis(bodyAngle, Vector3.up);
			Quaternion headQuat = Quaternion.AngleAxis(headAngle, Vector3.up);

			//other layers are defined by y-axis (ie depth) offsets
			Vector3 utilityLoc = bodyLoc;
			utilityLoc.y += effectiveBodyFacing == Rot4.South ? 0.00579150533f : 0.0289575271f;
			Vector3 bodyApparelLoc = bodyLoc;
			bodyApparelLoc.y += effectiveBodyFacing == Rot4.North ? 0.0231660213f : 3f / 148f;
			Vector3 woundLoc1 = bodyLoc;
			woundLoc1.y += 0.009687258f;
			Vector3 woundLoc2 = bodyLoc;
			woundLoc2.y += 0.0221660212f;
			Vector3 shellLoc = bodyLoc;
			shellLoc.y += 0.009187258f;
			Vector3 firefoamLoc = bodyLoc;
			firefoamLoc.y += 0.033301156f;
			Vector3 firefoamHeadLoc = headLoc;
			firefoamHeadLoc.y += 0.033301156f;

			Mesh bodyMesh = null;
			if (renderBody)
			{
				DrawPawnBody(pawnRenderer, bodyLoc, bodyAngle, effectiveBodyFacing, bodyDrawType, flags, out bodyMesh);
				if (bodyDrawType == RotDrawMode.Fresh && pawnRenderer.graphics.furCoveredGraphic != null)
				{
					DrawPawnFur(pawnRenderer, shellLoc, effectiveBodyFacing, bodyQuat, flags);
				}
				if (bodyDrawType == RotDrawMode.Fresh)
				{
					pawnRenderer.WoundOverlays.RenderPawnOverlay(woundLoc1, bodyMesh, bodyQuat, flags.FlagSet(PawnRenderFlags.DrawNow), PawnOverlayDrawer.OverlayLayer.Body, effectiveBodyFacing, false);
				}
				if (flags.FlagSet(PawnRenderFlags.Clothes))
				{
					DrawBodyApparel(pawnRenderer, bodyApparelLoc, utilityLoc, bodyMesh, bodyAngle, effectiveBodyFacing, flags);
				}
				if (ModLister.BiotechInstalled && pawn.genes != null)
				{
					DrawBodyGenes(pawnRenderer, bodyLoc, bodyQuat, bodyAngle, effectiveBodyFacing, bodyDrawType, flags);
				}
				if (bodyDrawType == RotDrawMode.Fresh)
				{
					pawnRenderer.WoundOverlays.RenderPawnOverlay(woundLoc2, bodyMesh, bodyQuat, flags.FlagSet(PawnRenderFlags.DrawNow), PawnOverlayDrawer.OverlayLayer.Body, effectiveBodyFacing, true);
				}
				if (bodyDrawType == RotDrawMode.Fresh && pawnRenderer.FirefoamOverlays.IsCoveredInFoam)
				{
					pawnRenderer.FirefoamOverlays.RenderPawnOverlay(firefoamLoc, bodyMesh, bodyQuat, flags.FlagSet(PawnRenderFlags.DrawNow), PawnOverlayDrawer.OverlayLayer.Body, effectiveBodyFacing);
				}
			}

			if (pawnRenderer.graphics.headGraphic != null)
			{
				Mesh headMesh = null;
				Material material;

				material = pawnRenderer.graphics.HeadMatAt(bodyFacing, bodyDrawType, flags.FlagSet(PawnRenderFlags.HeadStump), flags.FlagSet(PawnRenderFlags.Portrait), !flags.FlagSet(PawnRenderFlags.Cache));
				if (material != null)
				{
					headMesh = HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn(pawn).MeshAt(bodyFacing);

					if (Toddlers_Mod.facialAnimationLoaded)
					{
						Patch_FacialAnimation.DrawFace(headMesh, headLoc, headQuat, material, flags.FlagSet(PawnRenderFlags.Portrait));
					}
					else
					{
						GenDraw.DrawMeshNowOrLater(headMesh, headLoc, headQuat, material, flags.FlagSet(PawnRenderFlags.DrawNow));
					}
				}

				if (bodyDrawType == RotDrawMode.Fresh)
				{
					pawnRenderer.WoundOverlays.RenderPawnOverlay(headLoc, bodyMesh, headQuat, flags.FlagSet(PawnRenderFlags.DrawNow), PawnOverlayDrawer.OverlayLayer.Head, bodyFacing);
				}
				if (pawnRenderer.graphics.headGraphic != null)
				{
					Vector3 bodyLoc_effective;
					Vector3 headOffset_effective;

					if (headAngle != bodyAngle)
					{
						headOffset_effective = headOffset.RotatedBy(headAngle);
						bodyLoc_effective = headLoc - headOffset_effective;
                    }
                    else
                    {
						headOffset_effective = headOffset_actual;
						bodyLoc_effective = bodyLoc;
                    }

					DrawHeadHair(pawnRenderer, bodyLoc_effective, headOffset_effective, headAngle, bodyFacing, bodyFacing, bodyDrawType, flags, renderBody);
				}
				if (bodyDrawType == RotDrawMode.Fresh && pawnRenderer.FirefoamOverlays.IsCoveredInFoam && headMesh != null)
				{
					pawnRenderer.FirefoamOverlays.RenderPawnOverlay(firefoamHeadLoc, headMesh, headQuat, flags.FlagSet(PawnRenderFlags.DrawNow), PawnOverlayDrawer.OverlayLayer.Head, bodyFacing);
				}
			}

            if (Toddlers_Mod.HARLoaded)
            {
				// public static void DrawAddons(PawnRenderFlags renderFlags, Vector3 vector, Vector3 headOffset, Pawn pawn, Quaternion quat, Rot4 rotation)
				/*
				Type class_HARHarmonyPatches = (from asm in AppDomain.CurrentDomain.GetAssemblies()
														from type in asm.GetTypes()
														where type.Namespace == "AlienRace" && type.IsClass && type.Name == "HarmonyPatches"
												select type).Single();
				MethodInfo drawAddOns = class_HARHarmonyPatches.GetMethod("DrawAddons", BindingFlags.Static | BindingFlags.Public);
				*/
				Vector3 headOffset_sansY = new Vector3(headOffset_actual.x, 0f, headOffset_actual.z);

				//Log.Message("About to DrawAddons, pawn: " + pawn + ", bodyLoc: " + bodyLoc + ", headOffset_sansY: " + headOffset_sansY 
				//	+ ", bodyQuat: " + bodyQuat + ", bodyFacing: " + bodyFacing);
				object[] args = new object[] { flags, bodyLoc, headOffset_sansY, pawn , bodyQuat , bodyFacing };

				Patch_HAR.method_HarmonyPatches_DrawAddons.Invoke(null, args);
            }


			if (!flags.FlagSet(PawnRenderFlags.Portrait) && !flags.FlagSet(PawnRenderFlags.Cache))
			{
				DrawDynamicParts(pawnRenderer, bodyLoc, bodyAngle, bodyFacing, flags);
			}

		}

		private static void DrawPawnBody(PawnRenderer instance, Vector3 rootLoc, float angle, Rot4 facing, RotDrawMode bodyDrawType, PawnRenderFlags flags, out Mesh bodyMesh)
		{
			object[] parameters = new object[] { rootLoc, angle, facing, bodyDrawType, flags, null };
			typeof(PawnRenderer).GetMethod("DrawPawnBody", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, parameters);
			bodyMesh = (Mesh)parameters[5];
		}

		private static void DrawPawnFur(PawnRenderer instance, Vector3 shellLoc, Rot4 facing, Quaternion quat, PawnRenderFlags flags)
		{
			object[] parameters = new object[] { shellLoc, facing, quat, flags };
			typeof(PawnRenderer).GetMethod("DrawPawnFur", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, parameters);
		}

		private static void DrawBodyApparel(PawnRenderer instance, Vector3 shellLoc, Vector3 utilityLoc, Mesh bodyMesh, float angle, Rot4 bodyFacing, PawnRenderFlags flags)
		{
			object[] parameters = new object[] { shellLoc, utilityLoc, bodyMesh, angle, bodyFacing, flags };
			typeof(PawnRenderer).GetMethod("DrawBodyApparel", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, parameters);
		}

		private static void DrawBodyGenes(PawnRenderer instance, Vector3 rootLoc, Quaternion quat, float angle, Rot4 bodyFacing, RotDrawMode bodyDrawType, PawnRenderFlags flags)
		{
			object[] parameters = new object[] { rootLoc, quat, angle, bodyFacing, bodyDrawType, flags };
			typeof(PawnRenderer).GetMethod("DrawBodyGenes", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, parameters);
		}

		private static void DrawHeadHair(PawnRenderer instance, Vector3 rootLoc, Vector3 headOffset, float angle, Rot4 bodyFacing, Rot4 headFacing, RotDrawMode bodyDrawType, PawnRenderFlags flags, bool bodyDrawn)
		{
			object[] parameters = new object[] { rootLoc, headOffset, angle, bodyFacing, headFacing, bodyDrawType, flags, bodyDrawn };
			typeof(PawnRenderer).GetMethod("DrawHeadHair", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, parameters);
		}

		private static void DrawDynamicParts(PawnRenderer instance, Vector3 rootLoc, float angle, Rot4 pawnRotation, PawnRenderFlags flags)
		{
			object[] parameters = new object[] { rootLoc, angle, pawnRotation, flags };
			typeof(PawnRenderer).GetMethod("DrawDynamicParts", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, parameters);
		}
	}

	[HarmonyPatch(typeof(PawnRenderer), "RenderPawnInternal")]
	class RenderPawnInternal_Patch
	{
		[HarmonyAfter(new string[] { Patch_FacialAnimation.FAHarmonyID, Patch_HAR.HARHarmonyID })]
		static bool Prefix(PawnRenderer __instance, Pawn ___pawn, Vector3 rootLoc, ref float angle, bool renderBody, ref Rot4 bodyFacing, RotDrawMode bodyDrawType, PawnRenderFlags flags, out bool __state)
		{
			__state = false;
			//Log.Message("__instance: " + __instance.ToString());
			//Log.Message("___pawn: " + ___pawn.ToString());
			//Log.Message("rootLoc: " + rootLoc.ToString());
			//Log.Message("angle: " + angle.ToString());
			//Log.Message("renderBody: " + renderBody.ToString());
			//Log.Message("bodyFacing: " + bodyFacing.ToString());
			//Log.Message("bodyDrawType: " + bodyDrawType.ToString());
			//Log.Message("flags: " + flags.ToString());
			//don't mess with non-toddlers
			if (!ToddlerUtility.IsLiveToddler(___pawn)) return true;

			//leave portraits alone
			if (flags.HasFlag(PawnRenderFlags.Portrait) | flags.HasFlag(PawnRenderFlags.StylingStation)) return true;

			ToddlerRenderer.ToddlerRenderMode mode = ToddlerRenderer.GetToddlerRenderMode(___pawn);

			//Log.Message("IsLiveToddler, RenderMode: " + mode);

			switch (mode)
            {
				case ToddlerRenderer.ToddlerRenderMode.Base:
					return true;

				case ToddlerRenderer.ToddlerRenderMode.Bugwatching:
					if (bodyFacing == Rot4.East)
					{
						angle = 40f;
					}
					if (bodyFacing == Rot4.West)
					{
						angle = -40f;
					}
					return true;

				case ToddlerRenderer.ToddlerRenderMode.Carried:
					Pawn carrier = (___pawn.ParentHolder as Pawn_CarryTracker).pawn;

					angle = ((carrier.Rotation == Rot4.West) ? 290f : 70f) + carrier.Drawer.renderer.BodyAngle();
					bodyFacing = (!(carrier.Rotation == Rot4.West) ? Rot4.West : Rot4.East);
					return true;

				case ToddlerRenderer.ToddlerRenderMode.LayingAngle:
					JobDriver_LayAngleInCrib driver = (JobDriver_LayAngleInCrib)___pawn.jobs.curDriver;
					angle = driver.angle;
					return true;

				case ToddlerRenderer.ToddlerRenderMode.WigglingInCrib:
					if (!flags.HasFlag(PawnRenderFlags.Cache))
						angle = ToddlerRenderer.WiggleAngle(___pawn, 15f, 120, ToddlerRenderer.WiggleFunc.Sine);
					return true;

				default:
					if (Toddlers_Settings.customRenderer)
					{
						ToddlerRenderer.RenderToddlerInternal(__instance, mode, ___pawn, rootLoc, angle, renderBody, bodyFacing, bodyDrawType, flags);
						__state = true;
						return false;
					}
					else return true;
			}
		}
	}

	
	[HarmonyPatch(typeof(Pawn_PathFollower),nameof(Pawn_PathFollower.StartPath))]
	class StartPath_Patch
    {
		static void Postfix(Pawn ___pawn)
        {
			if (ToddlerUtility.IsToddler(___pawn))
				___pawn.Drawer.renderer.graphics.SetAllGraphicsDirty();
		}
    }

	[HarmonyPatch(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.StopDead))]
	class StopDead_Patch
	{
		static void Postfix(Pawn ___pawn)
		{
			if (ToddlerUtility.IsToddler(___pawn))
				___pawn.Drawer.renderer.graphics.SetAllGraphicsDirty();
		}
	}
	

	[HarmonyPatch(typeof(JobDriver_Carried),"CarryToil")]
	class CarryToil_Patch
    {
		static Toil Postfix(Toil toil)
        {
			if (ToddlerUtility.IsLiveToddler(toil.actor))
            {
				toil.initAction = () => toil.actor.Drawer.renderer.graphics.SetAllGraphicsDirty();
				toil.AddFinishAction(() => toil.actor.Drawer.renderer.graphics.SetAllGraphicsDirty());
			}
			return toil;
        }
    }
}
