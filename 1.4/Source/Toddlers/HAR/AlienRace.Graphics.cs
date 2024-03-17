using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Toddlers.Patch_HAR;

namespace Toddlers
{
    public partial class AlienRace
    {
        public const string VANILLA_BODY_PATH = "Things/Pawn/Humanlike/Bodies/";

        public Dictionary<object, BodyAddon> bodyAddons = new Dictionary<object, BodyAddon>();
        public Dictionary<string, string> babyAgeGraphics = new Dictionary<string, string>();
        public Dictionary<string, string> bodyAddonAgeGraphics = new Dictionary<string, string>();

        public List<object> ageGraphics = new List<object>();
        public List<object> bodyGraphicsWithBodyType = new List<object>();
        public List<object> otherGraphicsWithBodyType = new List<object>();

        public void InitGraphicFields()
        {
            //Log.Message("Firing InitGraphicFields, def: " + def);
            
            foreach (FieldInfo graphicField in fields_GraphicPaths_graphics)
            {
                string fieldName = graphicField.Name;
                object topGraphic = graphicField.GetValue(graphicPaths);

                FindNestedSubGraphicsWithAgeOrBodyType(topGraphic, fieldName);
            }

            foreach (KeyValuePair<object, BodyAddon> kvp in bodyAddons)
            {
                object orig = kvp.Key;
                BodyAddon wrapper = kvp.Value;

                FindNestedSubGraphicsWithAgeOrBodyType(orig, wrapper.name);
            }

        }

        public void FindNestedSubGraphicsWithAgeOrBodyType(object topGraphic, string fieldName = null, bool isBody = false)
        {
            //Log.Message("FindNestedSubGraphicsWithAgeOrBodyType, topGraphic: " + topGraphic + ", fieldName: " + fieldName);
            if (!HARClasses["AbstractExtendedGraphic"].IsAssignableFrom(topGraphic.GetType())) return;

            if (fieldName == "body") isBody = true;

            Dictionary<string, object> subgraphics = fields_AbstractExtendedGraphic_subgraphics
                                                            .ToDictionary(keySelector: x => x.Name, elementSelector: x => x.GetValue(topGraphic));
            //Log.Message("subgraphics: " + subgraphics.ToStringSafeEnumerable());

            foreach (KeyValuePair<string, object> kvp in subgraphics)
            {
                string key = kvp.Key;
                object subgraphic = kvp.Value;

                //Log.Message("key: " + key + ", subgraphic: " + subgraphic + ", " + (subgraphic as IEnumerable).ToStringSafeEnumerable());
                
                //only examine lists that aren't empty
                if (!((subgraphic as IEnumerable).EnumerableCount() == 0))
                {
                    //if the type of the list is List<AlienPartGenerator.ExtendedBodytypeGraphic>
                    //we want to "return" it as one of the objects we're looking for
                    Type[] types = subgraphic.GetType().GetGenericArguments();
                    //Log.Message("types[0]: " + types[0]);
                    if (types[0] == HARClasses["ExtendedBodytypeGraphic"])
                    {
                        if (isBody)
                        {
                            bodyGraphicsWithBodyType.Add(topGraphic);
                            //Log.Message("Adding topGraphic: " + topGraphic + ", fieldName: " + fieldName + " to bodyGraphicsWithBodyType because of subgraphic: " + subgraphic + ", key: " + key);
                        }
                        else
                        {
                            otherGraphicsWithBodyType.Add(topGraphic);
                            //Log.Message("Adding topGraphic: " + topGraphic + ", fieldName: " + fieldName + " to otherGraphicsWithBodyType because of subgraphic: " + subgraphic + ", key: " + key);
                        }
                    }
                    if (types[0] == HARClasses["ExtendedAgeGraphic"])
                    {
                        ageGraphics.Add(subgraphic);
                        //Log.Message("From topGraphic: " + topGraphic + ", fieldName: " + fieldName + " adding subgraphic:" + subgraphic + ", key: " + key + " to ageGraphics");
                    }
                    FindNestedSubGraphicsWithAgeOrBodyType(subgraphic, fieldName + "_" + key, isBody);
                }
            }
        }


        public void UpdateAgeGraphics()
        {
            //Log.Message("Started UpdateAgeGraphics");

            foreach (object ageGraphic in ageGraphics)
            {
                UpdateAgeGraphic(ageGraphic);
            }
        }

        public void UpdateAgeGraphic(object ageGraphic)
        {
            //Log.Message("UpdageAgeGraphic:");
            if ((ageGraphic as IEnumerable).EnumerableCount() == 0)
            {
                //Log.Message("ageGraphic was empty");
                return;
            }

            string path;
            string bestPath = null;

            foreach (object item in ageGraphic as IEnumerable)
            {
                LifeStageDef age = (LifeStageDef)field_ExtendedAgeGraphic_age.GetValue(item);
                path = (string)method_AbstractExtendedGraphic_GetPath.Invoke(item, new object[] { });

                //ideally get the path from the baby lifestage
                if (age == lifeStageBaby.def)
                {
                    bestPath = path;
                    break;
                }
                
                //failing that take the path from child
                if (age == lifeStageChild.def && bestPath == null)
                {
                    bestPath = path;
                }
            }
            //if we found neither baby nor child we should probably not add a toddler entry
            if (bestPath == null)
            {
                //Log.Message("ageGraphic contained no entry for baby or child, not adding entry for toddler");
                return;
            }

            object ageGraphic_toddler = Activator.CreateInstance(HARClasses["ExtendedAgeGraphic"]);
            field_ExtendedAgeGraphic_age.SetValue(ageGraphic_toddler, lifeStageToddler.def);
            field_AbstractExtendedGraphic_path.SetValue(ageGraphic_toddler, bestPath);
            field_AbstractExtendedGraphic_variantCount.SetValue(ageGraphic_toddler, 1);

            ageGraphic.GetType().GetMethod("Add").Invoke(ageGraphic, new object[] { ageGraphic_toddler });
        }


        public void UpdateBodyTypeGraphics()
        {
            //Log.Message("Started UpdateBodyTypeGraphics");

            foreach (object graphic in bodyGraphicsWithBodyType)
            {
                UpdateGraphicWithBodyTypeVariants(graphic, true);
            }
            foreach (object graphic in otherGraphicsWithBodyType)
            {
                UpdateGraphicWithBodyTypeVariants(graphic, false);
            }
        }

        public void UpdateGraphicWithBodyTypeVariants(object topGraphic, bool isBody = false)
        {
            string topPath = (string)field_AbstractExtendedGraphic_path.GetValue(topGraphic);
            //Log.Message("UpdateGraphicWithBodyTypeVariants, topGraphic: " + topGraphic + ", topPath: " + topPath);

            IEnumerable subgraphics = field_AbstractExtendedGraphic_bodytypeGraphics.GetValue(topGraphic) as IEnumerable;

            if (subgraphics.EnumerableCount() == 0)
            {
                //Log.Message("bodytypeGraphics is empty");
                return;
            }

            object babyGraphic = null;
            bool workingBabyGraphic = false;
            object childGraphic = null;
            bool workingChildGraphic = false;

            foreach (object subgraphic in subgraphics)
            {
                BodyTypeDef bodyType = (BodyTypeDef)field_ExtendedBodytypeGraphic_bodytype.GetValue(subgraphic);
                string subPath = (string)field_AbstractExtendedGraphic_path.GetValue(subgraphic);
                if (bodyType == bodyType_Baby)
                {
                    babyGraphic = subgraphic;
                    if (CheckTextures(subPath)) workingBabyGraphic = true;
                }
                if (bodyType == bodyType_Child)
                {
                    childGraphic = subgraphic;
                    if (CheckTextures(subPath)) workingChildGraphic = true;
                }
            }

            //Log.Message("workingBabyGraphic: " + workingBabyGraphic + ", babyGraphic: " + babyGraphic + ", workingChildGraphic: " + workingChildGraphic + ", childGraphic: " + childGraphic);

            //if we've already found all the graphics we need
            if (workingBabyGraphic && workingChildGraphic) return;

            //if there is no entry in the list for baby/child
            //make a new one
            if (babyGraphic == null)
            {
                //Log.Message("Generating new babyGraphic");
                babyGraphic = Activator.CreateInstance(HARClasses["ExtendedBodytypeGraphic"]);
                subgraphics.GetType().GetMethod("Add", new Type[] { HARClasses["ExtendedBodytypeGraphic"] })
                    .Invoke(subgraphics, new object[] { babyGraphic });
                field_ExtendedBodytypeGraphic_bodytype.SetValue(babyGraphic, bodyType_Baby);
                field_AbstractExtendedGraphic_variantCount.SetValue(babyGraphic, 1);
                //field_AbstractExtendedGraphic_variantCount.SetValue(subgraphics, (int)field_AbstractExtendedGraphic_variantCount.GetValue(subgraphics) + 1);
            }
            if (childGraphic == null)
            {
                //Log.Message("Generating new childGraphic");
                childGraphic = Activator.CreateInstance(HARClasses["ExtendedBodytypeGraphic"]);
                subgraphics.GetType().GetMethod("Add", new Type[] { HARClasses["ExtendedBodytypeGraphic"] })
                    .Invoke(subgraphics, new object[] { childGraphic });
                field_ExtendedBodytypeGraphic_bodytype.SetValue(childGraphic, bodyType_Child);
                field_AbstractExtendedGraphic_variantCount.SetValue(childGraphic, 1);
                //field_AbstractExtendedGraphic_variantCount.SetValue(subgraphics, (int)field_AbstractExtendedGraphic_variantCount.GetValue(subgraphics) + 1);
            }

            //case where we're dealing with the vanilla body graphics
            // = easiest case
            if (topPath == VANILLA_BODY_PATH)
            {
                //Log.Message("topPath = VANILLA_BODY_PATH");
                if (!workingBabyGraphic)
                {
                    field_AbstractExtendedGraphic_path.SetValue(babyGraphic, VANILLA_BODY_PATH + "Naked_Child");
                }
                if (!workingChildGraphic)
                {
                    field_AbstractExtendedGraphic_path.SetValue(childGraphic, VANILLA_BODY_PATH + "Naked_Child");
                }
                return;
            }


            //if we're not using vanilla textures
            //either because the race has custom body textures
            //or because we're actually looking at a bodyaddon/other with bodyType variants
            //then we reloop through the available defined bodyType variants for this graphic
            //and try to pick the least inappropriate option			
            int priority = 0;
            int bestPriority_Baby = 0;
            object bestGraphic_Baby = null;
            int bestPriority_Child = 0;
            object bestGraphic_Child = null;

            //Log.Message("Looking for best substitute graphics");
            foreach (object subgraphic in subgraphics)
            {
                BodyTypeDef bodyType = (BodyTypeDef)field_ExtendedBodytypeGraphic_bodytype.GetValue(subgraphic);
                string subPath = (string)field_AbstractExtendedGraphic_path.GetValue(subgraphic);
                //Log.Message("Testing subgraphic with bodyType: " + bodyType + ", subPath: " + subPath);

                //if the graphic path doesn't resolve, can't use this graphic for anything
                if (!CheckTextures(subPath))
                {
                    continue;
                }

                if (!workingBabyGraphic)
                {
                    priority = BodyTypePriority_Baby(bodyType);
                    if (priority > bestPriority_Baby)
                    {
                        bestPriority_Baby = priority;
                        bestGraphic_Baby = subgraphic;
                    }
                }
                if (!workingChildGraphic)
                {
                    priority = BodyTypePriority_Child(bodyType);
                    if (priority > bestPriority_Child)
                    {
                        bestPriority_Child = priority;
                        bestGraphic_Child = subgraphic;
                    }
                }
            }
            //Log.Message("bestGraphic_Baby: " + bestGraphic_Baby + ", bestGraphic_Child: " + bestGraphic_Child);

            if (!workingBabyGraphic)
            {
                //if we've identified a candidate
                if (bestGraphic_Baby != null)
                {
                    //Log.Message("Copying babyGraphic from bestGraphic");
                    //copy all fields
                    foreach (FieldInfo fieldInfo in HARClasses["AbstractExtendedGraphic"].GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        fieldInfo.SetValue(babyGraphic, fieldInfo.GetValue(bestGraphic_Baby));
                    }

                    //then set bodyType (back) to what it should be
                    field_ExtendedBodytypeGraphic_bodytype.SetValue(babyGraphic, bodyType_Baby);
                }
                //if we have no candidate
                //if we're looking at the main body graphic, default back to vanilla
                else if (isBody)
                {
                    //Log.Message("defaulting babyGraphic to vanilla path");
                    field_AbstractExtendedGraphic_path.SetValue(babyGraphic, VANILLA_BODY_PATH + "Naked_Child");
                }
                //otherwise give up
            }

            if (!workingChildGraphic)
            {
                //if we've identified a candidate
                if (bestGraphic_Child != null)
                {
                    //Log.Message("Copying childGraphic from bestGraphic");
                    //copy all fields
                    foreach (FieldInfo fieldInfo in HARClasses["AbstractExtendedGraphic"].GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        fieldInfo.SetValue(childGraphic, fieldInfo.GetValue(bestGraphic_Child));
                    }

                    //then set bodyType (back) to what it should be
                    field_ExtendedBodytypeGraphic_bodytype.SetValue(childGraphic, bodyType_Child);
                }
                //if we have no candidate
                //if we're looking at the main body graphic, default back to vanilla
                else if (isBody)
                {
                    //Log.Message("defaulting childGraphic to vanilla path");
                    field_AbstractExtendedGraphic_path.SetValue(childGraphic, VANILLA_BODY_PATH + "Naked_Child");
                }
                //otherwise give up
            }
        }

        public bool CheckTextures(string path)
        {
            if (ContentFinder<Texture2D>.Get(path + "_north", reportFailure: false) == null) return false;
            if (ContentFinder<Texture2D>.Get(path + "_east", reportFailure: false) == null) return false;
            if (ContentFinder<Texture2D>.Get(path + "_south", reportFailure: false) == null) return false;

            return true;
        }

        public int BodyTypePriority_Baby(BodyTypeDef def)
        {
            if (def == BodyTypeDefOf.Baby || def == bodyType_Baby || def.defName.Contains("Baby") || def.defName.Contains("baby")) return 100;
            if (def == BodyTypeDefOf.Child ||def == bodyType_Child || def.defName.Contains("Child") || def.defName.Contains("child")) return 10;
            if (def == BodyTypeDefOf.Thin || def.defName.Contains("Thin") || def.defName.Contains("thin")) return 9;
            if (def.defName.Contains("Main") || def.defName.Contains("main")
                || def.defName.Contains("Norm") || def.defName.Contains("norm")
                || def.defName.Contains("Stand") || def.defName.Contains("stand")
                || def.defName.Contains("Std") || def.defName.Contains("std")
                || def.defName.Contains("Default") || def.defName.Contains("default")
                || def.defName.Contains("Base") || def.defName.Contains("base")
                || def.defName.Contains("Basic") || def.defName.Contains("basic")
                ) return 8;
            if (def == BodyTypeDefOf.Male || def.defName.Contains("Male") || def.defName.Contains("male")) return 7;
            return 1;
        }
        public int BodyTypePriority_Child(BodyTypeDef def)
        {
            if (def == BodyTypeDefOf.Child || def == bodyType_Child || def.defName.Contains("Child") || def.defName.Contains("child")) return 100;
            if (def == BodyTypeDefOf.Baby || def == bodyType_Baby || def.defName.Contains("Baby") || def.defName.Contains("baby")) return 10;
            if (def == BodyTypeDefOf.Thin || def.defName.Contains("Thin") || def.defName.Contains("thin")) return 9;
            if (def.defName.Contains("Main") || def.defName.Contains("main")
                || def.defName.Contains("Norm") || def.defName.Contains("norm")
                || def.defName.Contains("Stand") || def.defName.Contains("stand")
                || def.defName.Contains("Std") || def.defName.Contains("std")
                || def.defName.Contains("Default") || def.defName.Contains("default")
                || def.defName.Contains("Base") || def.defName.Contains("base")
                || def.defName.Contains("Basic") || def.defName.Contains("basic")
                ) return 8;
            if (def == BodyTypeDefOf.Male || def.defName.Contains("Male") || def.defName.Contains("male")) return 7;
            return 1;
        }
    }
}
