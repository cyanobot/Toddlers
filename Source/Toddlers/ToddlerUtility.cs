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
using Verse.AI.Group;
using static Toddlers.Toddlers_Mod;

namespace Toddlers
{
    public static class ToddlerUtility
    {
        public const float BASE_MIN_AGE = 1f;
        public const float BASE_END_AGE = 3f;

        public static float ToddlerMinAge(Pawn p)
        {
            if (HARLoaded)
            {
                AlienRace alienRace = HARUtil.GetAlienRaceWrapper(p);
                if (alienRace != null) return alienRace.toddlerMinAge;
            }
            return BASE_MIN_AGE;

        }

        public static float ToddlerEndAge(Pawn p)
        {
            if (HARLoaded)
            {
                AlienRace alienRace = HARUtil.GetAlienRaceWrapper(p);
                if (alienRace != null) return alienRace.toddlerEndAge;
            }
            return BASE_END_AGE;
        }

        public static float ToddlerStageInTicks(Pawn p)
        {
            //time in years * 60 days per year * 60000 ticks per day
            return (ToddlerEndAge(p) - ToddlerMinAge(p)) * 60f * 60000f;
        }

        public static bool IsToddler(Pawn p)
        {
            if (p == null) return false;
            if (p.ageTracker.CurLifeStage == Toddlers_DefOf.HumanlikeToddler) return true;
            if (HARLoaded)
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
            float toddlerStageInTicks = ToddlerStageInTicks(p);
            //age up at 1 yearold
            float ticksSinceBaby = (float)p.ageTracker.AgeBiologicalTicks - (ToddlerMinAge(p) * 60f * 60000f);

            //Log.Message("MaxAge: " + ToddlerMaxAge(p) + ", MinAge: " + ToddlerMinAge(p) + ", toddlerStageInTicks: " + toddlerStageInTicks
            //    + ", ticksSinceBaby: " + ticksSinceBaby + ", PercentGrowth: " + (ticksSinceBaby / toddlerStageInTicks));
            return (ticksSinceBaby / toddlerStageInTicks);
        }

        public static bool IsBabyBusy(Pawn baby)
        {
            //busy if drafted
            if (baby.Drafted) return true;

            //busy if attending a ceremony/caravan/etc
            if (baby.GetLord() != null) return true;

            //busy if eating urgently
            if (FeedingUtility.IsToddlerEatingUrgently(baby)) return true;

            //busy if another pawn has already targeted baby
            if (baby.MapHeld.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer).Any(
                p => p.CurJob != null && p.CurJob.AnyTargetIs(baby)
                ))
            {
                return true;
            }

            //otherwise not busy
            return false;
        }
    }
}
