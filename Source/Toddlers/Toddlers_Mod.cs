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
        public static bool facialAnimationLoaded;
        public static bool injuredCarryLoaded;
        public static bool dressPatientsLoaded;
        public static bool DBHLoaded;
        public static bool HARLoaded;

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

            //Toddlers_Mod.televisionDefs = DefDatabase<JoyGiverDef>.GetNamed("WatchTelevision").thingDefs;
            //Toddlers_Mod.televisionMaxParticipants = DefDatabase<JobDef>.GetNamed("WatchTelevision").joyMaxParticipants;
        }

        public static void ApplySettings()
        {
            LifeStageDef babyDef = DefDatabase<LifeStageDef>.GetNamed("HumanlikeBaby");
            LifeStageDef toddlerDef = DefDatabase<LifeStageDef>.GetNamed("HumanlikeToddler");

            List<ThingDef> babyClothes = new List<ThingDef>
                { Toddlers_DefOf.Apparel_BabyOnesie, Toddlers_DefOf.Apparel_BabyTuque, Toddlers_DefOf.Apparel_BabyShadecone };

            foreach (ThingDef babyClothe in babyClothes)
            {
                if (Toddlers_Settings.tribalBabyClothes)
                {
                    babyClothe.recipeMaker.researchPrerequisite = null;
                    babyClothe.techLevel = TechLevel.Neolithic;

                    foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefs.Where((RecipeDef x) => x.ProducedThingDef == babyClothe))
                    {
                        recipe.researchPrerequisite = null;
                    }
                }
                else
                {
                    babyClothe.recipeMaker.researchPrerequisite = DefDatabase<ResearchProjectDef>.GetNamed("ComplexClothing");
                    babyClothe.techLevel = TechLevel.Medieval;

                    foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefs.Where((RecipeDef x) => x.ProducedThingDef == babyClothe))
                    {
                        recipe.researchPrerequisite = DefDatabase<ResearchProjectDef>.GetNamed("ComplexClothing");
                    }
                }
            }
            
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
