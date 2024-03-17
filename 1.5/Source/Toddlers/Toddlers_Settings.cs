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
        public static bool feedCapableToddlers = true;

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
            Scribe_Values.Look(ref feedCapableToddlers, "feedCapableToddlers", feedCapableToddlers, true);
            Scribe_Values.Look(ref canDraftToddlers, "canDraftToddlers", canDraftToddlers, true);
            Scribe_Values.Look(ref playFallFactor_Baby, "playFallFactor_Baby", playFallFactor_Baby, true);
            Scribe_Values.Look(ref playFallFactor_Toddler, "playFallFactor_Toddler", playFallFactor_Toddler, true);
            Scribe_Values.Look(ref lonelinessGainFactor, "lonelinessGainFactor", lonelinessGainFactor, true);
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

            l.CheckboxLabeled("SettingLabelCanDraftToddlers".Translate() + " :", ref canDraftToddlers,
               tooltip: "[" + "Default".Translate() + ": " + "Off".Translate() + "] " + "SettingTooltipCanDraftToddlers".Translate());
            l.CheckboxLabeled("SettingLabelCareAboutBedtime".Translate() + " :", ref careAboutBedtime,
               tooltip: "[" + "Default".Translate() + ": " + "On".Translate() + "] " + "SettingTooltipCareAboutBedtime".Translate());
            l.CheckboxLabeled("SettingLabelCareAboutFloorSleep".Translate() + " :", ref careAboutFloorSleep,
               tooltip: "[" + "Default".Translate() + ": " + "On".Translate() + "] " + "SettingTooltipCareAboutFloorSleep".Translate());
            l.CheckboxLabeled("SettingLabelFeedCapableToddlers".Translate() + " :", ref feedCapableToddlers,
               tooltip: "[" + "Default".Translate() + ": " + "On".Translate() + "] " + "SettingTooltipFeedCapableToddlers".Translate());
            l.CheckboxLabeled("SettingLabelBabyTalk".Translate() + " :", ref toddlerBabyTalk, 
                tooltip: "[" + "Default".Translate() + ": " + "Off".Translate() + "] " + "SettingTooltipBabyTalk".Translate());

            if (l.ButtonTextLabeled(Toddlers_DefOf.ApparelBaby.LabelCap + " : ", ApparelSettingLabel(apparelSetting)))
            {
                List<FloatMenuOption> floatMenuOptions = new List<FloatMenuOption>();
                foreach (ApparelSetting a in Enum.GetValues(typeof(ApparelSetting)))
                {
                    floatMenuOptions.Add(new FloatMenuOption(ApparelSettingLabel(a), delegate ()
                    {
                        apparelSetting = a;
                        ApparelSettings.ApplyApparelSettings();
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

            l.Label("SettingLabelPlayFallFactor".Translate() + " (" + LifeStageDefOf.HumanlikeBaby.label + ") : " + playFallFactor_Baby.ToString("F1"),
                tooltip: "[" + "Default".Translate() + ": 5.0] " + "SettingTooltipPlayFallFactor".Translate());
            playFallFactor_Baby = l.Slider(playFallFactor_Baby, 0f, 50f);

            l.Label("SettingLabelPlayFallFactor".Translate() + " (" + Toddlers_DefOf.HumanlikeToddler.label + ") : " + playFallFactor_Toddler.ToString("F1"), 
                tooltip: "[" + "Default".Translate() + ": 5.0] " + "SettingTooltipPlayFallFactor".Translate());
            playFallFactor_Toddler = l.Slider(playFallFactor_Toddler, 0f, 50f);

            l.Label("SettingLabelLonelinessFactor".Translate() + " : " + lonelinessGainFactor.ToString("F1"), 
                tooltip: "[" + "Default".Translate() + ": 1.0] " + "SettingTooltipLonelinessFactor".Translate());
            lonelinessGainFactor = l.Slider(lonelinessGainFactor, 0f, 10f);

            l.Label("SettingLabelMoodImpact".Translate(Toddlers_DefOf.BabyNoExpectations.LabelCap) + " : " + expectations.ToString("F0"), 
                tooltip: "[" + "Default".Translate() + ": 20]");
            expectations = l.Slider(expectations, 0f, 100f);

            l.GapLine();
            l.Gap();

            l.Label(StatDefOf.ComfyTemperatureMax.LabelCap + " (" + LifeStageDefOf.HumanlikeBaby.label + ") : " + maxComfortableTemperature_Baby.ToStringTemperature(), 
                tooltip: "[" + "Default".Translate() + ": " + 30f.ToStringTemperature() + "]");
            maxComfortableTemperature_Baby = l.Slider(maxComfortableTemperature_Baby, 26f, 50f);

            l.Label(StatDefOf.ComfyTemperatureMin.LabelCap + " (" + LifeStageDefOf.HumanlikeBaby.label + ") : " + minComfortableTemperature_Baby.ToStringTemperature(),
                tooltip: "[" + "Default".Translate() + ": " + 20f.ToStringTemperature() + "]");
            minComfortableTemperature_Baby = l.Slider(minComfortableTemperature_Baby, -30f, 25f);

            l.Label(StatDefOf.ComfyTemperatureMax.LabelCap + " (" + Toddlers_DefOf.HumanlikeToddler.label + ") : " + maxComfortableTemperature_Toddler.ToStringTemperature(),
                tooltip: "[" + "Default".Translate() + ": " + 28f.ToStringTemperature() + "]");
            maxComfortableTemperature_Toddler = l.Slider(maxComfortableTemperature_Toddler, 26f, 50f);

            l.Label(StatDefOf.ComfyTemperatureMin.LabelCap + " (" + Toddlers_DefOf.HumanlikeToddler.label + ") : " + minComfortableTemperature_Toddler.ToStringTemperature(),
                tooltip: "[" + "Default".Translate() + ": " + 18f.ToStringTemperature() + "]");
            minComfortableTemperature_Toddler = l.Slider(minComfortableTemperature_Toddler, -30f, 25f);

            l.GapLine();
            l.Gap();

            l.Label("SettingLabelTimeToLearn".Translate());
            l.GapLine();

            l.Label("walking".Translate().CapitalizeFirst() + " : " + learningFactor_Walk.ToStringPercent(), 
                tooltip: "[" + "Default".Translate() + ": 80%]");
            learningFactor_Walk = l.Slider(learningFactor_Walk, 0.01f, 1f);

            l.Label(PawnCapacityDefOf.Manipulation.LabelCap + " : " + learningFactor_Manipulation.ToStringPercent(), 
                tooltip: "[" + "Default".Translate() + ": 80%]");
            learningFactor_Manipulation = l.Slider(learningFactor_Manipulation, 0.01f, 1f);

            l.End();

            Widgets.EndScrollView();

           Toddlers_Init.ApplySettings();
        }


        public static string ApparelSettingLabel(ApparelSetting setting)
        {
            return ("ApparelSettingLabel" + setting.ToString()).Translate();

        }

        public static string ApparelSettingTooltip(ApparelSetting setting)
        {
            return ("ApparelSettingTooltip" + setting.ToString()).Translate();
        }
    }
}
