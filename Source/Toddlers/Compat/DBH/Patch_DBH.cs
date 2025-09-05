using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Toddlers.LogUtil;

namespace Toddlers
{
    public static class Patch_DBH
    {
        public static bool babyHygiene = false;
        public static bool babyBladder = false;


        public static Type t_ContaminationLevel;
        public static Type t_FixtureType;

        public static Type t_NeedsUtil;
        public static MethodInfo m_ShouldHaveNeed;

        public static Type t_Need_Hygiene;
        public static FieldInfo f_lastGainTick;
        public static FieldInfo f_contaminated;

        public static Type t_ClosestSanitation;
        public static MethodInfo m_FindBestCleanWaterSource;
        public static MethodInfo m_FindBestHygieneSource;
        public static MethodInfo m_IsEverUsable;
        public static MethodInfo m_UsableNow;

        public static Type t_SanitationToils;
        public static MethodInfo m_FillBottleFromThing;
        public static MethodInfo m_FillBottleFromCell;

        public static Type t_Building_bath;
        public static FieldInfo f_occupant;
        public static FieldInfo f_contamination_bath;
        public static PropertyInfo p_IsFull;
        public static MethodInfo m_TryFillBath;
        public static MethodInfo m_TryPullPlug;

        public static Type t_Building_washbucket;
        public static FieldInfo f_WaterUsesRemaining;

        public static Type t_SanitationUtil;
        public static MethodInfo m_ContaminationCheckWater;
        public static MethodInfo m_CheckForBlockage;
        public static MethodInfo m_AllFixtures;

        public static bool InitReferences()
        {
            t_ContaminationLevel = AccessTools.TypeByName("DubsBadHygiene.ContaminationLevel");
            if (t_ContaminationLevel == null) return false;
            t_FixtureType = AccessTools.TypeByName("DubsBadHygiene.FixtureType");
            if (t_FixtureType == null) return false;

            t_NeedsUtil = AccessTools.TypeByName("DubsBadHygiene.NeedsUtil");
            if (t_NeedsUtil == null) return false;
            m_ShouldHaveNeed = AccessTools.Method(t_NeedsUtil, "ShouldHaveNeed", new Type[] { typeof(Pawn), typeof(NeedDef) });
            if (m_ShouldHaveNeed == null) return false;

            t_Need_Hygiene = AccessTools.TypeByName("DubsBadHygiene.Need_Hygiene");
            if (t_Need_Hygiene == null) return false;
            f_lastGainTick = AccessTools.Field(t_Need_Hygiene, "lastGainTick");
            if (f_lastGainTick == null) return false;
            f_contaminated = AccessTools.Field(t_Need_Hygiene, "contaminated");
            if (f_contaminated == null) return false;

            t_ClosestSanitation = AccessTools.TypeByName("DubsBadHygiene.ClosestSanitation");
            if (t_ClosestSanitation == null) return false;
            m_FindBestCleanWaterSource = AccessTools.Method(t_ClosestSanitation, "FindBestCleanWaterSource",
                new Type[] { typeof(Pawn), typeof(Pawn), typeof(bool), typeof(float), typeof(ThingDef), typeof(Pawn) });
            if (m_FindBestCleanWaterSource == null) return false;
            m_FindBestHygieneSource = AccessTools.Method(t_ClosestSanitation, "FindBestHygieneSource",
                new Type[] { typeof(Pawn), typeof(bool), typeof(float) });
            if (m_FindBestHygieneSource == null) return false;
            DebugLog("about to try m_IsEverUsable");
            m_IsEverUsable = AccessTools.Method(t_ClosestSanitation, "IsEverUsable");
            if (m_IsEverUsable == null) return false;
            DebugLog("about to try m_UsableNow");
            m_UsableNow = AccessTools.Method(t_ClosestSanitation, "UsableNow",
                new Type[] { typeof(Thing), typeof(Pawn), typeof(bool), typeof(float) });
            if (m_UsableNow == null) return false;

            t_SanitationToils = AccessTools.TypeByName("DubsBadHygiene.SanitationToils");
            //DebugLog("t_SanitationToils: " + t_SanitationToils);
            if (t_SanitationToils == null) return false;
            m_FillBottleFromThing = AccessTools.Method(t_SanitationToils, "FillBottleFromThing",
                new Type[] { typeof(TargetIndex), typeof(bool), t_ContaminationLevel, typeof(bool) });
            //DebugLog("m_FillBottleFromThing: " + m_FillBottleFromThing);
            if (m_FillBottleFromThing == null) return false;
            m_FillBottleFromCell = AccessTools.Method(t_SanitationToils, "FillBottleFromCell",
                new Type[] { typeof(TargetIndex), typeof(bool), typeof(bool) });
            //DebugLog("m_FillBottleFromCell: " + m_FillBottleFromCell);
            if (m_FillBottleFromCell == null) return false;

            t_Building_bath = AccessTools.TypeByName("DubsBadHygiene.Building_bath");
            if (t_Building_bath == null) return false;
            f_occupant = AccessTools.Field(t_Building_bath, "occupant");
            if (f_occupant == null) return false;
            f_contamination_bath = AccessTools.Field(t_Building_bath, "contamination");
            if (f_contamination_bath == null) return false;
            p_IsFull = AccessTools.Property(t_Building_bath, "IsFull");
            if (p_IsFull == null) return false;
            m_TryFillBath = AccessTools.Method(t_Building_bath, "TryFillBath",
                new Type[] { });
            if (m_TryFillBath == null) return false;
            m_TryPullPlug = AccessTools.Method(t_Building_bath, "TryPullPlug",
                new Type[] { });
            if (m_TryPullPlug == null) return false;

            t_Building_washbucket = AccessTools.TypeByName("DubsBadHygiene.Building_washbucket");
            if (t_Building_washbucket == null) return false;
            f_WaterUsesRemaining = AccessTools.Field(t_Building_washbucket, "WaterUsesRemaining");
            if (f_WaterUsesRemaining == null) return false;

            t_SanitationUtil = AccessTools.TypeByName("DubsBadHygiene.SanitationUtil");
            if (t_SanitationUtil == null) return false;
            m_ContaminationCheckWater = AccessTools.Method(t_SanitationUtil, "ContaminationCheckWater",
                new Type[] { typeof(Pawn), t_ContaminationLevel });
            if (m_ContaminationCheckWater == null) return false;
            m_CheckForBlockage = AccessTools.Method(t_SanitationUtil, "CheckForBlockage",
                new Type[] { typeof(Building) });
            if (m_CheckForBlockage == null) return false;
            DebugLog("about to try m_AllFixtures");
            m_AllFixtures = AccessTools.Method(t_SanitationUtil, "AllFixtures",
                new Type[] { typeof(Map) });
            if (m_AllFixtures == null) return false;


            return true;
        }

        public static void GeneratePatches(Harmony harmony)
        {
            if (!InitReferences())
            {
                Log.Error("[Toddlers] Failed to generate patches for Dubs Bad Hygiene");
                return;
            }
                        
            harmony.Patch(m_ShouldHaveNeed,
                postfix: new HarmonyMethod(typeof(Patch_DBH),nameof(ShouldHaveNeed_Postfix)));

        }

        public static bool ShouldHaveNeed_Postfix(bool result, Pawn pawn, NeedDef nd)
        {
            if (ToddlerUtility.IsLiveToddler(pawn))
            {
                if (nd == DBHDefOf.DBHThirst) return false;
                if (nd == DBHDefOf.Bladder)
                {
                    return babyBladder;
                }
                if (nd == DBHDefOf.Hygiene)
                {
                    babyHygiene = result;
                }
            }
            return result;
        }

        public static void CheckBabyBladderNeed()
        {
            if (false)
            {
                babyBladder = true;
            }
            else babyBladder = false;
        }
        
    }
}
