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
    class Toddlers_Mod : Mod
    {
        //public static bool extinguishRefuelablesLoaded;
        public static bool dressPatientsLoaded;
        public static bool injuredCarryLoaded;

        public Toddlers_Mod(ModContentPack mcp) : base(mcp)
        {
            GetSettings<Toddlers_Settings>();
        }

        public override string SettingsCategory()
        {
            return "Toddlers";
        }

        public override void DoSettingsWindowContents(Rect inRect) => Toddlers_Settings.DoSettingsWindowContents(inRect);
    }

    [StaticConstructorOnStartup]
    class Toddlers_Init
    {
        static Toddlers_Init()
        {
            Toddlers_Mod.dressPatientsLoaded = LoadedModManager.RunningModsListForReading.Any(x => x.Name == "Dress Patients (1.4)");
            Toddlers_Mod.injuredCarryLoaded = LoadedModManager.RunningModsListForReading.Any(x => x.Name == "Injured Carry");

            var harmony = new Harmony("cyanobot.toddlers");
            harmony.PatchAll();

            ApplySettings();

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
        }
    }
}
