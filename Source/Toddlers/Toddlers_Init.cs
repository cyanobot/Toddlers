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
            Toddlers_Mod.facialAnimationLoaded = LoadedModManager.RunningModsListForReading.Any(x => x.Name == "[NL] Facial Animation - WIP");
            Toddlers_Mod.injuredCarryLoaded = LoadedModManager.RunningModsListForReading.Any(x => x.Name == "Injured Carry");
            Toddlers_Mod.HARLoaded = LoadedModManager.RunningModsListForReading.Any(x => x.Name == "Humanoid Alien Races");

            var harmony = new Harmony("cyanobot.toddlers");

            if (Toddlers_Mod.DBHLoaded) Patch_DBH.GeneratePatches(harmony);
            if (Toddlers_Mod.facialAnimationLoaded) Patch_FacialAnimation.Init();
            if (Toddlers_Mod.HARLoaded) Patch_HAR.Init();

            harmony.PatchAll();

            ApplySettings();
            ApparelSettings.ApplyApparelSettings();

            //Toddlers_Mod.televisionDefs = DefDatabase<JoyGiverDef>.GetNamed("WatchTelevision").thingDefs;
            //Toddlers_Mod.televisionMaxParticipants = DefDatabase<JobDef>.GetNamed("WatchTelevision").joyMaxParticipants;
        }

        public static void ApplySettings()
        {
            LifeStageDef babyDef = DefDatabase<LifeStageDef>.GetNamed("HumanlikeBaby");
            LifeStageDef toddlerDef = DefDatabase<LifeStageDef>.GetNamed("HumanlikeToddler");          
          
            StatModifier maxComfyTempMod_baby = babyDef.statOffsets.Find(x => x.stat == StatDefOf.ComfyTemperatureMax);
            maxComfyTempMod_baby.value = Toddlers_Settings.maxComfortableTemperature_Baby - 26f;

            StatModifier maxComfyTempMod_toddler = toddlerDef.statOffsets.Find(x => x.stat == StatDefOf.ComfyTemperatureMax);
            maxComfyTempMod_toddler.value = Toddlers_Settings.maxComfortableTemperature_Toddler - 26f;

            StatModifier minComfyTempMod_baby = babyDef.statOffsets.Find(x => x.stat == StatDefOf.ComfyTemperatureMin);
            minComfyTempMod_baby.value = Toddlers_Settings.minComfortableTemperature_Baby - 16f;

            StatModifier minComfyTempMod_toddler = toddlerDef.statOffsets.Find(x => x.stat == StatDefOf.ComfyTemperatureMin);
            minComfyTempMod_toddler.value = Toddlers_Settings.minComfortableTemperature_Toddler - 16f;

            Toddlers_DefOf.BabyNoExpectations.stages[0].baseMoodEffect = Toddlers_Settings.expectations;
        }

    }
}
