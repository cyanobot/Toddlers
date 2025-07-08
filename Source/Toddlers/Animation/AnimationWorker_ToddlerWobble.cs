using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using LudeonTK;
using static Toddlers.AnimationUtility;

namespace Toddlers
{
#if RW_1_5
    class AnimationWorker_ToddlerWobble : AnimationWorker
    {
        /*
        [TweakValue("AA", 0.1f, 0.9f)]
        public static float PIVOT_X = 0.5f;
        [TweakValue("AA", 0.1f, 0.9f)]
        public static float PIVOT_Z = 0.2f;
        public static Vector2 Pivot => new Vector2(PIVOT_X, PIVOT_Z);
        */

        private Hediff_LearningToWalk walkHediff = null;

        public Hediff_LearningToWalk WalkHediff
        {
            get
            {
                //Log.Message("WalkHediff - walkHediff: " + walkHediff);
                if (walkHediff == null || walkHediff.pawn != this.pawn)
                {
                    walkHediff = (Hediff_LearningToWalk)pawn.health.hediffSet.GetFirstHediffOfDef(Toddlers_DefOf.LearningToWalk);
                }
                return walkHediff;
            }
        }

        public AnimationWorker_ToddlerWobble(AnimationDef def, Pawn pawn, AnimationPart part, PawnRenderNode node)
        : base(def, pawn, part, node)
        {

        }

        public override bool Enabled()
        {
            if (!pawn.Spawned) return false;
            if (!base.Enabled()) return false;
            if (!pawn.pather.Moving) return false;

            return true;
        }

        public override float AngleAtTick(int tick, PawnDrawParms parms)
        {
            //part.pivot = Pivot;

            //Log.Message("AngleAtTick - tick: " + tick + ", WobblePeriod: " + WalkHediff.WobblePeriod 
            //    + ", WobbleMagnitude: " + WalkHediff.WobbleMagnitude);
            if (WalkHediff == null) return 0f;

            float x = (float)(Find.TickManager.TicksGame % WalkHediff.WobblePeriod) / (float)WalkHediff.WobblePeriod;
            //Log.Message("tick: " + tick + ", %period: " + (tick % WalkHediff.WobblePeriod) + ", x: " + x);
            float mag = WalkHediff.WobbleMagnitude;
            //Log.Message("x: " + x + ", mag: " + mag + ", toddleCurve.Evaluate: " + toddleCurve.Evaluate(x));
            return mag * Waveform(f => toddleCurve.Evaluate(f), x);
        }
    }
#else
    class AnimationWorker_ToddlerWobble : AnimationWorker_Toddler
    {
        /*
        [TweakValue("AA", 0.1f, 0.9f)]
        public static float PIVOT_X = 0.5f;
        [TweakValue("AA", 0.1f, 0.9f)]
        public static float PIVOT_Z = 0.2f;
        public static Vector2 Pivot => new Vector2(PIVOT_X, PIVOT_Z);
        */

        private Hediff_LearningToWalk walkHediff = null;

        public Hediff_LearningToWalk WalkHediff(Pawn pawn)
        {
            //Log.Message("WalkHediff - walkHediff: " + walkHediff);
            if (walkHediff == null || walkHediff.pawn != pawn)
            {
                walkHediff = (Hediff_LearningToWalk)pawn.health.hediffSet.GetFirstHediffOfDef(Toddlers_DefOf.LearningToWalk);
            }
            return walkHediff;
        }

        public override float AngleAtTick(int tick, AnimationDef def, PawnRenderNode node, AnimationPart part, PawnDrawParms parms)
        {
            //part.pivot = Pivot;

            //LogUtil.DebugLog("AnimationWorker_ToddlerWobble.AngleAtTick - tick: " + tick + ", WobblePeriod: " + WalkHediff(parms.pawn)?.WobblePeriod 
            //    + ", WobbleMagnitude: " + WalkHediff(parms.pawn)?.WobbleMagnitude);
            if (WalkHediff(parms.pawn) == null) return 0f;

            float x = (float)(Find.TickManager.TicksGame % walkHediff.WobblePeriod) / (float)walkHediff.WobblePeriod;
            //Log.Message("tick: " + tick + ", %period: " + (tick % WalkHediff.WobblePeriod) + ", x: " + x);
            float mag = walkHediff.WobbleMagnitude;
            //Log.Message("x: " + x + ", mag: " + mag + ", toddleCurve.Evaluate: " + toddleCurve.Evaluate(x));
            float result = mag * Waveform(f => toddleCurve.Evaluate(f), x);

            //LogUtil.DebugLog($"AnimationWorker_ToddlerWobble.AngleAtTick - tick: {tick}, pawn: {parms.pawn}" +
            //    $", result: {result}");
            return result;
        }

        public override bool Enabled(AnimationDef def, PawnRenderNode node, AnimationPart part, PawnDrawParms parms)
        {
            /*
            LogUtil.DebugLog($"AnimationWorker_ToddlerWobble.Enabled - def: {def}, pawn: {parms.pawn}" +
                $", node: {node}, part: {part}, base.Enabled: {base.Enabled(def, node, part, parms)}" +
                $", Spawned: {parms.pawn.Spawned}, Moving: {parms.pawn.pather.Moving}" +
                $", node.Props.drawData: {node.Props.drawData}, parms.flags: {parms.flags}"
                );
            */

            if (!base.Enabled(def, node, part, parms)) return false;

            if (!parms.pawn.Spawned) return false;
            if (!parms.pawn.pather.Moving) return false;

            return true;
        }
    }
#endif

}
