using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static Toddlers.HARUtil;
using static Toddlers.Toddlers_Mod;

namespace Toddlers
{
    public static class HARCompat
    {
        public const string HARNamespace = "AlienRace";
        public const string HARHarmonyID = "rimworld.erdelf.alien_race.main";

        public const bool VERBOSE_LOGGING_ALIENRACE = true;

        public static Dictionary<string, AlienRace> alienRaces;

        public static Type t_ThingDef_AlienRace;
        public static Type t_LifeStageAgeAlien;
        public static Type t_AbstractExtendedGraphic;
        public static Type t_AlienPartGenerator;
        public static Type t_ExtendedConditionGraphic;
        public static Type t_ConditionAge;
        public static Type t_AlienSettings;
        public static Type t_PawnRenderResolveData;
        public static Type t_AlienComp;

        public static FieldInfo f_graphicPaths;

        public static void Init()
        {
            try
            {
                t_ThingDef_AlienRace = AccessTools.TypeByName("AlienRace.ThingDef_AlienRace");
                t_LifeStageAgeAlien = AccessTools.TypeByName("AlienRace.LifeStageAgeAlien");
                t_AbstractExtendedGraphic = AccessTools.TypeByName("AlienRace.ExtendedGraphics.AbstractExtendedGraphic");
                t_AlienPartGenerator = AccessTools.TypeByName("AlienRace.AlienPartGenerator");
                t_ExtendedConditionGraphic = (Type)Traverse.Create(t_AlienPartGenerator).Type("ExtendedConditionGraphic").GetValue();
                t_ConditionAge = AccessTools.TypeByName("AlienRace.ExtendedGraphics.ConditionAge");
                t_AlienSettings = (Type)Traverse.Create(t_ThingDef_AlienRace).Type("AlienSettings").GetValue();
                t_PawnRenderResolveData = (Type)Traverse.Create(AccessTools.TypeByName("AlienRace.AlienRenderTreePatches")).Type("PawnRenderResolveData").GetValue();
                t_AlienComp = (Type)Traverse.Create(t_AlienPartGenerator).Type("AlienComp").GetValue();

                f_graphicPaths = AccessTools.Field(t_AlienSettings, "graphicPaths");

                HARExtendedGraphic.f_path = AccessTools.Field(t_AbstractExtendedGraphic, "path");
                HARExtendedGraphic.f_paths = AccessTools.Field(t_AbstractExtendedGraphic, "paths");
                HARExtendedGraphic.f_extendedGraphics = AccessTools.Field(t_AbstractExtendedGraphic, "extendedGraphics");
                HARExtendedGraphic.f_conditions = AccessTools.Field(t_ExtendedConditionGraphic, "conditions");

                LogUtil.DebugLog("t_ThingDef_AlienRace: " + t_ThingDef_AlienRace);
                LogUtil.DebugLog("t_LifeStageAgeAlien: " + t_LifeStageAgeAlien);
                LogUtil.DebugLog("t_AbstractExtendedGraphic: " + t_AbstractExtendedGraphic);
                LogUtil.DebugLog("t_AlienPartGenerator: " + t_AlienPartGenerator);
                LogUtil.DebugLog("t_ExtendedConditionGraphic: " + t_ExtendedConditionGraphic);
                LogUtil.DebugLog("t_ConditionAge: " + t_ConditionAge);
                LogUtil.DebugLog("t_AlienSettings: " + t_AlienSettings);
                LogUtil.DebugLog("t_PawnRenderResolveData: " + t_PawnRenderResolveData);
                LogUtil.DebugLog("t_AlienComp: " + t_AlienComp);

                LogUtil.DebugLog("f_graphicPaths: " + f_graphicPaths);

                LogUtil.DebugLog($"f_path: {HARExtendedGraphic.f_path}");
                LogUtil.DebugLog($"f_paths: {HARExtendedGraphic.f_paths}");
                LogUtil.DebugLog($"f_extendedGraphics: {HARExtendedGraphic.f_extendedGraphics}");
                LogUtil.DebugLog($"f_conditions: {HARExtendedGraphic.f_conditions}");

                alienRaces = LoadRaces().ToDictionary(x => x.def.defName);
            }
            catch (Exception e)
            {
                Log.Error("[Toddlers] Patch for Humanoid Alien Races failed: " + e.Message + ", StackTrace: " + e.StackTrace);
                HARLoaded = false;
            }
        }

        static List<AlienRace> LoadRaces()
        {
            List<ThingDef> thingDefs = DefDatabase<ThingDef>.AllDefsListForReading.FindAll(
                d => d.GetType() == t_ThingDef_AlienRace);

            List<AlienRace> alienRaces = new List<AlienRace>();
            addedHumanlikeLifestage = new List<AlienRace>();
            createdNewLifestage = new List<AlienRace>();
            skipped = new Dictionary<AlienRace, AlienRaceSkipReason>();

            foreach (ThingDef def in thingDefs)
            {
                try
                {
                    AlienRace alienRace = new AlienRace(def);
                    alienRaces.Add(alienRace);
                }
                catch (Exception e)
                {
                    Log.Error("[Toddlers] Init for alien race " + def.defName + " threw an error: " + e.Message + ", StackTrace: " + e.StackTrace);
                }
            }

            StringBuilder sb_addedHumanlike = new StringBuilder($"[Toddlers] Added lifestage HumanlikeToddler to {addedHumanlikeLifestage.Count} races");
            if (addedHumanlikeLifestage.Count > 0)
            {
                sb_addedHumanlike.Append(": ");
                foreach(AlienRace race in addedHumanlikeLifestage)
                {
                    sb_addedHumanlike.AppendInNewLine($"{race.def.label} ({race.def.defName})");
                }
            }
            Log.Message(sb_addedHumanlike.ToString());

            StringBuilder sb_createdNew = new StringBuilder($"[Toddlers] Created new toddler lifestage for {createdNewLifestage.Count} races");
            if (createdNewLifestage.Count > 0)
            {
                sb_createdNew.Append(": ");
                foreach(AlienRace race in createdNewLifestage)
                {
                    sb_createdNew.AppendInNewLine($"{race.def.label} ({race.def.defName})");
                }
            }
            Log.Message(sb_createdNew.ToString());

            StringBuilder sb_skipped = new StringBuilder($"[Toddlers] Skipped {skipped.Count} races");
            if (sb_skipped.Length > 0)
            {
                sb_skipped.Append(": ");
                foreach(KeyValuePair<AlienRace,AlienRaceSkipReason> kvp in skipped)
                {
                    sb_skipped.AppendInNewLine($"{kvp.Key.def.label} ({kvp.Key.def.defName}) : {SkipReasonString(kvp.Value)}");
                }
            }
            Log.Message(sb_skipped.ToString());

            return alienRaces;
        }
    }
}
