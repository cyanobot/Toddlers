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
    class Hediff_LearningToWalk : Hediff_ToddlerLearning
    {
        private const float MAX_WOBBLE_PENALTY = -0.5f;

        private float cachedWobbleMagnitude = AnimationUtility.MAX_WOBBLE_MAGNITUDE;
        private int cachedWobblePeriod = AnimationUtility.MAX_WOBBLE_PERIOD;
        private float cachedWobblePenalty = MAX_WOBBLE_PENALTY;

        public float WobbleMagnitude => cachedWobbleMagnitude;
        public int WobblePeriod => cachedWobblePeriod;

        public override string SettingName => "learningFactor_Walk";

        public override void Tick()
        {
            base.Tick();

            if (pawn.IsHashIntervalTick(60))
            {
                cachedWobblePenalty = MAX_WOBBLE_PENALTY * (1f - Progress);
                cachedWobbleMagnitude = Mathf.Lerp(AnimationUtility.MAX_WOBBLE_MAGNITUDE, 0f, Progress); ;
                cachedWobblePeriod = (int)Mathf.Lerp(AnimationUtility.MAX_WOBBLE_PERIOD, AnimationUtility.MIN_WOBBLE_PERIOD, Progress);
            }

            AnimationDef animation = null;
            if (CurStageIndex == 0)
            {
                animation = Toddlers_AnimationDefOf.ToddlerCrawl;
            } 
            else if (CurStageIndex == 1)
            {
                animation = Toddlers_AnimationDefOf.ToddlerWobble;
            }
            AnimationUtility.SetLocomotionAnimation(pawn, animation);
        }

        public float Progress
        {
            get
            {
                if (CurStageIndex != 1) return 0f;
                return (Severity - CurStage.minSeverity) / (1f - CurStage.minSeverity);
            }
        }


        //TODO: This approach doesn't work! Stages are defined per def, not per hediff
        /*
        public override void OnUpdate(int stageIndex)
        {
            if (stageIndex == 1)
            {
                CurStage.capMods.Clear();
                CurStage.capMods.Add(this.WobblyCapacityModifier);
            }
        }
        */

        public override void OnStageUp(int newStageIndex)
        {
            if (newStageIndex == 1 && !(pawn.ParentHolder is Building_GrowthVat))
            {
                Find.LetterStack.ReceiveLetter("LetterTitleFirstSteps".Translate(), "LetterTextFirstSteps".Translate(pawn.Named("PAWN")), LetterDefOf.NeutralEvent, pawn);
            }
        }

        public override void PreRemoved()
        {
            base.PreRemoved();
            if (pawn.Drawer.renderer.CurAnimation == Toddlers_AnimationDefOf.ToddlerCrawl
                || pawn.Drawer.renderer.CurAnimation == Toddlers_AnimationDefOf.ToddlerWobble)
            {
                pawn.Drawer.renderer.SetAnimation(null);
            }
        }

        public PawnCapacityModifier WobblyCapacityModifier
        {
            get
            {
                if (CurStageIndex != 1) return null;

                PawnCapacityModifier result= new PawnCapacityModifier();

                result.capacity = PawnCapacityDefOf.Moving;
                result.offset = cachedWobblePenalty;

                return result;
            }
        }
    
        
    }
}
