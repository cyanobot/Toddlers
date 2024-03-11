using RimWorld;
using Verse;
using Verse.AI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace Toddlers
{


    static class Patch_HAR
    {
        public const string HARNamespace = "AlienRace";
        public const string HARHarmonyID = "rimworld.erdelf.alien_race.main";

        public const bool VERBOSE_LOGGING_ALIENRACE = false;

        public static ConstructorInfo constructor_LifeStageAgeAlien;

        public static MethodInfo method_AbstractExtendedGraphic_GetPath;
        public static MethodInfo method_HarmonyPatches_DrawAddons;
        public static MethodInfo method_HarmonyPatches_DrawAddonsFinalHook;

        public static FieldInfo field_AbstractExtendedGraphic_bodytypeGraphics;
        public static FieldInfo field_AbstractExtendedGraphic_path;
        public static FieldInfo field_AbstractExtendedGraphic_variantCount;
        public static FieldInfo field_AlienPartGenerator_bodyTypes;
        public static FieldInfo field_AlienSettings_generalSettings;
        public static FieldInfo field_AlienSettings_graphicPaths;
        public static FieldInfo field_ExtendedAgeGraphic_age;
        public static FieldInfo field_ExtendedBodytypeGraphic_bodytype;
        public static FieldInfo field_GeneralSettings_alienPartGenerator;
        public static FieldInfo field_ThingDef_AlienRace_alienRace;

        public static List<FieldInfo> fields_AbstractExtendedGraphic_subgraphics;
        public static List<FieldInfo> fields_GraphicPaths_graphics;
        public static List<FieldInfo> fields_LifeStageAgeAlien;

        public static Dictionary<string,AlienRace> alienRaces;
        public static Dictionary<string, Type> HARClasses;

        public static void Init()
        {
            //do as much of the required reflection as possible just once at initialization
            //because reflection is slow and we don't want to do it repeatedly/during play
            //or especially on every rendering tick!

            HARClasses = (from asm in AppDomain.CurrentDomain.GetAssemblies()
                            from type in asm.GetTypes()
                            where (type.Namespace == "AlienRace" || type.Namespace == "AlienRace.ExtendedGraphics")
                            && type.IsClass && !type.CustomAttributes.Any(x => x.AttributeType == typeof(CompilerGeneratedAttribute))
                            select type)
                            .ToDictionary(t => t.Name);

            constructor_LifeStageAgeAlien = HARClasses["LifeStageAgeAlien"].GetConstructor(System.Type.EmptyTypes);

            method_AbstractExtendedGraphic_GetPath = HARClasses["AbstractExtendedGraphic"].GetMethod("GetPath", new Type[] { });
            method_HarmonyPatches_DrawAddons = HARClasses["HarmonyPatches"].GetMethod("DrawAddons");
            method_HarmonyPatches_DrawAddonsFinalHook = HARClasses["HarmonyPatches"].GetMethod("DrawAddonsFinalHook");

            field_AbstractExtendedGraphic_bodytypeGraphics = HARClasses["AbstractExtendedGraphic"].GetField("bodytypeGraphics", BindingFlags.Instance | BindingFlags.Public);
            field_AbstractExtendedGraphic_path = HARClasses["AbstractExtendedGraphic"].GetField("path", BindingFlags.Instance | BindingFlags.Public);
            field_AbstractExtendedGraphic_variantCount = HARClasses["AbstractExtendedGraphic"].GetField("variantCount", BindingFlags.Instance | BindingFlags.Public);
            field_AlienPartGenerator_bodyTypes = HARClasses["AlienPartGenerator"].GetField("bodyTypes", BindingFlags.Public | BindingFlags.Instance);
            field_AlienSettings_generalSettings = HARClasses["AlienSettings"].GetField("generalSettings", BindingFlags.Public | BindingFlags.Instance);
            field_AlienSettings_graphicPaths = HARClasses["AlienSettings"].GetField("graphicPaths", BindingFlags.Instance | BindingFlags.Public);
            field_ExtendedAgeGraphic_age = HARClasses["ExtendedAgeGraphic"].GetField("age", BindingFlags.Instance | BindingFlags.Public);
            field_ExtendedBodytypeGraphic_bodytype = HARClasses["ExtendedBodytypeGraphic"].GetField("bodytype", BindingFlags.Instance | BindingFlags.Public);
            field_GeneralSettings_alienPartGenerator = HARClasses["GeneralSettings"].GetField("alienPartGenerator", BindingFlags.Public | BindingFlags.Instance);
            field_ThingDef_AlienRace_alienRace = HARClasses["ThingDef_AlienRace"].GetField("alienRace", BindingFlags.Public | BindingFlags.Instance);

            fields_AbstractExtendedGraphic_subgraphics = HARClasses["AbstractExtendedGraphic"].GetFields().Where(x => 
                    x.FieldType.IsGenericType && x.FieldType.GetGenericTypeDefinition() == typeof(List<>)
                    && HARClasses["AbstractExtendedGraphic"].IsAssignableFrom(x.FieldType.GetGenericArguments()[0]))
                    .ToList();
            fields_GraphicPaths_graphics = HARClasses["GraphicPaths"].GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => HARClasses["AbstractExtendedGraphic"].IsAssignableFrom(x.FieldType)).ToList();
            fields_LifeStageAgeAlien = HARClasses["LifeStageAgeAlien"].GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList();

            alienRaces = LoadRaces().ToDictionary(x => x.def.defName);

<<<<<<< Updated upstream
            //Log.Message("Finished Init");
=======
                //Log.Message("Finished Init");
            }
            catch (Exception e)
            {
                Log.Error("[Toddlers] Patch for Humanoid Alien Races failed: " + e.Message + ", StackTrace: " + e.StackTrace);
                HARLoaded = false;
            }
>>>>>>> Stashed changes
        }

        static IEnumerable<AlienRace> LoadRaces()
        {
            Log.Message("]Toddlers] Reading HAR races for compatibility...");

            IEnumerable<ThingDef> raceDefs = (from def in DefDatabase<ThingDef>.AllDefsListForReading
                                              where def.GetType() == HARClasses["ThingDef_AlienRace"]
                                              select def);

            foreach (ThingDef def in raceDefs)
            {
                Log.Message(def.ToString());

<<<<<<< Updated upstream
                AlienRace alienRace = new AlienRace(def);

                yield return alienRace;
=======
                    races.Add(alienRace);
                }
                catch (Exception e)
                {
                    Log.Error("[Toddlers] Init for alien race " + def.defName + " threw an error: " + e.Message + ", StackTrace: " + e.StackTrace);
                }
>>>>>>> Stashed changes
            }
        }

        public static AlienRace GetAlienRaceWrapper(Pawn pawn)
        {
            if (pawn == null) return null;
            if (!HARClasses["ThingDef_AlienRace"].IsAssignableFrom(pawn.def.GetType())) return null;
            if (!alienRaces.ContainsKey(pawn.def.defName))
            {
                Log.Error("[Toddlers] Could not determine alien race for pawn: " + pawn);
                return null;
            }

            return alienRaces[pawn.def.defName];
        }

        public static BodyAddon GetBodyAddonWrapper(Pawn pawn, object addon_orig)
        {
            if (addon_orig == null) return null;
            if (!HARClasses["BodyAddon"].IsAssignableFrom(addon_orig.GetType())) return null;
            AlienRace alienRace = GetAlienRaceWrapper(pawn);
            if (alienRace == null || alienRace.bodyAddons.NullOrEmpty()) return null;
            if (!alienRace.bodyAddons.ContainsKey(addon_orig)) return null;
            return alienRace.bodyAddons[addon_orig];
        }

    }


    
}
