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
        public static JobDef BeDressed;
        public static JobDef ToddlerRemoveApparel;
        public static JobDef KidnapToddler;

        public static LifeStageDef HumanlikeToddler;

        //public static ThingDef BabyApparelMakeableBase;
        public static ThingDef Apparel_BabyOnesie;
        public static ThingDef Apparel_BabyTuque;
        public static ThingDef Apparel_BabyShadecone;

        public static ThoughtDef BabyNoExpectations;

        public static ToddlerPlayDef ToddlerSkydreaming;
        public static ToddlerPlayDef ToddlerWatchTelevision;

    }

}
