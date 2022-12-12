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
    public static class ToddlerPlayUtility
    {
        public const float PlayNeedSatisfiedPerTick = 1.2E-05f;
        public const float LonelinessCuredPerTick = 1.2E-04f;
        public const int PlayDuration = 2000;

        public static float GetLoneliness(Pawn pawn)
        {
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(Toddlers_DefOf.ToddlerLonely);
            if (hediff == null)
            {
                hediff = pawn.health.AddHediff(Toddlers_DefOf.ToddlerLonely);
            }
            return hediff.Severity;
        }

        public static float GetMaxPlay(Pawn pawn)
        {
            return (1f - GetLoneliness(pawn));
        }

        public static bool ToddlerPlayTickCheckEnd(Pawn pawn)
        {
            pawn.needs.play.Play(PlayNeedSatisfiedPerTick * BabyPlayUtility.GetRoomPlayGainFactors(pawn));
            if (pawn.needs.play.CurLevel >= GetMaxPlay(pawn))
            {
                pawn.jobs.curDriver.EndJobWith(JobCondition.Succeeded);
                return true;
            }
            return false;
        }

        public static void CureLoneliness(Pawn pawn)
        {
            if (pawn.ageTracker.CurLifeStage != Toddlers_DefOf.HumanlikeToddler) return;
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(Toddlers_DefOf.ToddlerLonely);
            if (hediff != null) hediff.Severity -= LonelinessCuredPerTick;
        }

        public static bool ToddlerPlayedWithTickCheckEnd(Pawn pawn)
        {
            if (pawn.ageTracker.CurLifeStage == Toddlers_DefOf.HumanlikeToddler)
            {
                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(Toddlers_DefOf.ToddlerLonely);
                if (hediff != null) hediff.Severity -= LonelinessCuredPerTick;
            }
            return false;
        }
        public enum DecorType
        {
            Horse,
            Car,
            Xylophone,
            Hanoi,
            Phone,
            None
        }

        public static DecorType GetDecorType(Thing decor)
        {
            if (decor.def != ThingDefOf.BabyDecoration) return DecorType.None;
            return (DecorType)(decor.thingIDNumber % 5);
        }
        public static bool PlayOnCell(Thing decor)
        {
            DecorType decorType = GetDecorType(decor);
            if (decorType == DecorType.Horse)
            {
                return true;
            }
            return false;
        }
    }
}
