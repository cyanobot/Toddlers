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

namespace Toddlers
{
    public static class ToddlerUtility
    {
        public const float BASE_MIN_AGE = 1f;
        public const float BASE_MAX_AGE = 3f;

        public static float ToddlerMinAge(Pawn p)
        {
            return BASE_MIN_AGE;
            /*
            if (!Toddlers_Mod.HARLoaded)
            {
                return BASE_MIN_AGE;
            }
            else
            {
                AlienRace alienRace = Patch_HAR.GetAlienRaceWrapper(p);
                return alienRace.toddlerMinAge;
            }
            */
        }

        public static float ToddlerMaxAge(Pawn p)
        {
            return BASE_MAX_AGE;
            /*
            if (!Toddlers_Mod.HARLoaded)
            {
                return BASE_MAX_AGE;
            }
            else
            {
                AlienRace alienRace = Patch_HAR.GetAlienRaceWrapper(p);
                return alienRace.lifeStageChild.minAge;
            }
            */
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
