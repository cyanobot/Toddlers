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

        public LifeStageAge lifeStageBaby = null;
        public int babyIndex;
        public LifeStageAge lifeStageChild = null;
        public float toddlerMinAge = -1f;
        public bool hasToddler = false;
        public LifeStageAge lifeStageToddler = null;


        public float CalcToddlerMinAge()
        {
            //can't be a toddler if we have not found the lifeStages to go either side
            if (lifeStageBaby == null || lifeStageChild == null) return -1f;

            float babyDuration = lifeStageChild.minAge - lifeStageBaby.minAge;
            //can't be a toddler if we're only a baby for less than two years anyway
            if (babyDuration < 2f) return -1f;

            //extrapolating from humans, aim to be a toddler for
            //2/3 of the time we would otherwise be a baby
            float toddlerStart = lifeStageBaby.minAge + (babyDuration / 3f);

            //if we'd be a baby for less than a year we have to round up
            //otherwise we'll never be a baby
            if (toddlerStart <= 1f) return 1f;

            //otherwise round to the closest whole number
            toddlerStart = Mathf.Round(toddlerStart);

            return toddlerStart;
        }

        public void InitLifeStageFields()
        {
            //first pass to check if there is already a toddler life stage
            //due to copying from Human or explicit patching for this mod
            //or due to defining its own
            for (int i = 0; i < def.race.lifeStageAges.Count; i++)
            {
                LifeStageAge lsa = def.race.lifeStageAges[i];

                //checks to identify toddler
                if (lsa.def == Toddlers_DefOf.HumanlikeToddler || lsa.def.defName.Contains("Toddler"))
                {
                    hasToddler = true;
                    lifeStageToddler = lsa;
                    toddlerMinAge = lsa.minAge;
                    lifeStageBaby = def.race.lifeStageAges[i - 1];
                    lifeStageChild = def.race.lifeStageAges[i + 1];
                    //Log.Message("def: " + def.defName + ", hasToddler: " + hasToddler + ", lifeStageToddler: " + lifeStageToddler + ", lifeStageChild: " + lifeStageChild
                    //    + ", minAge: " + toddlerMinAge + ", maxAge: " + lifeStageChild.minAge);
                    return;
                }
            }

            //second pass
            for (int i = 0; i < def.race.lifeStageAges.Count; i++)
            {
                LifeStageAge lsa = def.race.lifeStageAges[i];

                //Log.Message("Testing lsa with defName: " + lsa.def.defName + ", devStage: " + lsa.def.developmentalStage
                //    + ", alwaysDowned: " + lsa.def.alwaysDowned);

                //several checks to try and identify the life stage that best corresponds to baby
                //if already mobile, either this is not the baby lifestage or the race is born precocious
                //either way we don't need a toddler stage
                if (lsa.def == LifeStageDefOf.HumanlikeBaby
                    || ((lsa.def.defName.Contains("Baby") || lsa.def.developmentalStage == DevelopmentalStage.Baby)
                    && lsa.def.alwaysDowned))
                {
                    //check the next life stage to see if it's a decent match for child
                    lifeStageChild = def.race.lifeStageAges[i + 1];
                    //Log.Message("lsa: " + lsa.def.defName + " is a candidate for baby. Testing lsa: " + lifeStageChild.def.defName + 
                    //    ", devStage: " + lifeStageChild.def.developmentalStage + ", alwaysDowned: " + lifeStageChild.def.alwaysDowned);

                    if (lifeStageChild.def == LifeStageDefOf.HumanlikeChild
                        || ((lifeStageChild.def.defName.Contains("Child") || lifeStageChild.def.developmentalStage == DevelopmentalStage.Child)
                        && !lifeStageChild.def.alwaysDowned))
                    {
                        //Log.Message("Success");
                        lifeStageBaby = lsa;
                        babyIndex = i;
                        break;
                    }
                }
            }

            toddlerMinAge = CalcToddlerMinAge();
        }

        public bool CanCreateToddlerLifeStage()
        {
            if (hasToddler)
            {
                Log.Message("[Toddlers] " + def.defName + " already has a toddler life stage, skipping");
                return false;
            }
            if (lifeStageBaby == null)
            {
                Log.Message("[Toddlers] " + def.defName + ": cannot identify Baby life stage, skipping");
                return false;
            }
            if (lifeStageChild == null)
            {
                Log.Message("[Toddlers] " + def.defName + ": cannot identify Child life stage, skipping");
                return false;
            }
            if (toddlerMinAge == -1f)
            {
                Log.Message("[Toddlers] " + def.defName + ": no room for at least a year of toddlerhood between Baby and Child, skipping");
                return false;
            }
            return true;
        }

        public void CreateToddlerLifeStage()
        {
            object lsaa_Baby = Convert.ChangeType(lifeStageBaby, HARClasses["LifeStageAgeAlien"]);

            //instantiate the class
            object lsaa_Toddler = constructor_LifeStageAgeAlien.Invoke(null);

            //copy all the fields from baby
            foreach (FieldInfo field in fields_LifeStageAgeAlien)
            {
                field.SetValue(lsaa_Toddler, field.GetValue(lsaa_Baby));
            }

            //use the minAge calculated based on the duration of other lifestages
            //class_LifeStageAgeAlien.GetField("minAge").SetValue(lsaa_Toddler, toddlerMinAge);
            (lsaa_Toddler as LifeStageAge).minAge = toddlerMinAge;

            //use the lifestage def for toddlers
            //class_LifeStageAgeAlien.GetField("def").SetValue(lsaa_Toddler, Toddlers_DefOf.HumanlikeToddler);

            //create a new lifestage def, based on the def for alien babies
            LifeStageDef ls_Baby = (lsaa_Baby as LifeStageAge).def;
            LifeStageDef ls_Toddler;

            //if alien babies use the human baby def, use the default toddler def too
            if (ls_Baby == LifeStageDefOf.HumanlikeBaby)
            {
                ls_Toddler = Toddlers_DefOf.HumanlikeToddler;
                Log.Message("[Toddlers] " + def.defName + ": inserting HumanlikeToddler life stage for ages " + toddlerMinAge + "-" + lifeStageChild.minAge);
            }
            else
            {
                ls_Toddler = new LifeStageDef();
                foreach (FieldInfo field in typeof(LifeStageDef).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    field.SetValue(ls_Toddler, field.GetValue(ls_Baby));
                }
                ls_Toddler.defName = def.defName + "_HumanlikeToddler";
                ls_Toddler.label = "toddler";
                ls_Toddler.workerClass = typeof(LifeStageWorker_HumanlikeToddler);
                ls_Toddler.thinkTreeMainOverride = Toddlers_ThinkTreeDefOf.HumanlikeToddler;
                ls_Toddler.thinkTreeConstantOverride = Toddlers_ThinkTreeDefOf.HumanlikeToddlerConstant;
                ls_Toddler.alwaysDowned = false;
                StatModifier statFactor_MoveSpeed = ls_Toddler.statFactors.Find(x => x.stat == StatDefOf.MoveSpeed);
                if (statFactor_MoveSpeed == null)
                {
                    ls_Toddler.statFactors.Add(new StatModifier() { stat = StatDefOf.MoveSpeed, value = 0.4f });
                }
                else
                {
                    statFactor_MoveSpeed.value = 0.4f;
                }

                DefDatabase<LifeStageDef>.Add(ls_Toddler);

                Log.Message("[Toddlers] " + def.defName + ": created new toddler life stage for ages " + toddlerMinAge + "-" + lifeStageChild.minAge);
            }

            (lsaa_Toddler as LifeStageAge).def = ls_Toddler;

            def.race.lifeStageAges.Insert(babyIndex + 1, (LifeStageAge)lsaa_Toddler);

            /*
            Log.Message("New life stages for " + def.defName + ":");
            for (int i = 0; i < def.race.lifeStageAges.Count; i++)
            {
                LifeStageAge lsa = def.race.lifeStageAges[i];
                Log.Message("Life stage: " + i + ", def: " + lsa.def.defName
                    + ", minAge: " + lsa.minAge);
            }
            */

            lifeStageToddler = (LifeStageAge)lsaa_Toddler;
            hasToddler = true;

            //Log.Message("Finished CreateToddlerLifeStage");
        }


    }
}
