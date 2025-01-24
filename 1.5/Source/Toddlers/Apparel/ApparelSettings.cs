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
    static class ApparelSettings
    {
        public static MethodInfo m_RemoveThingDef = AccessTools.Method(typeof(DefDatabase<ThingDef>), "Remove");
        public static MethodInfo m_RemoveRecipeDef = AccessTools.Method(typeof(DefDatabase<RecipeDef>), "Remove");

        public static List<ThingDef> babyClothes;
        public static Dictionary<ThingDef,List<RecipeDef>> babyClotheRecipes;
        public static Dictionary<ThingDef, Tradeability> babyClotheTradeabilities;

        public static List<ThingDef> childClothes;
        public static List<ThingDef> specialClothes;

        public static List<ThingDef> standardBabyClothes;
        public static List<ThingDef> tribalBabyClothes;

        public static List<ThingDef> medievalRecipeUsers;
        public static List<ThingDef> neolithicRecipeUsers;

        public static List<ResearchProjectDef> medievalResearch;
        public static List<ResearchProjectDef> neolithicResearch;

        public static void InitializeApparelLists()
        {
            babyClothes = DefDatabase<ThingDef>.AllDefs.Where(x =>
                x.IsApparel && x.apparel.developmentalStageFilter.Has(DevelopmentalStage.Baby)
                && !x.apparel.developmentalStageFilter.Has(DevelopmentalStage.Child)
                && !x.apparel.developmentalStageFilter.Has(DevelopmentalStage.Adult)).ToList();
            babyClothes.RemoveAll(x => x == null);

            babyClotheRecipes = new Dictionary<ThingDef, List<RecipeDef>>();
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefs)
            {
                ThingDef producedThing = recipe.ProducedThingDef;
                if (babyClothes.Contains(producedThing))
                {
                    if (!babyClotheRecipes.ContainsKey(producedThing))
                    {
                        babyClotheRecipes.Add(producedThing, new List<RecipeDef>());
                    }
                    babyClotheRecipes[producedThing].Add(recipe);
                }
            }

            babyClotheTradeabilities = new Dictionary<ThingDef, Tradeability>();
            foreach (ThingDef babyClothe in babyClothes)
            {
                babyClotheTradeabilities.Add(babyClothe, babyClothe.tradeability);
            }

            childClothes = DefDatabase<ThingDef>.AllDefs.Where(x =>
                x.IsApparel && x.apparel.developmentalStageFilter.Has(DevelopmentalStage.Child)).ToList();
            childClothes.RemoveAll(x => x == null);
            
            specialClothes = new List<ThingDef>
                { 
                    ThingDefOf.Apparel_GasMask, 
                    ThingDefOf.Apparel_ShieldBelt,
                    DefDatabase<ThingDef>.AllDefsListForReading.Find(x => x.defName == "Apparel_PsychicFoilHelmet"),
                    DefDatabase<ThingDef>.AllDefsListForReading.Find(x => x.defName == "Apparel_ClothMask") 
                };
            specialClothes.RemoveAll(x => x == null);

            tribalBabyClothes = babyClothes.Where(t => t.techLevel == TechLevel.Neolithic).ToList();
            standardBabyClothes = babyClothes.Where(t => t.techLevel != TechLevel.Neolithic).ToList();

            medievalRecipeUsers = Toddlers_DefOf.Apparel_BabyOnesie.recipeMaker.recipeUsers;
            neolithicRecipeUsers = Toddlers_DefOf.Apparel_BabyTribal.recipeMaker.recipeUsers;

            medievalResearch = Toddlers_DefOf.Apparel_BabyOnesie.researchPrerequisites;
            neolithicResearch = Toddlers_DefOf.Apparel_BabyTribal.researchPrerequisites;

            LogUtil.DebugLog("babyClothes: " + babyClothes.ToStringSafeEnumerable());
            LogUtil.DebugLog("childClothes: " + childClothes.ToStringSafeEnumerable());
            LogUtil.DebugLog("specialClothes: " + specialClothes.ToStringSafeEnumerable());

            LogUtil.DebugLog("standardBabyClothes: " + standardBabyClothes.ToStringSafeEnumerable());
            LogUtil.DebugLog("tribalBabyClothes: " + tribalBabyClothes.ToStringSafeEnumerable());

            LogUtil.DebugLog("medievalRecipeUsers: " + medievalRecipeUsers.ToStringSafeEnumerable());
            LogUtil.DebugLog("neolithicRecipeUsers: " + neolithicRecipeUsers.ToStringSafeEnumerable());

            LogUtil.DebugLog("medievalResearch: " + medievalResearch.ToStringSafeEnumerable());
            LogUtil.DebugLog("neolithicResearch: " + neolithicResearch.ToStringSafeEnumerable());

        }

        public static void ApplyApparelSettings()
        {
            List<ThingDef> empty = new List<ThingDef>();

            List<ThingDef> toHide = empty;
            List<ThingDef> wearableByBaby = specialClothes;
            List<ThingDef> neolithic = tribalBabyClothes;

            switch (apparelSetting)
            {
                case ApparelSetting.NoBabyApparel:
                    LogUtil.DebugLog("Case: NoBabyApparel");
                    toHide = babyClothes;
                    wearableByBaby = empty;
                    neolithic = empty;
                    break;

                case ApparelSetting.NoTribal:
                    LogUtil.DebugLog("Case: NoTribal");
                    toHide = tribalBabyClothes;
                    neolithic = empty;
                    break;

                case ApparelSetting.AllTribal:
                    LogUtil.DebugLog("Case: AllTribal");
                    neolithic = babyClothes;
                    break;

                case ApparelSetting.BabyTribalwear:
                    LogUtil.DebugLog("Case: BabyTribalwear");
                    break;

                case ApparelSetting.AnyChildApparel:
                    LogUtil.DebugLog("Case: AnyChildApparel");
                    GenerateMissingGraphics();
                    toHide = babyClothes;
                    wearableByBaby = childClothes.Concat(specialClothes).ToList();
                    break;

                default:
                    break;
            }

            LogUtil.DebugLog("toHide: " + toHide.ToStringSafeEnumerable());
            LogUtil.DebugLog("wearableByBaby: " + wearableByBaby.ToStringSafeEnumerable());
            LogUtil.DebugLog("neolithic: " + neolithic.ToStringSafeEnumerable());

            //block anything to be hidden
            if (!toHide.NullOrEmpty())
            {
                foreach(ThingDef def in toHide)
                {
                    def.destroyOnDrop = true;
                    def.tradeability = 0;
                    def.researchPrerequisites = new List<ResearchProjectDef>();
                    UdpateRecipes(def, empty, new List<ResearchProjectDef>());
                }
            }

            //re-enable anything that is no longer to be hidden
            foreach (ThingDef babyClothe in babyClothes)
            {
                if (!toHide.Contains(babyClothe))
                {
                    babyClothe.destroyOnDrop = false;
                    babyClothe.tradeability = babyClotheTradeabilities[babyClothe];
                }
            }

            //set wearable by baby anything that should be wearable
            if (!wearableByBaby.NullOrEmpty())
            {
                foreach(ThingDef clothe in wearableByBaby)
                {
                    clothe.apparel.developmentalStageFilter |= DevelopmentalStage.Baby;
                }
            }

            //set not-wearable by baby anything that should not be wearable
            foreach (ThingDef clothe in childClothes.Concat(specialClothes))
            {
                if (!wearableByBaby.Contains(clothe))
                {
                    clothe.apparel.developmentalStageFilter &= ~DevelopmentalStage.Baby;
                }
            }

            //set neolithic anything that should be neolithic
            if (!neolithic.NullOrEmpty())
            {
                foreach (ThingDef neolithicClothe in neolithic)
                {
                    if (!toHide.Contains(neolithicClothe))
                    {
                        neolithicClothe.techLevel = TechLevel.Neolithic;
                        neolithicClothe.researchPrerequisites = neolithicResearch;
                        UdpateRecipes(neolithicClothe, neolithicRecipeUsers, neolithicResearch);
                    }
                }
            }

            //set anything not specified neolithic to medieval
            foreach (ThingDef babyClothe in babyClothes)
            {
                if (!neolithic.Contains(babyClothe) && !toHide.Contains(babyClothe))
                {
                    babyClothe.techLevel = TechLevel.Medieval;
                    babyClothe.researchPrerequisites = medievalResearch;
                    UdpateRecipes(babyClothe, medievalRecipeUsers, medievalResearch);
                }
            }

            //clear research unlocked defs cache to force reload
            foreach (ResearchProjectDef researchProjectDef in medievalResearch.Concat(neolithicResearch))
            {
                typeof(ResearchProjectDef).GetField("cachedUnlockedDefs", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(researchProjectDef, null);
            }
            if (MainButtonDefOf.Research.TabWindow.GetType() == typeof(MainTabWindow_Research))
            {
                typeof(MainTabWindow_Research).GetField("cachedUnlockedDefsGroupedByPrerequisites", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(
                        MainButtonDefOf.Research.TabWindow
                        , null);
            }
            
            //make sure references are resolved
            //for eg updating graphics
            foreach (ThingDef clothe in babyClothes.Concat(childClothes).Concat(specialClothes))
            {
                clothe.ResolveReferences();
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

        public static void UdpateRecipes(ThingDef apparel, List<ThingDef> recipeUsers, List<ResearchProjectDef> prerequisites)
        {
            LogUtil.DebugLog("UdpateRecipes - apparel: " + apparel
                + ", recipeUsers: " + recipeUsers.ToStringSafeEnumerable()
                + ", prerequisites: " + prerequisites.ToStringSafeEnumerable());
            List<RecipeDef> recipes;
            if (babyClotheRecipes.TryGetValue(apparel, out recipes))
            {
                LogUtil.DebugLog("Recipes for " + apparel + ": " + recipes.ToStringSafeEnumerable());
                foreach (RecipeDef recipe in recipes)
                {
                    recipe.recipeUsers = recipeUsers;
                    recipe.researchPrerequisite = null;     //use list instead
                    recipe.researchPrerequisites = prerequisites;
                }

                foreach (ThingDef recipeUser in neolithicRecipeUsers.Concat(medievalRecipeUsers))
                {
                    //empties the workbench's recipe cache so that it will be regenerated
                    typeof(ThingDef).GetField("allRecipesCached", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(recipeUser, null);
                }
            }

        }

    }
}
