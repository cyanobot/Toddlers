using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;
using LudeonTK;

namespace Toddlers
{
    public static class AnimationUtility
    {
        //[TweakValue("AA", 0f, 30f)]
        public const float MAX_WOBBLE_MAGNITUDE = 10f;

        //[TweakValue("AA", 20, 150)]
        public const int MAX_WOBBLE_PERIOD = 60;
        public const int MIN_WOBBLE_PERIOD = 30;
        public const float CRAWL_ANGLE = 40F;

        //[TweakValue("AA", 0.1f, 0.9f)]
        public const float TODDLE_WAVEFORM_THRESHOLD_X = 0.4f;

        //[TweakValue("AA", 0.1f, 0.9f)]
        public const float TODDLE_WAVEFORM_THRESHOLD_Y = 0.7f;

        public static SimpleCurve toddleCurve = new SimpleCurve(new CurvePoint[] 
        { 
            new CurvePoint(0f,0f),
            new CurvePoint(TODDLE_WAVEFORM_THRESHOLD_X,TODDLE_WAVEFORM_THRESHOLD_Y),
            new CurvePoint(1f,1f)
        });

		public static float Waveform(Func<float, float> quarterform, float x)
		{
			if (x < 0 || x > 1)
            {
                Log.Error("Toddlers.AnimationUtility.Waveform - input must be between 0 and 1. Received x: " + x);
                return 0f;
            }

			if (x <= 0.25f) return quarterform(4f*x);
			if (x <= 0.5f) return quarterform(4f*(0.5f-x));
            if (x <= 0.75f) return -1f * quarterform(4f*(x-0.5f));
            return -1f * quarterform(4f * (1f - x));
		}

        public static void SetLocomotionAnimation(Pawn pawn, AnimationDef animation)
        {
            if (!pawn.Spawned || pawn.DeadOrDowned || pawn.Drawer?.renderer == null)
                return;

            AnimationDef curAnimation = pawn.Drawer.renderer.CurAnimation;
            if (curAnimation == animation
                || curAnimation == Toddlers_AnimationDefOf.Bugwatch
                || curAnimation == Toddlers_AnimationDefOf.LayAngleInCrib
                || curAnimation == Toddlers_AnimationDefOf.WiggleInCrib)
                return;

            pawn.Drawer.renderer.SetAnimation(animation);
        }
    }
}
