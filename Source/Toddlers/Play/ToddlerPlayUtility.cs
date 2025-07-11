﻿using RimWorld;
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
        public const float PlayNeedSatisfiedPerTick = 1.2E-04f;
        public const float LonelinessCuredPerTick = 6E-04f;
        public const int PlayDuration = 2000;
        public const float BaseLonelinessRate = 0.0015f;

        public static List<ThingDef> cachedTelevisionDefs = new List<ThingDef>();
        public static int cachedTelevisionMaxParticipants = -1;

        public static List<ThingDef> TelevisionDefs
        {
            get
            {
                if (cachedTelevisionDefs.NullOrEmpty()) cachedTelevisionDefs = DefDatabase<JoyGiverDef>.GetNamed("WatchTelevision").thingDefs;
                return cachedTelevisionDefs;
            }
        }

        public static int TelevisionMaxParticipants
        {
            get
            {
                if (cachedTelevisionMaxParticipants== -1) cachedTelevisionMaxParticipants = DefDatabase<JobDef>.GetNamed("WatchTelevision").joyMaxParticipants;
                return cachedTelevisionMaxParticipants;
            }
        }

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

        public static bool ToddlerPlayTickCheckEnd(Pawn pawn, int delta = 1)
        {
            if (pawn.needs.play.CurLevel <= GetMaxPlay(pawn))
            {
                pawn.needs.play.Play(delta * PlayNeedSatisfiedPerTick * BabyPlayUtility.GetRoomPlayGainFactors(pawn));
            }
            if (pawn.needs.play.CurLevel >= 0.99f)
            {
                pawn.jobs.curDriver.EndJobWith(JobCondition.Succeeded);
                return true;
            }
            return false;
        }

        public static void CureLoneliness(Pawn pawn, int delta = 1)
        {
            if (!ToddlerUtility.IsToddler(pawn)) return;
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(Toddlers_DefOf.ToddlerLonely);
            if (hediff != null) hediff.Severity -= delta * LonelinessCuredPerTick;
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

        public static bool IsToddlerPlaying(Pawn toddler)
        {
            if (toddler == null) return false;
            if (!toddler.Spawned) return false;
            JobDriver curDriver = toddler.jobs?.curDriver;
            if (curDriver == null) return false;
            if (curDriver is JobDriver_ToddlerBugwatching 
                || curDriver is JobDriver_ToddlerFiregazing
                || curDriver is JobDriver_ToddlerFloordrawing
                || curDriver is JobDriver_ToddlerPlayDecor
                || curDriver is JobDriver_ToddlerPlayToys
                || curDriver is JobDriver_ToddlerSkydreaming
                || curDriver is JobDriver_ToddlerWatchTelevision)
            {
                return true;
            }
            return false;
        }
    }
}
