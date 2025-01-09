using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Toddlers.Toddlers_Settings;
using System.IO;

namespace Toddlers
{
    [StaticConstructorOnStartup]
    class Toddlers_Init
    {
        static Toddlers_Init()
        {
            Toddlers_Mod.dressPatientsLoaded = LoadedModManager.RunningModsListForReading.Any(x => x.Name == "Dress Patients (1.4)");
            Toddlers_Mod.DBHLoaded = LoadedModManager.RunningModsListForReading.Any(x => x.Name == "Dubs Bad Hygiene" || x.Name == "Dubs Bad Hygiene Lite");
            Toddlers_Mod.injuredCarryLoaded = LoadedModManager.RunningModsListForReading.Any(x => x.Name == "Injured Carry");
            Toddlers_Mod.HARLoaded = LoadedModManager.RunningModsListForReading.Any(x => x.Name == "Humanoid Alien Races");
            Toddlers_Mod.celsiusLoaded = LoadedModManager.RunningModsListForReading.Any(x => x.Name == "Celsius");


            var harmony = new Harmony("cyanobot.toddlers");

            /*
            MethodInfo m_FindUnsafeBaby = AccessTools.Method(typeof(ChildcareUtility), nameof(ChildcareUtility.FindUnsafeBaby), new System.Type[] { typeof(Pawn), typeof(AutofeedMode) });
            LogUtil.DebugLog("m_FindUnsafeBaby: " + m_FindUnsafeBaby);
            if (m_FindUnsafeBaby != null)
            {
                var patches = Harmony.GetPatchInfo(m_FindUnsafeBaby);
                LogUtil.DebugLog("all owners: " + patches?.Owners);
                if (patches != null && patches.Prefixes != null)
                {
                    foreach (var patch in patches.Prefixes)
                    {
                        LogUtil.DebugLog("index: " + patch.index);
                        LogUtil.DebugLog("owner: " + patch.owner);
                        LogUtil.DebugLog("patch method: " + patch.PatchMethod);
                        LogUtil.DebugLog("priority: " + patch.priority);
                        LogUtil.DebugLog("before: " + patch.before);
                        LogUtil.DebugLog("after: " + patch.after);
                    }
                }
                
            }
            */

            if (Toddlers_Mod.DBHLoaded) Patch_DBH.GeneratePatches(harmony);
            //if (Toddlers_Mod.HARLoaded) Patch_HAR.Init();


            harmony.PatchAll();

            ApplySettings();
            ApparelSettings.InitializeApparelLists();
            ApparelSettings.ApplyApparelSettings();

            //Toddlers_Mod.televisionDefs = DefDatabase<JoyGiverDef>.GetNamed("WatchTelevision").thingDefs;
            //Toddlers_Mod.televisionMaxParticipants = DefDatabase<JobDef>.GetNamed("WatchTelevision").joyMaxParticipants;
        }

    }
}
