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
    //wrapper for HAR alien races to make handling them easier
    public partial class AlienRace
    {
        public ThingDef def;

        public CrawlingTweak crawlingTweak;
        public bool humanlikeGait;

        public object graphicPaths;
        public object alienSettings;
        public object generalSettings;
        public object alienPartGenerator;

        public AlienRace(ThingDef thingDef)
        {
            Log.Message("Toddlers Mod initialising AlienRace wrapper for: " + thingDef.defName);

            def = thingDef;

            //Log.Message("Original life stages:");
            for (int i = 0; i < def.race.lifeStageAges.Count; i++)
            {
                LifeStageAge lsa = def.race.lifeStageAges[i];
                //Log.Message("Life stage: " + i + ", def: " + lsa.def.defName
               //     + ", minAge: " + lsa.minAge);
            }

            InitLifeStageFields();
            /*
            if (lifeStageBaby == null)
                Log.Message("lifeStageBaby == null");
            else
                Log.Message("Baby = life stage with def: " + lifeStageBaby.def.defName
                    + ", minAge: " + lifeStageBaby.minAge);
            if (lifeStageChild == null)
                Log.Message("lifeStageChild == null");
            else
                Log.Message("Child = life stage with def: " + lifeStageChild.def.defName
                    + ", minAge: " + lifeStageChild.minAge);
            */

            if (!CanCreateToddlerLifeStage()) return;

            humanlikeGait = HasHumanlikeGait();
            //Log.Message("humanlikeGait: " + humanlikeGait);

            alienSettings = field_ThingDef_AlienRace_alienRace.GetValue(def);
            //Log.Message("alienSettings: " + alienSettings);
            generalSettings = field_AlienSettings_generalSettings.GetValue(alienSettings);
            //Log.Message("generalSettings: " + generalSettings);
            alienPartGenerator = field_GeneralSettings_alienPartGenerator.GetValue(generalSettings);
            //Log.Message("alienPartGenerator: " + alienPartGenerator);
            graphicPaths = field_AlienSettings_graphicPaths.GetValue(alienSettings);
            //Log.Message("graphicPaths: " + graphicPaths);

            InitBodyTypes();
            InitBodyAddons();
            InitGraphicFields();

            crawlingTweak = def.GetModExtension<CrawlingTweak>();
            //Log.Message("crawlingTweak: " + crawlingTweak);

            CreateToddlerLifeStage();
            //Log.Message("Finished CreateToddlerLifeStage");
            UpdateAgeGraphics();
            //Log.Message("Finished UpdateAgeGraphics");
            UpdateBodyTypeGraphics();
            //Log.Message("Finished UpdateBodyTypeGraphics");
        }

        public bool HasHumanlikeGait()
        {
            BodyDef body = def.race.body;
            List<BodyPartRecord> parts = body.AllParts;

            int legCount = body.AllParts.Where(x => IsLeg(x)).Count();
            if (legCount != 2) return false;
            int armCount = body.AllParts.Where(x => IsArm(x)).Count();
            if (armCount < 2) return false;
            return true;
        }

        public bool IsLeg(BodyPartRecord partRecord)
        {
            if (partRecord.def == BodyPartDefOf.Leg) return true;
            if (partRecord.def.defName.Contains("leg") || partRecord.def.defName.Contains("Leg") || partRecord.def.defName.Contains("LEG")) return true;
            if (partRecord.Label.Contains("leg") || partRecord.def.defName.Contains("Leg") || partRecord.def.defName.Contains("LEG")) return true;
            else return false;
        }
        public bool IsArm(BodyPartRecord partRecord)
        {
            if (partRecord.def == BodyPartDefOf.Arm) return true;
            if (partRecord.def.defName.Contains("arm") || partRecord.def.defName.Contains("Arm") || partRecord.def.defName.Contains("ARM")) return true;
            if (partRecord.Label.Contains("arm") || partRecord.def.defName.Contains("Arm") || partRecord.def.defName.Contains("ARM")) return true;
            else return false;
        }

        public override string ToString()
        {
            return def.ToString();
        }

        public void InitBodyAddons()
        {
            object bodyAddons_obj = alienPartGenerator.GetType().GetField("bodyAddons", BindingFlags.Public | BindingFlags.Instance).GetValue(alienPartGenerator);
            //Log.Message("bodyAddons_obj: " + bodyAddons_obj + ", Count: " + (bodyAddons_obj as IEnumerable).EnumerableCount());

            foreach (object bodyAddon_origType in bodyAddons_obj as IEnumerable)
            {
                //Log.Message("bodyAddon_origType: " + bodyAddon_origType + ", Type: " + bodyAddon_origType.GetType());
                BodyAddon bodyAddon = new BodyAddon(bodyAddon_origType);
                bodyAddons.Add(bodyAddon_origType, bodyAddon);
               // Log.Message("Adding BodyAddon " + bodyAddon.name + " to list for " + def.defName);
            }
        }
    }

}
