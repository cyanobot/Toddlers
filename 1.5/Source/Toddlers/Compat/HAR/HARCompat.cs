using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
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
        //public static Type t_ConditionBodyType;

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
                //t_ConditionBodyType = AccessTools.TypeByName("AlienRace.ExtendedGraphics.ConditionBodyType");

                //LogUtil.DebugLog("t_ThingDef_AlienRace: " + t_ThingDef_AlienRace);
                //LogUtil.DebugLog("t_LifeStageAgeAlien: " + t_LifeStageAgeAlien);
                //LogUtil.DebugLog("t_AbstractExtendedGraphic: " + t_AbstractExtendedGraphic);
                //LogUtil.DebugLog("t_AlienPartGenerator: " + t_AlienPartGenerator);
                //LogUtil.DebugLog("t_ExtendedConditionGraphic: " + t_ExtendedConditionGraphic);
                //LogUtil.DebugLog("t_ConditionAge: " + t_ConditionAge);

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
            return alienRaces;
        }
    }
}
