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
    public static class ToddlerUtility
    {
        public const float BASE_MIN_AGE = 1f;
        public const float BASE_MAX_AGE = 3f;

        public static float ToddlerMinAge(Pawn p)
        {
            if (!Toddlers_Mod.HARLoaded)
            {
                return BASE_MIN_AGE;
            }
            else
            {
                AlienRace alienRace = Patch_HAR.GetAlienRaceWrapper(p);
                return alienRace.toddlerMinAge;
            }
        }

        public static float ToddlerMaxAge(Pawn p)
        {
            if (!Toddlers_Mod.HARLoaded)
            {
                return BASE_MAX_AGE;
            }
            else
            {
                AlienRace alienRace = Patch_HAR.GetAlienRaceWrapper(p);
                return alienRace.lifeStageChild.minAge;
            }
        }

        public static bool IsToddler(Pawn p)
        {
            if (p == null) return false;
            if (p.ageTracker.CurLifeStage == Toddlers_DefOf.HumanlikeToddler) return true;
            if (Toddlers_Mod.HARLoaded)
            {
                if (p.ageTracker.CurLifeStage.workerClass == typeof(LifeStageWorker_HumanlikeToddler)) return true;
            }
            return false;
        }

        public static bool IsLiveToddler(Pawn p)
        {
            return (ChildcareUtility.CanSuckle(p, out var _) && IsToddler(p));
        }

        public static float PercentGrowth(Pawn p)
        {
            //2 years * 60 days per year * 60000 ticks per day
            float toddlerStageInTicks = (ToddlerMaxAge(p) - ToddlerMinAge(p)) * 60f * 60000f;
            //age up at 1 yearold
            float ticksSinceBaby = (float)p.ageTracker.AgeBiologicalTicks - (ToddlerMinAge(p) * 60f * 60000f);
<<<<<<< Updated upstream
=======
            //Log.Message("MaxAge: " + ToddlerMaxAge(p) + ", MinAge: " + ToddlerMinAge(p) + ", toddlerStageInTicks: " + toddlerStageInTicks
            //    + ", ticksSinceBaby: " + ticksSinceBaby + ", PercentGrowth: " + (ticksSinceBaby / toddlerStageInTicks));
>>>>>>> Stashed changes
            return (ticksSinceBaby / toddlerStageInTicks);
        }

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

            if (Toddlers_Mod.HARLoaded && !Patch_HAR.GetAlienRaceWrapper(p).humanlikeGait) return;

            Hediff_LearningToWalk hediff_LearningToWalk = (Hediff_LearningToWalk)HediffMaker.MakeHediff(Toddlers_DefOf.LearningToWalk, p);
            hediff_LearningToWalk.Severity = Mathf.Min(1f, percentAge / Toddlers_Settings.learningFactor_Walk);
            p.health.AddHediff(hediff_LearningToWalk);

            return;
        }

        public static float GetLearningPerTickBase(Pawn p, Storyteller storyteller = null)
        {
            //2 years * 60 days per year * 60000 ticks per day
            if (storyteller == null) storyteller = Find.Storyteller;
            float ticksAsToddler = (ToddlerMaxAge(p) - ToddlerMinAge(p)) * 60 * 60000 / Find.Storyteller.difficulty.childAgingRate;
            return 1 / ticksAsToddler;
        }

        /*
        public static float GetLoneliness(Pawn pawn)
        {
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(Toddlers_DefOf.ToddlerLonely) as Hediff_ToddlerLonely;
            if (hediff == null) return 0f;
            else return hediff.Severity;
        }
        */

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
            Hediff hediff = p.health.hediffSet.GetFirstHediffOfDef(Toddlers_DefOf.LearningManipulation) as Hediff_LearningManipulation;
            if (hediff == null || hediff.CurStageIndex >= 1) return true;
            return false;
        }

        public static bool EatsOnFloor(Pawn p)
        {
            Hediff hediff = p.health.hediffSet.GetFirstHediffOfDef(Toddlers_DefOf.LearningManipulation) as Hediff_LearningManipulation;
            if (hediff != null && hediff.CurStageIndex == 1) return true;
            return false;
        }

        public static bool InCrib(Pawn p)
        {
            //Building_Bed bed = p.CurrentBed();
            //return bed != null && IsCrib(bed) ;
            if (!(p.ParentHolder is Map) || p.pather.Moving) return false;

            Building_Bed building_Bed = null;
            List<Thing> thingList = p.Position.GetThingList(p.Map);
            for (int i = 0; i < thingList.Count; i++)
            {
                building_Bed = thingList[i] as Building_Bed;
                if (building_Bed != null && IsCrib(building_Bed)) break;

            }
            if (building_Bed == null) return false;
            else return true;
        }

        public static Building_Bed GetCurrentCrib(Pawn p)
        {
            Building_Bed bed = null;
            List<Thing> thingList = p.Position.GetThingList(p.Map);
            for (int i = 0; i < thingList.Count; i++)
            {
                bed = thingList[i] as Building_Bed;
                if (bed != null && IsCrib(bed)) return bed;
            }
            return null;
        }

        public static bool IsCrib(Building_Bed bed)
        {
            return bed.def == DefDatabase<ThingDef>.GetNamed("Crib");
        }

    }
}
