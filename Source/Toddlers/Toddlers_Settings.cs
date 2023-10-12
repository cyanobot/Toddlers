using RimWorld;
using Verse;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Toddlers.Toddlers_Init;

namespace Toddlers
{
    public enum ApparelSetting
    {
        NoBabyApparel,
        NoTribal,
        AllTribal,
        BabyTribalwear,
        AnyChildApparel
    }

    public class Toddlers_Settings : ModSettings
    {
        public static bool careAboutBedtime = true;
        public static bool careAboutFloorSleep = true;

        public static bool customRenderer = true;
        public static bool canDraftToddlers = false;

        public static float playFallFactor_Baby = 5f;
        public static float playFallFactor_Toddler = 3f;
        public static float lonelinessGainFactor = 1f;

        public static bool tribalBabyClothes = false;
        public static ApparelSetting apparelSetting = ApparelSetting.BabyTribalwear;

        public static float maxComfortableTemperature_Baby = 30f;
        public static float maxComfortableTemperature_Toddler = 28f;

        public static float minComfortableTemperature_Baby = 20f;
        public static float minComfortableTemperature_Toddler = 18f;

        public static float learningFactor_Walk = 0.8f;
        public static float learningFactor_Manipulation = 0.8f;

        public static float expectations = 20f;

        public static bool toddlerBabyTalk = false;
        public static int ToddlerTalkInt => toddlerBabyTalk ? 1 : 0;

        private static Vector2 scrollPosition = new Vector2(0f, 0f);

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref careAboutBedtime, "careAboutBedtime", careAboutBedtime, true);
            Scribe_Values.Look(ref careAboutFloorSleep, "careAboutFloorSleep", careAboutFloorSleep, true);
            Scribe_Values.Look(ref customRenderer, "customRenderer", customRenderer, true);
            Scribe_Values.Look(ref canDraftToddlers, "canDraftToddlers", canDraftToddlers, true);
            Scribe_Values.Look(ref playFallFactor_Baby, "playFallFactor_Baby", playFallFactor_Baby, true);
            Scribe_Values.Look(ref playFallFactor_Toddler, "playFallFactor_Toddler", playFallFactor_Toddler, true);
            Scribe_Values.Look(ref lonelinessGainFactor, "lonelinessGainFactor", lonelinessGainFactor, true);
            //Scribe_Values.Look(ref tribalBabyClothes, "tribalBabyClothes", tribalBabyClothes, true);
            Scribe_Values.Look(ref apparelSetting, "apparelSetting", apparelSetting, true);
            Scribe_Values.Look(ref maxComfortableTemperature_Baby, "maxComfortableTemperature_Baby", maxComfortableTemperature_Baby, true);
            Scribe_Values.Look(ref maxComfortableTemperature_Toddler, "maxComfortableTemperature_Toddler", maxComfortableTemperature_Toddler, true);
            Scribe_Values.Look(ref minComfortableTemperature_Baby, "minComfortableTemperature_Baby", minComfortableTemperature_Baby, true);
            Scribe_Values.Look(ref minComfortableTemperature_Toddler, "minComfortableTemperature_Toddler", minComfortableTemperature_Toddler, true);
            Scribe_Values.Look(ref learningFactor_Walk, "learningFactor_Walk", learningFactor_Walk, true);
            Scribe_Values.Look(ref learningFactor_Manipulation, "learningFactor_Manipulation", learningFactor_Manipulation, true);
            Scribe_Values.Look(ref expectations, "expectations", expectations, true);
            Scribe_Values.Look(ref toddlerBabyTalk, "toddlerBabyTalk", toddlerBabyTalk, true);
        }

        public static void DoSettingsWindowContents(Rect inRect)
        {
            float totalContentHeight = 1000f;
            float scrollBarWidth = 18f;

            Rect contentRect = new Rect(inRect);
            //contentRect.width -= scrollBarWidth;
            //contentRect.height = totalContentHeight;

            Rect viewRect = new Rect(inRect);
            //viewRect.width -= scrollBarWidth;
            viewRect.height = totalContentHeight;

            Widgets.DrawHighlight(contentRect);
            Widgets.BeginScrollView(contentRect, ref scrollPosition, viewRect);

            Listing_Standard l = new Listing_Standard(GameFont.Small)
            {
                ColumnWidth = contentRect.width - scrollBarWidth
            };

            l.Begin(viewRect);

            l.CheckboxLabeled("Custom renderer :", ref customRenderer, "[Default: on] Custom (minor) animations for crawling babies, wiggling, wobbling, etc. Turn off if you are having a compatibility issue with other mods that affect rendering.");

            l.CheckboxLabeled("Can draft toddlers :", ref canDraftToddlers, "[Default: off] Toddlers remain incapable of violence, but with this setting on they can be drafted and given orders.");

            l.CheckboxLabeled("Pawns care about baby's Sleep schedue :", ref careAboutBedtime, "[Default: on] Pawns will return babies/toddlers to their cribs when they're scheduled to Sleep");
            l.CheckboxLabeled("Pawns care about babies sleeping on floor :", ref careAboutFloorSleep, "[Default: on] Pawns will return babies/toddlers to their cribs if they notice them sleeping on the floor");

            //l.CheckboxLabeled("Baby clothes at tribal tech level :", ref tribalBabyClothes, "[Default: off] Toggles whether baby clothes require industrial tech");

            l.CheckboxLabeled("Baby talk for toddlers :", ref toddlerBabyTalk, "[Default: off] Whether toddler thoughts should be translated to goo goo ba gee");

            if (l.ButtonTextLabeled("Baby clothes : ", ApparelSettingLabel(apparelSetting), tooltip: "[Default: Tribalwear For Babies]"))
            {
                List<FloatMenuOption> floatMenuOptions = new List<FloatMenuOption>();
                foreach (ApparelSetting a in Enum.GetValues(typeof(ApparelSetting)))
                {
                    floatMenuOptions.Add(new FloatMenuOption(ApparelSettingLabel(a), delegate ()
                    {
                        apparelSetting = a;
                        BabyApparel.ApplyApparelSettings();
                    })
                    {
                        tooltip = ApparelSettingTooltip(a)
                    }
                    );
                }
                Find.WindowStack.Add(new FloatMenu(floatMenuOptions));
            }

            l.GapLine();
            l.Gap();

            l.Label("Play need fall factor (baby) : " + playFallFactor_Baby.ToString("F1"), tooltip: "[Default: 5.0] How fast the Play need falls");
            playFallFactor_Baby = l.Slider(playFallFactor_Baby, 0f, 50f);

            l.Label("Play need fall factor (toddler) : " + playFallFactor_Toddler.ToString("F1"), tooltip: "[Default: 5.0] How fast the Play need falls");
            playFallFactor_Toddler = l.Slider(playFallFactor_Toddler, 0f, 50f);

            l.Label("Loneliness gain rate factor : " + lonelinessGainFactor.ToString("F1"), tooltip: "[Default: 1.0] Controls how often toddlers need adult attention. Turn it up for more attention needed and down for less.");
            lonelinessGainFactor = l.Slider(lonelinessGainFactor, 0f, 10f);

            l.Label("'No expectations' mood impact : " + expectations.ToString("F0"), tooltip: "[Default: 20]");
            expectations = l.Slider(expectations, 0f, 100f);

            l.GapLine();
            l.Gap();

            l.Label("Max Comfortable Temperature (baby) : " + maxComfortableTemperature_Baby.ToStringTemperature(), tooltip: "[Default: 30C / 86F]");
            maxComfortableTemperature_Baby = l.Slider(maxComfortableTemperature_Baby, 26f, 50f);

            l.Label("Min Comfortable Temperature (baby) : " + minComfortableTemperature_Baby.ToStringTemperature(), tooltip: "[Default: 20C / 68F]");
            minComfortableTemperature_Baby = l.Slider(minComfortableTemperature_Baby, -30f, 25f);

            l.Label("Max Comfortable Temperature (toddler) : " + maxComfortableTemperature_Toddler.ToStringTemperature(), tooltip: "[Default: 28C / 82F]");
            maxComfortableTemperature_Toddler = l.Slider(maxComfortableTemperature_Toddler, 26f, 50f);

            l.Label("Min Comfortable Temperature (toddler) : " + minComfortableTemperature_Toddler.ToStringTemperature(), tooltip: "[Default: 18C / 64F]");
            minComfortableTemperature_Toddler = l.Slider(minComfortableTemperature_Toddler, -30f, 25f);

            l.GapLine();
            l.Gap();

            l.Label("Time to fully learn (as a % of the toddler lifestage)");
            l.GapLine();

            l.Label("Walking : " + learningFactor_Walk.ToStringPercent(), tooltip: "[Default: 80%]");
            learningFactor_Walk = l.Slider(learningFactor_Walk, 0.01f, 1f);

            l.Label("Manipulation : " + learningFactor_Manipulation.ToStringPercent(), tooltip: "[Default: 80%]");
            learningFactor_Manipulation = l.Slider(learningFactor_Manipulation, 0.01f, 1f);

            l.End();

            Widgets.EndScrollView();

           Toddlers_Init.ApplySettings();
        }


        public static string ApparelSettingLabel(ApparelSetting setting)
        {
            switch (setting)
            {
                case ApparelSetting.NoBabyApparel:
                    return "No Baby Apparel";
                case ApparelSetting.NoTribal:
                    return "Medieval+ Only";
                case ApparelSetting.AllTribal:
                    return "All Baby Apparel Neolithic";
                case ApparelSetting.BabyTribalwear:
                    return "Baby Tribalwear";
                case ApparelSetting.AnyChildApparel:
                    return "Babies Wear Child Apparel";
                default:
                    return "";
            }
        }

        public static string ApparelSettingTooltip(ApparelSetting setting)
        {
            switch (setting)
            {
                case ApparelSetting.NoBabyApparel:
                    return "Babies and toddlers must go naked";
                case ApparelSetting.NoTribal:
                    return "Tribal babies go naked, medieval+ can make baby clothes";
                case ApparelSetting.AllTribal:
                    return "Baby clothes require no tech";
                case ApparelSetting.BabyTribalwear:
                    return "Baby tribalwear neolithic, other baby clothes medieval+";
                case ApparelSetting.AnyChildApparel:
                    return "Babies can wear any apparel children can";
                default:
                    return "";
            }
        }
    }
}
