using HarmonyLib;
using RimWorld;
using System;
using System.Reflection;
using UnityEngine;
using Verse;

namespace Toddlers
{
    static class ToddlerRenderer
	{
		private const float MaxWobbleMagnitude = 10f;
		private const int MaxWobblePeriod = 70;
		private const int MinWobblePeriod = 40;

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
			Crawling,
			Toddling,
			WigglingInCrib
		}

		public static void RenderToddlerInternal(PawnRenderer pawnRenderer, ToddlerRenderMode renderMode, Pawn pawn, Vector3 rootLoc, float angleIn, bool renderBody, Rot4 bodyFacing, RotDrawMode bodyDrawType, PawnRenderFlags flags)
		{
			if (!pawnRenderer.graphics.AllResolved)
			{
				pawnRenderer.graphics.ResolveAllGraphics();
			}

			Mesh bodyMesh = null;

			//start in the unrotated frame
			Vector3 baseLoc = rootLoc;
			if (pawn.ageTracker.CurLifeStage.bodyDrawOffset.HasValue)
			{
				baseLoc += pawn.ageTracker.CurLifeStage.bodyDrawOffset.Value;
			}
			Vector3 bodyLoc = baseLoc;
			Vector3 headOffset = Vector3.zero;
			Vector3 headLoc = baseLoc;

			//set the y-axis (depth) element of the head offset
			headOffset.y += bodyFacing == Rot4.North ? 3f / 148f : 0.0231660213f;

			//get the base head offset
			headOffset += pawnRenderer.BaseHeadOffsetAt(bodyFacing);

			//if crawling, apply further custom offsets for the head 
			if (renderMode == ToddlerRenderMode.Crawling)
			{
				if (bodyFacing == Rot4.East)
				{
				}
				if (bodyFacing == Rot4.West)
				{
				}
				if (bodyFacing == Rot4.North)
				{
				}
				if (bodyFacing == Rot4.South)
				{
					headOffset.z += 0.00f;
				}
			}

			//establish rotations
			float bodyAngle = angleIn;
			float headAngle = angleIn;

			//if we're crawling we rotate around the body centre
			//so the body only gets its angles changed
			if (renderMode == ToddlerRenderMode.Crawling)
            {
				if (bodyFacing == Rot4.East)
				{
					bodyAngle = 40f;
					headAngle = bodyAngle / 2;
				}
				if (bodyFacing == Rot4.West)
				{
					bodyAngle = -40f;
					headAngle = bodyAngle / 2;
				}
				if (bodyFacing == Rot4.South)
				{
					bodyAngle = 180f;
				}
			}
			if (renderMode == ToddlerRenderMode.WigglingInCrib)
            {
				float wiggleAngle = WiggleAngle(pawn, 15f, 120, WiggleFunc.Sine);
				bodyAngle = wiggleAngle;
				headAngle = wiggleAngle;
            }
			//if we're toddling we rotate around a separate rotation centre
			//so we also need to apply offsets to the body
			if (renderMode == ToddlerRenderMode.Toddling)
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

			headLoc = bodyLoc + headOffset.RotatedBy(bodyAngle);

			Quaternion bodyQuat = Quaternion.AngleAxis(bodyAngle, Vector3.up);
			Quaternion headQuat = Quaternion.AngleAxis(headAngle, Vector3.up);

			//other layers are defined by y-axis (ie depth) offsets
			Vector3 utilityLoc = bodyLoc;
			utilityLoc.y += bodyFacing == Rot4.South ? 0.00579150533f : 0.0289575271f;
			Vector3 bodyApparelLoc = bodyLoc;
			bodyApparelLoc.y += bodyFacing == Rot4.North ? 0.0231660213f : 3f / 148f;
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

			if (renderBody)
			{
				DrawPawnBody(pawnRenderer, bodyLoc, bodyAngle, bodyFacing, bodyDrawType, flags, out bodyMesh);
				if (bodyDrawType == RotDrawMode.Fresh && pawnRenderer.graphics.furCoveredGraphic != null)
				{
					DrawPawnFur(pawnRenderer, shellLoc, bodyFacing, bodyQuat, flags);
				}
				if (bodyDrawType == RotDrawMode.Fresh)
				{
					pawnRenderer.WoundOverlays.RenderPawnOverlay(woundLoc1, bodyMesh, bodyQuat, flags.FlagSet(PawnRenderFlags.DrawNow), PawnOverlayDrawer.OverlayLayer.Body, bodyFacing, false);
				}
				if (flags.FlagSet(PawnRenderFlags.Clothes))
				{
					DrawBodyApparel(pawnRenderer, bodyApparelLoc, utilityLoc, bodyMesh, bodyAngle, bodyFacing, flags);
				}
				if (ModLister.BiotechInstalled && pawn.genes != null)
				{
					DrawBodyGenes(pawnRenderer, bodyLoc, bodyQuat, bodyAngle, bodyFacing, bodyDrawType, flags);
				}
				if (bodyDrawType == RotDrawMode.Fresh)
				{
					pawnRenderer.WoundOverlays.RenderPawnOverlay(woundLoc2, bodyMesh, bodyQuat, flags.FlagSet(PawnRenderFlags.DrawNow), PawnOverlayDrawer.OverlayLayer.Body, bodyFacing, true);
				}
				if (bodyDrawType == RotDrawMode.Fresh && pawnRenderer.FirefoamOverlays.IsCoveredInFoam)
				{
					pawnRenderer.FirefoamOverlays.RenderPawnOverlay(firefoamLoc, bodyMesh, bodyQuat, flags.FlagSet(PawnRenderFlags.DrawNow), PawnOverlayDrawer.OverlayLayer.Body, bodyFacing);
				}
			}


			if (pawnRenderer.graphics.headGraphic != null)
			{
				Mesh headMesh = null;

				Material material = pawnRenderer.graphics.HeadMatAt(bodyFacing, bodyDrawType, flags.FlagSet(PawnRenderFlags.HeadStump), flags.FlagSet(PawnRenderFlags.Portrait), !flags.FlagSet(PawnRenderFlags.Cache));
				if (material != null)
				{
					headMesh = HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn(pawn).MeshAt(bodyFacing);
					GenDraw.DrawMeshNowOrLater(headMesh, headLoc, headQuat, material, flags.FlagSet(PawnRenderFlags.DrawNow));
				}

				if (bodyDrawType == RotDrawMode.Fresh)
				{
					pawnRenderer.WoundOverlays.RenderPawnOverlay(headLoc, bodyMesh, headQuat, flags.FlagSet(PawnRenderFlags.DrawNow), PawnOverlayDrawer.OverlayLayer.Head, bodyFacing);
				}
				if (pawnRenderer.graphics.headGraphic != null)
				{
					DrawHeadHair(pawnRenderer, headLoc, Vector3.zero, headAngle, bodyFacing, bodyFacing, bodyDrawType, flags, renderBody);
				}
				if (bodyDrawType == RotDrawMode.Fresh && pawnRenderer.FirefoamOverlays.IsCoveredInFoam && headMesh != null)
				{
					pawnRenderer.FirefoamOverlays.RenderPawnOverlay(firefoamHeadLoc, headMesh, headQuat, flags.FlagSet(PawnRenderFlags.DrawNow), PawnOverlayDrawer.OverlayLayer.Head, bodyFacing);
				}
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
		static bool Prefix(PawnRenderer __instance, Pawn ___pawn, Vector3 rootLoc, ref float angle, bool renderBody, ref Rot4 bodyFacing, RotDrawMode bodyDrawType, PawnRenderFlags flags)
		{
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

			//Log.Message("parentHolder: " + ___pawn.ParentHolder.ToString());
			IThingHolder parentHolder = ___pawn.ParentHolder;

			if (!(parentHolder is Map || parentHolder is Pawn_CarryTracker)) return true;

			bool isBugWatching = parentHolder is Map 
				&& ___pawn.jobs != null && ___pawn.jobs.curJob != null
				&& ___pawn.CurJobDef == Toddlers_DefOf.ToddlerBugwatching && ___pawn.jobs.curJob.targetA.Cell == ___pawn.Position;
			if (isBugWatching)
			{
				if (bodyFacing == Rot4.East)
				{
					angle = 40f;
					return true;
				}
				if (bodyFacing == Rot4.West)
				{
					angle = -40f;
					return true;
				}
			}

			//baby carry angle relies on them being downed therefore not in the standing posture
			//therefore this is a copy of the same logic to draw toddlers at the right angle when held
			bool isCarried = parentHolder is Pawn_CarryTracker && ___pawn.GetPosture() == PawnPosture.Standing;
			if (isCarried)
			{
				angle = (((parentHolder as Pawn_CarryTracker).pawn.Rotation == Rot4.West) ? 290f : 70f) + (parentHolder as Pawn_CarryTracker).pawn.Drawer.renderer.BodyAngle();
				bodyFacing = (!((parentHolder as Pawn_CarryTracker).pawn.Rotation == Rot4.West) ? Rot4.West : Rot4.East);
				return true;
			}

			bool isCrawling = parentHolder is Map && ToddlerUtility.IsCrawler(___pawn) && ___pawn.GetPosture() == PawnPosture.Standing && ___pawn.pather.Moving;
			if (isCrawling && Toddlers_Settings.customRenderer)
			{
				ToddlerRenderer.RenderToddlerInternal(__instance, ToddlerRenderer.ToddlerRenderMode.Crawling, ___pawn, rootLoc, angle, renderBody, bodyFacing, bodyDrawType, flags);
				return false;
			}

			bool isWobbling = parentHolder is Map && ToddlerUtility.IsWobbly(___pawn) && ___pawn.GetPosture() == PawnPosture.Standing && ___pawn.pather.Moving;
			if (isWobbling && Toddlers_Settings.customRenderer)
			{
				ToddlerRenderer.RenderToddlerInternal(__instance, ToddlerRenderer.ToddlerRenderMode.Toddling, ___pawn, rootLoc, angle, renderBody, bodyFacing, bodyDrawType, flags);
				return false;
			}

			bool isWigglingInCrib = parentHolder is Map && ___pawn.CurJob != null 
				&& ((___pawn.CurJobDef.reportString != null && ___pawn.CurJobDef.reportString.Contains("wiggling"))
				|| (___pawn.CurJob.reportStringOverride != null && ___pawn.CurJob.reportStringOverride.Contains("wiggling")));
			if (isWigglingInCrib && Toddlers_Settings.customRenderer)
			{
				ToddlerRenderer.RenderToddlerInternal(__instance, ToddlerRenderer.ToddlerRenderMode.WigglingInCrib, ___pawn, rootLoc, angle, renderBody, bodyFacing, bodyDrawType, flags);
				return false;
			}

			bool isLayingAngleInCrib = parentHolder is Map && ___pawn.CurJobDef == Toddlers_DefOf.LayAngleInCrib;
			if (isLayingAngleInCrib)
            {
				JobDriver_LayAngleInCrib driver = (JobDriver_LayAngleInCrib)___pawn.jobs.curDriver;
				angle = driver.angle;
				return true;
            }

			return true;
		}
	}
}
