using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static Toddlers.ToddlerUtility;
using static Toddlers.Toddlers_Mod;

namespace Toddlers
{
    public static class ToddlerLearningUtility
    {

        public static void ResetHediffsForAge(Pawn p, bool clearExisting = true)
        {
            if (!IsToddler(p)) return;

            if (p.ageTracker == null || p.health == null) return;

            if (clearExisting)
            {
                List<Hediff_ToddlerLearning> learningHediffs = new List<Hediff_ToddlerLearning>();
                p.health.hediffSet.GetHediffs<Hediff_ToddlerLearning>(ref learningHediffs);
                foreach (Hediff hediff in learningHediffs)
                {
                    p.health.RemoveHediff(hediff);
                }
            }

            float percentAge = PercentGrowth(p);

            Hediff_LearningManipulation hediff_LearningManipulation = (Hediff_LearningManipulation)HediffMaker.MakeHediff(Toddlers_DefOf.LearningManipulation, p);
            hediff_LearningManipulation.Severity = Mathf.Min(1f, percentAge / Toddlers_Settings.learningFactor_Manipulation);
            p.health.AddHediff(hediff_LearningManipulation);

            //if (Toddlers_Mod.HARLoaded && !Patch_HAR.GetAlienRaceWrapper(p).humanlikeGait) return;

            if (HARLoaded && !HARUtil.GetAlienRaceWrapper(p).humanlikeGait) return;

            Hediff_LearningToWalk hediff_LearningToWalk = (Hediff_LearningToWalk)HediffMaker.MakeHediff(Toddlers_DefOf.LearningToWalk, p);
            hediff_LearningToWalk.Severity = Mathf.Min(1f, percentAge / Toddlers_Settings.learningFactor_Walk);
            p.health.AddHediff(hediff_LearningToWalk);

            return;
        }

        public static float GetLearningPerTickBase(Pawn p, Storyteller storyteller = null)
        {
            //2 years * 60 days per year * 60000 ticks per day
            //if (storyteller == null) storyteller = Find.Storyteller;
            float ticksAsToddler = ToddlerStageInTicks(p); // / Find.Storyteller.difficulty.childAgingRate;
            return 1 / ticksAsToddler;
        }

        public static bool IsCrawler(Pawn pawn)
        {
            if (!IsToddler(pawn)) return false;
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(Toddlers_DefOf.LearningToWalk) as Hediff_LearningToWalk;
            if (hediff != null && hediff.CurStageIndex == 0) return true;
            return false;
        }

        public static bool IsWobbly(Pawn pawn)
        {
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(Toddlers_DefOf.LearningToWalk) as Hediff_LearningToWalk;
            if (hediff != null && hediff.CurStageIndex == 1) return true;
            return false;
        }

        public static bool CanDressSelf(Pawn p)
        {
            Hediff hediff = p.health.hediffSet.GetFirstHediffOfDef(Toddlers_DefOf.LearningManipulation) as Hediff_LearningManipulation;
            if (hediff == null || hediff.CurStageIndex >= 2) return true;
            return false;
        }

        public static bool CanFeedSelf(Pawn p)
        {
            Hediff hediff = p.health?.hediffSet?.GetFirstHediffOfDef(Toddlers_DefOf.LearningManipulation) as Hediff_LearningManipulation;
            if (hediff == null || hediff.CurStageIndex >= 1) return true;
            return false;
        }

        public static bool EatsOnFloor(Pawn p)
        {
            Hediff hediff = p.health.hediffSet.GetFirstHediffOfDef(Toddlers_DefOf.LearningManipulation) as Hediff_LearningManipulation;
            if (hediff != null && hediff.CurStageIndex == 1) return true;
            return false;
        }

    }
}
