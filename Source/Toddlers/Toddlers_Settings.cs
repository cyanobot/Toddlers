using RimWorld;
using Verse;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toddlers
{
    class Toddlers_Settings : ModSettings
    {
        public static bool customRenderer = true;
        public static bool canDraftToddlers = false;

        public static float playFallFactor_Baby = 5f;
        public static float playFallFactor_Toddler = 3f;
        public static float lonelinessGainFactor = 1f;

        public static float maxComfortableTemperature_Baby = 30f;
        public static float maxComfortableTemperature_Toddler = 28f;

        public static float minComfortableTemperature_Baby = 20f;
        public static float minComfortableTemperature_Toddler = 18f;

        public static float learningFactor_Walk = 0.8f;
        public static float learningFactor_Manipulation = 0.8f;


        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref customRenderer, "customRenderer", customRenderer, true);
            Scribe_Values.Look(ref canDraftToddlers, "canDraftToddlers", canDraftToddlers, true);
            Scribe_Values.Look(ref playFallFactor_Baby, "playFallFactor_Baby", playFallFactor_Baby, true);
            Scribe_Values.Look(ref playFallFactor_Toddler, "playFallFactor_Toddler", playFallFactor_Toddler, true);
            Scribe_Values.Look(ref lonelinessGainFactor, "lonelinessGainFactor", lonelinessGainFactor, true);
            Scribe_Values.Look(ref maxComfortableTemperature_Baby, "maxComfortableTemperature_Baby", maxComfortableTemperature_Baby, true);
            Scribe_Values.Look(ref maxComfortableTemperature_Toddler, "maxComfortableTemperature_Toddler", maxComfortableTemperature_Toddler, true);
            Scribe_Values.Look(ref minComfortableTemperature_Baby, "minComfortableTemperature_Baby", minComfortableTemperature_Baby, true);
            Scribe_Values.Look(ref minComfortableTemperature_Toddler, "minComfortableTemperature_Toddler", minComfortableTemperature_Toddler, true);
            Scribe_Values.Look(ref learningFactor_Walk, "learningFactor_Walk", learningFactor_Walk, true);
            Scribe_Values.Look(ref learningFactor_Manipulation, "learningFactor_Manipulation", learningFactor_Manipulation, true);
        }

        public static void DoSettingsWindowContents(Rect rect)
        {
            Listing_Standard l = new Listing_Standard(GameFont.Small)
            {
                ColumnWidth = rect.width
            };

            l.Begin(rect);

            l.CheckboxLabeled("Custom renderer :", ref customRenderer, "Custom (minor) animations for crawling babies, wiggling, wobbling, etc. Turn off if you are having a compatibility issue with other mods that affect rendering.");

            l.CheckboxLabeled("Can draft toddlers :", ref canDraftToddlers,"Toddlers remain incapable of violence, but with this setting on they can be drafted and given orders.");

            l.Label("Play need fall factor (baby) : " + playFallFactor_Baby.ToString("F1"),tooltip:"How fast the Play need falls");
            playFallFactor_Baby = l.Slider(playFallFactor_Baby, 0f, 50f);

            l.Label("Play need fall factor (toddler) : " + playFallFactor_Toddler.ToString("F1"), tooltip: "How fast the Play need falls");
            playFallFactor_Toddler = l.Slider(playFallFactor_Toddler, 0f, 50f);

            l.Label("Loneliness gain rate factor : " + lonelinessGainFactor.ToString("F1"), tooltip: "Controls how often toddlers need adult attention. Turn it up for more attention needed and down for less.");
            lonelinessGainFactor = l.Slider(lonelinessGainFactor, 0f, 10f);

            l.Label("Max Comfortable Temperature (baby) : " + maxComfortableTemperature_Baby.ToStringTemperature());
            maxComfortableTemperature_Baby = l.Slider(maxComfortableTemperature_Baby, 26f, 50f);

            l.Label("Min Comfortable Temperature (baby) : " + minComfortableTemperature_Baby.ToStringTemperature());
            minComfortableTemperature_Baby = l.Slider(minComfortableTemperature_Baby, -30f, 25f);

            l.Label("Max Comfortable Temperature (toddler) : " + maxComfortableTemperature_Toddler.ToStringTemperature());
            maxComfortableTemperature_Toddler = l.Slider(maxComfortableTemperature_Toddler, 26f, 50f);

            l.Label("Min Comfortable Temperature (toddler) : " + minComfortableTemperature_Toddler.ToStringTemperature());
            minComfortableTemperature_Toddler = l.Slider(minComfortableTemperature_Toddler, -30f, 25f);

            l.GapLine();

            l.Label("Time to fully learn (as a % of the toddler lifestage)");
            l.GapLine();

            l.Label("Walking : " + learningFactor_Walk.ToStringPercent());
            learningFactor_Walk = l.Slider(learningFactor_Walk, 0.01f, 1f);

            l.Label("Manipulation : " + learningFactor_Manipulation.ToStringPercent());
            learningFactor_Manipulation = l.Slider(learningFactor_Manipulation, 0.01f, 1f);
            
            l.End();

            Toddlers_Init.ApplySettings();
        }
    }
}
