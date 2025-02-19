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

        public static Type t_NeedsUtil;
        public static MethodInfo m_ShouldHaveNeed;

        public static Type t_Need_Hygiene;
        public static FieldInfo f_lastGainTick;
        public static FieldInfo f_contaminated;

        public static Type t_ClosestSanitation;
        public static MethodInfo m_FindBestCleanWaterSource;

        public static Type t_ContaminationLevel;

        public static Type t_SanitationToils;
        public static MethodInfo m_FillBottleFromThing;
        public static MethodInfo m_FillBottleFromCell;

        public static bool InitReferences()
        {
            t_NeedsUtil = AccessTools.TypeByName("DubsBadHygiene.NeedsUtil");
            if (t_NeedsUtil == null) return false;
            m_ShouldHaveNeed = AccessTools.Method(t_NeedsUtil, "ShouldHaveNeed", new Type[] { typeof(Pawn), typeof(NeedDef) });
            if (m_ShouldHaveNeed == null) return false;

            t_Need_Hygiene = AccessTools.TypeByName("DubsBadHygiene.Need_Hygiene");
            f_lastGainTick = AccessTools.Field(t_Need_Hygiene, "lastGainTick");
            f_contaminated = AccessTools.Field(t_Need_Hygiene, "contaminated");

            t_ClosestSanitation = AccessTools.TypeByName("DubsBadHygiene.ClosestSanitation");
            if (t_ClosestSanitation == null) return false;
            m_FindBestCleanWaterSource = AccessTools.Method(t_ClosestSanitation, "FindBestCleanWaterSource",
                new Type[] { typeof(Pawn), typeof(Pawn), typeof(bool), typeof(float), typeof(ThingDef), typeof(Pawn) });
            if (m_FindBestCleanWaterSource == null) return false;

            t_ContaminationLevel = AccessTools.TypeByName("DubsBadHygiene.ContaminationLevel");
            //DebugLog("t_ContaminationLevel: " + t_ContaminationLevel);
            if (t_ContaminationLevel == null) return false;

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
                if (nd.defName == "DBHThirst") return false;
                if (nd.defName == "Bladder") return false;
                if (nd.defName == "Hygiene")
                {
                    babyHygiene = result;
                }
            }
            return result;
        }
        
    }
}
