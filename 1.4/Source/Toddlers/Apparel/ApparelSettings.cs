using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;
using System.Reflection;
using static Toddlers.Toddlers_Settings;
using System.IO;

namespace Toddlers
{
    class ApparelSettings
    {
        //public static List<ThingCategoryDef> categoryDefs_Apparel = new List<ThingCategoryDef>() { Toddlers_DefOf.ApparelBaby };


        public static void ApplyApparelSettings()
        {
            List<ThingDef> babyClothes = DefDatabase<ThingDef>.AllDefs.Where(x =>
                x.IsApparel && x.apparel.developmentalStageFilter.Has(DevelopmentalStage.Baby) 
                && !x.apparel.developmentalStageFilter.Has(DevelopmentalStage.Child) 
                && !x.apparel.developmentalStageFilter.Has(DevelopmentalStage.Adult)).ToList();
            babyClothes.RemoveAll(x => x == null);
            List<ThingDef> childClothes = DefDatabase<ThingDef>.AllDefs.Where(x =>
                x.IsApparel && x.apparel.developmentalStageFilter.Has(DevelopmentalStage.Child)).ToList();
            childClothes.RemoveAll(x => x == null);
            List<ThingDef> specialClothes = new List<ThingDef>
                { ThingDefOf.Apparel_GasMask, ThingDefOf.Apparel_ShieldBelt, DefDatabase<ThingDef>.AllDefsListForReading.Find(x => x.defName == "Apparel_ClothMask") };
            specialClothes.RemoveAll(x => x == null);

            List<ThingDef> industrialRecipeUsers = new List<ThingDef>
                { DefDatabase<ThingDef>.GetNamed("ElectricTailoringBench"), DefDatabase<ThingDef>.GetNamed("HandTailoringBench") };
            List<ThingDef> neolithicRecipeUsers = new List<ThingDef>
                { DefDatabase<ThingDef>.GetNamed("ElectricTailoringBench"), DefDatabase<ThingDef>.GetNamed("HandTailoringBench"),
                    DefDatabase<ThingDef>.GetNamed("CraftingSpot")};

            //Log.Message("babyClothes: " + babyClothes.ToStringSafeEnumerable());
            //Log.Message("childClothes: " + childClothes.ToStringSafeEnumerable());
            //Log.Message("industrialRecipeUsers: " + industrialRecipeUsers.ToStringSafeEnumerable());
            //Log.Message("neolithicRecipeUsers: " + neolithicRecipeUsers.ToStringSafeEnumerable());


            switch (apparelSetting)
            {
                case ApparelSetting.NoBabyApparel:
                    //Log.Message("Case: NoBabyApparel");
                    foreach (ThingDef babyClothe in babyClothes)
                    {
                        //Log.Message("babyClothe: " + babyClothe);
                        SetRecipesHidden(babyClothe);
                        SetGameVisibility(babyClothe, false);
                    }

                    foreach (ThingDef clothe in childClothes.Concat(specialClothes))
                    {
                        //Log.Message("childClothe: " + childClothe);
                        clothe.apparel.developmentalStageFilter &= ~DevelopmentalStage.Baby;
                    }
                    break;

                case ApparelSetting.NoTribal:
                    //Log.Message("Case: IndustrialOnly");
                    foreach (ThingDef babyClothe in babyClothes)
                    {
                        //Log.Message("babyClothe: " + babyClothe);
                        if (babyClothe == Toddlers_DefOf.Apparel_BabyTribal)
                        {
                            SetRecipesHidden(babyClothe);
                            SetGameVisibility(babyClothe, false);
                        }
                        else
                        {
                            SetRecipesMedieval(babyClothe);
                            SetGameVisibility(babyClothe, true);
                        }
                    }

                    foreach (ThingDef childClothe in childClothes)
                    {
                        //Log.Message("childClothe: " + childClothe);
                        childClothe.apparel.developmentalStageFilter &= ~DevelopmentalStage.Baby;
                    }
                    foreach (ThingDef specialClothe in specialClothes)
                    {
                        specialClothe.apparel.developmentalStageFilter |= DevelopmentalStage.Baby;
                    }

                    break;

                case ApparelSetting.AllTribal:
                    //Log.Message("Case: TribalOnesies");
                    foreach (ThingDef babyClothe in babyClothes)
                    {
                        //Log.Message("babyClothe: " + babyClothe);
                        SetRecipesNeolithic(babyClothe);
                        SetGameVisibility(babyClothe, true);
                    }

                    foreach (ThingDef childClothe in childClothes)
                    {
                        //Log.Message("childClothe: " + childClothe);
                        childClothe.apparel.developmentalStageFilter &= ~DevelopmentalStage.Baby;
                    }
                    foreach (ThingDef specialClothe in specialClothes)
                    {
                        specialClothe.apparel.developmentalStageFilter |= DevelopmentalStage.Baby;
                    }

                    break;

                case ApparelSetting.BabyTribalwear:
                    //Log.Message("Case: BabyTribalwear");
                    foreach (ThingDef babyClothe in babyClothes)
                    {
                        //Log.Message("babyClothe: " + babyClothe);
                        if (babyClothe == Toddlers_DefOf.Apparel_BabyTribal)
                        {
                            SetRecipesNeolithic(babyClothe);
                        }
                        else
                        {
                            SetRecipesMedieval(babyClothe);
                        }

                        SetGameVisibility(babyClothe, true);
                    }

                    foreach (ThingDef childClothe in childClothes)
                    {
                        //Log.Message("childClothe: " + childClothe);
                        childClothe.apparel.developmentalStageFilter &= ~DevelopmentalStage.Baby;
                    }
                    foreach (ThingDef specialClothe in specialClothes)
                    {
                        specialClothe.apparel.developmentalStageFilter |= DevelopmentalStage.Baby;
                    }
                    break;


                case ApparelSetting.AnyChildApparel:
                    //Log.Message("Case: AnyChildApparel");
                    GenerateMissingGraphics();

                    foreach (ThingDef babyClothe in babyClothes)
                    {
                        //Log.Message("babyClothe: " + babyClothe);
                        SetRecipesHidden(babyClothe);
                        SetGameVisibility(babyClothe, false);
                    }

                    foreach (ThingDef childClothe in childClothes)
                    {
                        //Log.Message("childClothe: " + childClothe);
                        childClothe.apparel.developmentalStageFilter |= DevelopmentalStage.Baby;
                    }

                    break;


                default:
                    break;
            }
        }

        public static void GenerateMissingGraphics()
        {
            List<ThingDef> childClothes = DefDatabase<ThingDef>.AllDefs.Where(x =>
                x.IsApparel && x.apparel.developmentalStageFilter.Has(DevelopmentalStage.Child)).ToList();

            foreach (ThingDef childClothe in childClothes)
            {
                ModContentPack mcp = childClothe.modContentPack;
                //Log.Message("childClothe: " + childClothe + ", mcp: " + mcp);

                if (!ApparelDependentOnBodyType(childClothe)) continue;

                GenerateMissingGraphics_Inner(childClothe.apparel.wornGraphicPath, mcp);

                if (!childClothe.apparel.wornGraphicPaths.NullOrEmpty())
                {
                    foreach (string path in childClothe.apparel.wornGraphicPaths)
                    {
                        GenerateMissingGraphics_Inner(path, mcp);
                    }
                }

                List<StyleCategoryDef> styleCategories = childClothe.RelevantStyleCategories;
                foreach (StyleCategoryDef styleCategory in styleCategories)
                {
                    List<ThingStyleDef> styleDefs = styleCategory.thingDefStyles.Where(x => x.ThingDef == childClothe).Select<ThingDefStyle, ThingStyleDef>(x => x.StyleDef).ToList();
                    foreach (ThingStyleDef styleDef in styleDefs)
                    {

                        GenerateMissingGraphics_Inner(styleDef.wornGraphicPath, styleDef.modContentPack);
                    }
                }
            }
        }

        public static void GenerateMissingGraphics_Inner(string path, ModContentPack mcp)
        {
            if (path.NullOrEmpty()) return;
            //Log.Message("base path: " + path);

            string childPath = path + "_Child";
            string babyPath = path + "_Baby";

            Texture2D babyTex = ContentFinder<Texture2D>.Get(babyPath, false);
            //Log.Message("babyTex: " + babyTex);
            if (babyTex != null) return;

            Texture2D babyTex_north = ContentFinder<Texture2D>.Get(babyPath + "_north", false);
            //Log.Message("babyTex_north: " + babyTex_north);
            if (babyTex_north != null) return;

            DirectoryInfo directoryInfo = new DirectoryInfo(mcp.RootDir);
            //Log.Message("directoryInfo: " + directoryInfo);
            if (!directoryInfo.Exists)
            {
                return;
            }

            string searchPattern = "*" + Path.GetFileName(childPath) + "*";
            //Log.Message(searchPattern);

            FileInfo[] files = directoryInfo.GetFiles(searchPattern, SearchOption.AllDirectories);
            //Log.Message("files: " + files.ToStringSafeEnumerable());

            foreach (FileInfo file in files)
            {
                string name = file.Name;
                string pathStub = file.FullName.Replace(mcp.RootDir, "");
                pathStub = pathStub.Remove(pathStub.Length - name.Length, name.Length);

                string newPath = Toddlers_Mod.mcp.RootDir + pathStub;
                int place = name.LastIndexOf("Child");
                string newName = name.Remove(place, "Child".Length).Insert(place, "Baby");

                //Log.Message("name: " + name + ", pathStub: " + pathStub + ", newPath: " + newPath + ", newName: " + newName);

                DirectoryInfo newDir = new DirectoryInfo(newPath);
                if (!newDir.Exists)
                {
                    //Log.Message("!newDir.Exists");
                    newDir.Create();
                }
                if (newDir.GetFiles(newName).Any())
                {
                    //Log.Message("baby file already exists");
                }
                else
                {
                    //Log.Message("baby file doesn't yet exist");
                    file.CopyTo(newPath + newName);
                }
            }
        }

        public static bool ApparelDependentOnBodyType(ThingDef def)
        {
            //Log.Message(def + ", LastLayer: " + def.apparel.LastLayer + ", wornGraphicData.renderUtilityAsPack: " + (def.apparel.wornGraphicData != null ? def.apparel.wornGraphicData.renderUtilityAsPack : false));
            if (!def.IsApparel) return false;
            if (def.apparel.LastLayer == ApparelLayerDefOf.Overhead) return false;
            if (def.apparel.LastLayer == ApparelLayerDefOf.EyeCover) return false;
            if (def.apparel.LastLayer.IsUtilityLayer
                && def.apparel.wornGraphicData != null
                && def.apparel.wornGraphicData.renderUtilityAsPack) return false;
            if (def.apparel.wornGraphicPath == BaseContent.PlaceholderImagePath) return false;
            if (def.apparel.wornGraphicPath == BaseContent.PlaceholderGearImagePath) return false;

            return true;
        }

        public static void SetGameVisibility(ThingDef def, bool visible)
        {
            if (visible)
            {
                //def.thingCategories = categoryDefs_Apparel;
                def.tradeability = Tradeability.All;
            }
            else
            {
                //def.thingCategories.Clear();
                def.tradeability = Tradeability.Sellable;
            }
        }

        public static void SetRecipesHidden(ThingDef apparel)
        {
            List<ThingDef> recipeUsers = null;
            ResearchProjectDef researchPrerequisite = null;
            TechLevel techLevel = TechLevel.Undefined;

            SetRecipes(apparel, recipeUsers, researchPrerequisite, techLevel);
        }

        public static void SetRecipesMedieval(ThingDef apparel)
        {
            List<ThingDef> recipeUsers = new List<ThingDef>
                { DefDatabase<ThingDef>.GetNamed("ElectricTailoringBench"), DefDatabase<ThingDef>.GetNamed("HandTailoringBench") };
            ResearchProjectDef researchPrerequisite = DefDatabase<ResearchProjectDef>.GetNamed("ComplexClothing");
            TechLevel techLevel = TechLevel.Medieval;

            SetRecipes(apparel, recipeUsers, researchPrerequisite, techLevel);
        }

        public static void SetRecipesNeolithic(ThingDef apparel)
        {
            List<ThingDef> recipeUsers = new List<ThingDef>
                { DefDatabase<ThingDef>.GetNamed("ElectricTailoringBench"), DefDatabase<ThingDef>.GetNamed("HandTailoringBench"),
                    DefDatabase<ThingDef>.GetNamed("CraftingSpot")};
            ResearchProjectDef researchPrerequisite = null;
            TechLevel techLevel = TechLevel.Neolithic;

            SetRecipes(apparel, recipeUsers, researchPrerequisite, techLevel);
        }

        public static void SetRecipes(ThingDef apparel, List<ThingDef> recipeUsers, ResearchProjectDef researchPrerequisite, TechLevel techLevel)
        {
            //Log.Message("SetRecpies firing, apparel: " + apparel + ", recipeUsers: " + recipeUsers.ToStringSafeEnumerable()
            //    + ", researchPrerequisite: " + researchPrerequisite + ", techLevel: " + techLevel);
            //apparel.recipeMaker.researchPrerequisite = researchPrerequisite;
            apparel.techLevel = techLevel;

            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefs.Where((RecipeDef x) => x.ProducedThingDef == apparel))
            {
                List<ThingDef> prevUsers = recipe.recipeUsers;
                //Log.Message("recipe: " + recipe + ", prev users: " + prevUsers.ToStringSafeEnumerable());

                recipe.recipeUsers = recipeUsers;
                //Log.Message("new users: " + recipe.recipeUsers.ToStringSafeEnumerable());
                recipe.researchPrerequisite = researchPrerequisite;
                //Log.Message("new researchPrerequisite: " + researchPrerequisite);

                List<ThingDef> allUsers;
                if (recipeUsers != null && prevUsers != null)
                {
                    allUsers = recipeUsers.Union(prevUsers).ToList();
                }
                else if (recipeUsers != null)
                {
                    allUsers = recipeUsers;
                }
                else if (prevUsers != null)
                {
                    allUsers = prevUsers;
                }
                else
                {
                    return;
                }

                foreach (ThingDef recipeUser in allUsers)
                {
                    //empties the workbench's recipe cache so that it will be regenerated
                    typeof(ThingDef).GetField("allRecipesCached", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(recipeUser, null);
                }
            }
        }
    }
}
