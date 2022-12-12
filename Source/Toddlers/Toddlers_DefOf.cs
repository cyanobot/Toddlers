using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toddlers
{
    [DefOf]
    static class Toddlers_DefOf
    {
        public static HediffDef LearningToWalk;
        public static HediffDef LearningManipulation;
        public static HediffDef ToddlerLonely;

        public static JobDef ToddlerBugwatching;
        public static JobDef LeaveCrib;
        public static JobDef RestIdleInCrib;
        public static JobDef LayAngleInCrib;
        public static JobDef WiggleInCrib;
        public static JobDef PutInCrib;
        public static JobDef DressBaby;
        public static JobDef UndressBaby;

        public static LifeStageDef HumanlikeToddler;

        public static ToddlerPlayDef ToddlerSkydreaming;
        public static ToddlerPlayDef ToddlerWatchTelevision;

    }

}
