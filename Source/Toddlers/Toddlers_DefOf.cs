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
    [DefOf]
    public static class Toddlers_DefOf
    {
        public static DefListDef FiregazingTargets;
        public static DefListDef WearableByBaby;

        public static DutyDef ToddlerLoiter;

        public static HediffDef LearningToWalk;
        public static HediffDef LearningManipulation;
        public static HediffDef ToddlerLonely;

        public static JobDef ToddlerBugwatching;
        public static JobDef LeaveCrib;
        public static JobDef RestIdleInCrib;
        public static JobDef LayAngleInCrib;
        public static JobDef WiggleInCrib;
        public static JobDef PutInCrib;
        public static JobDef BeDressed;
        public static JobDef ToddlerRemoveApparel;
        public static JobDef KidnapToddler;
        public static JobDef BePlayedWith;

#if RW_1_5
        public static JobDef UndressBaby;
        public static JobDef DressBaby;
#endif

        public static LifeStageDef HumanlikeToddler;

        public static MentalStateDef RemoveClothes;

        public static ThingCategoryDef ApparelBaby;

        public static ThingDef Apparel_BabyOnesie;
        //public static ThingDef Apparel_BabyTuque;
        //public static ThingDef Apparel_BabyShadecone;
        public static ThingDef Apparel_BabyTribal;
        public static ThingDef Toddlers_Filth_BabyFood;

        public static ThoughtDef BabyNoExpectations;
        public static ThoughtDef Toddlers_TraumaticCrash;

        public static ToddlerPlayDef ToddlerSkydreaming;
        public static ToddlerPlayDef ToddlerWatchTelevision;
        static Toddlers_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(Toddlers_DefOf));
        }

    }

    [DefOf]
    public static class Toddlers_ThinkTreeDefOf
    {
        public static ThinkTreeDef HumanlikeToddler;
        public static ThinkTreeDef HumanlikeToddlerConstant;
        static Toddlers_ThinkTreeDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(Toddlers_ThinkTreeDefOf));
        }
    }
}
