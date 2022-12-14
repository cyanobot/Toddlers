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
        public static bool IsToddler(Pawn p)
        {
            return p != null && p.ageTracker.CurLifeStage == Toddlers_DefOf.HumanlikeToddler;
        }

        public static bool IsLiveToddler(Pawn p)
        {
            return (ChildcareUtility.CanSuckle(p, out var _) && IsToddler(p));
        }

        public static float PercentGrowth(Pawn p)
        {
            //2 years * 60 days per year * 60000 ticks per day
            float toddlerStageInTicks = 2f * 60f * 60000f;
            //age up at 1 yearold
            float ticksSinceBaby = (float)p.ageTracker.AgeBiologicalTicks - (60f * 60000f);
            return (ticksSinceBaby / toddlerStageInTicks);
        }

        public static float GetLearningPerTickBase(Storyteller storyteller = null)
        {
            //2 years * 60 days per year * 60000 ticks per day
            if (storyteller == null) storyteller = Find.Storyteller;
            float ticksAsToddler = 2 * 60 * 60000 / Find.Storyteller.difficulty.childAgingRate;
            return 1 / ticksAsToddler;
        }

        public static float GetLoneliness(Pawn pawn)
        {
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(Toddlers_DefOf.ToddlerLonely) as Hediff_ToddlerLonely;
            if (hediff == null) return 0f;
            else return hediff.Severity;
        }

        public static bool IsCrawler(Pawn pawn)
        {
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
