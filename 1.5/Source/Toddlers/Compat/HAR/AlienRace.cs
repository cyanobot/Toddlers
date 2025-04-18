﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static Toddlers.Toddlers_Mod;
using static Toddlers.LogUtil;
using static Toddlers.HARCompat;
using static Toddlers.HARUtil;
using RimWorld;
using System.Collections;
using System.Reflection;

namespace Toddlers
{
    //wrapper for HAR alien races to make handling them easier
    public class AlienRace
    {
        public ThingDef def;
        public LifeStageAge lsa_Baby;
        public LifeStageAge lsa_Toddler;
        public LifeStageAge lsa_Child;
        public bool hasToddler;
        public int babyIndex;
        public float toddlerMinAge = -1f;
        public float toddlerEndAge = -1f;
        public bool humanlikeGait = true;

        public Dictionary<object, object> extendedGraphicsRequiringToddlerCondition = new Dictionary<object, object>();
        private object curParentGraphic = null;
        private Stack<object> parentStack = new Stack<object>();

        public AlienRace(ThingDef def)
        {
            DebugLog("[Toddlers] Processing alien race: " + def.defName);
            this.def = def;

            if (def.race == null)
            {
                Log.Error("[Toddlers] Could not find RaceProperties for alien race: " + def.defName);
                return;
            }

            string s = "Original life stages: ";
            for (int i = 0; i < def.race.lifeStageAges.Count; i++)
            {
                if (i > 0) s += ", ";
                LifeStageAge lsa = def.race.lifeStageAges[i];
                s += lsa.minAge + ":" + lsa.def.defName;
            }
            DebugLog(s);

            InitLifeStageFields();

            if (ShouldGiveToddlerLifeStage())
            {
                toddlerMinAge = CalculateToddlerMinAge();
                CreateToddlerLifeStageAge();
                def.race.lifeStageAges.Insert(babyIndex + 1, lsa_Toddler);
                hasToddler = true;
            }

            if (hasToddler)
            {
                AnalyzeGraphicPaths();
                AnalyzeBodyAddons();
                AddNewExtendedGraphics();
            }

            humanlikeGait = HasHumanlikeGait(def);
            DebugLog("humanlikeGait: " + humanlikeGait);

            s = "Final life stages: ";
            for (int i = 0; i < def.race.lifeStageAges.Count; i++)
            {
                if (i > 0) s += ", ";
                LifeStageAge lsa = def.race.lifeStageAges[i];
                s += lsa.minAge + ":" + lsa.def.defName;
            }
            DebugLog(s);
        }

        public void IterateExtendedGrapics(Traverse trav_curField)
        {
            object curField = trav_curField.GetValue();
            if (curField == null) return;

            Type curFieldType = curField.GetType();
            if (!t_AbstractExtendedGraphic.IsAssignableFrom(curFieldType)) return;

            DebugLog("Iterating - curField: " + curField
                //+ ", curFieldType: " + curFieldType
                + ", curParentGraphic: " + curParentGraphic
                //+ ", t_AbstractExtendedGraphic.IsAssignableFrom : " 
                //+ t_AbstractExtendedGraphic.IsAssignableFrom(curFieldType)
                //+ ", t_ExtendedConditionGraphic.IsAssignableFrom : "
                //+ t_ExtendedConditionGraphic.IsAssignableFrom(curFieldType)
                //+ ", t_ExtendedConditionGraphic == "
                //+ (t_ExtendedConditionGraphic == curFieldType)
                );


            if (t_ExtendedConditionGraphic.IsAssignableFrom(curFieldType))
            {
                IList conditions = trav_curField.Field("conditions").GetValue() as IList;
                //DebugLog("Iterating - found ExtendedConditionGraphic " + curField
                //    + ", with conditions.Count: " + conditions?.Count);

                if (conditions != null && conditions.Count > 0)
                {
                    Dictionary<object, object> toAdd = new Dictionary<object, object>();
                    foreach (object condition in conditions)
                    {
                        Traverse trav_condition = Traverse.Create(condition);
                        Type conditionType = condition.GetType();
                        if (t_ConditionAge.IsAssignableFrom(conditionType))
                        {
                            LifeStageDef age = trav_condition.Field("age").GetValue() as LifeStageDef;
                            DebugLog("Iterating - found ConditionAge with age: " + age.defName);
                            if (age == lsa_Baby.def)
                            {
                                DebugLog("Iterating - found baby graphic - noting for replication");                                

                                extendedGraphicsRequiringToddlerCondition.Add(curParentGraphic, curField);

                                break;
                            }
                        }
                    }
                }
            }

            /*
            DebugLog("trav_curField.GetValue: " + trav_curField.GetValue()
                + ", .Field('extendedGraphics').GetValue: " + trav_curField.Field("extendedGraphics").GetValue()
                );
            */
            IList extendedGraphics = trav_curField.Field("extendedGraphics").GetValue() as IList;
            //DebugLog("Iterating - child extendedGraphics: " + extendedGraphics
            //    + ".Count: " + extendedGraphics?.Count);
            if (extendedGraphics != null && extendedGraphics.Count > 0)
            {
                parentStack.Push(curParentGraphic);
                curParentGraphic = curField;

                foreach (object child in extendedGraphics)
                {
                    //DebugLog("Iterating - child: " + child);
                    IterateExtendedGrapics(Traverse.Create(child));
                }

                curParentGraphic = parentStack.Pop();
            }
        }

        public void AddNewExtendedGraphics()
        {

            if (extendedGraphicsRequiringToddlerCondition.Count > 0)
            {
                object toddlerCondition = Activator.CreateInstance(t_ConditionAge);
                Traverse.Create(toddlerCondition).Field("age").SetValue(lsa_Toddler.def);

                foreach (KeyValuePair<object, object> kvp in extendedGraphicsRequiringToddlerCondition)
                {
                    object parentGraphic = kvp.Key;
                    object babyGraphic = kvp.Value;

                    object toddlerGraphic = Activator.CreateInstance(t_ExtendedConditionGraphic);
                    DebugLog("Iterating over new extended graphics - parentGraphic: " + parentGraphic
                        + ", babyGraphic: " + babyGraphic
                        + ", toddlerGraphic: " + toddlerGraphic);


                    FieldInfo[] fields = babyGraphic.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    //DebugLog("GetFields for babyGraphic: " + fields.Select(f => f.Name).ToStringSafeEnumerable());
                    foreach (FieldInfo field in fields)
                    {
                        //DebugLog("Field: " + field.Name + ", value: " + field.GetValue(babyGraphic));
                        field.SetValue(toddlerGraphic,
                            field.GetValue(babyGraphic));
                    }


                    Traverse toddlerTrav = Traverse.Create(toddlerGraphic);
                    IList babyConditions = toddlerTrav.Field("conditions").GetValue() as IList;
                    if (babyConditions == null)
                    {
                        Log.Error("[Toddlers] Error in copying baby graphics for alien race " + this.def
                            + " : field babyGraphic.conditions is null"
                            + ", parentGraphic: " + parentGraphic
                            + ", babyGraphic: " + babyGraphic);
                        continue;
                    }
                    IList toddlerConditions = Activator.CreateInstance(babyConditions.GetType()) as IList;
                    toddlerConditions.Add(toddlerCondition);
                    toddlerTrav.Field("conditions").SetValue(toddlerConditions);
                    
                    //DebugLog("babyConditions: " + babyConditions.ToStringSafeEnumerable()
                    //    + ", toddlerConditions: " + toddlerConditions.ToStringSafeEnumerable()
                    //    );

                    IList babyExtendedGraphics = Traverse.Create(babyGraphic).Field("extendedGraphics").GetValue() as IList;
                    IList toddlerExtendedGraphics = Traverse.Create(toddlerGraphic).Field("extendedGraphics").GetValue() as IList;
                    DebugLog("babyGraphic.path: " + Traverse.Create(babyGraphic).Field("path").GetValue()
                        + ", toddlerGraphic.path: " + Traverse.Create(toddlerGraphic).Field("path").GetValue()
                        //+ ", babyGraphic.extendedGraphics: " + babyExtendedGraphics
                        //+ ", Count: " + babyExtendedGraphics.Count
                        //+ ", toddlerGraphic.extendedGraphics: " + toddlerExtendedGraphics
                        //+ ", Count: " + toddlerExtendedGraphics.Count
                        );


                    Traverse parentTrav = Traverse.Create(parentGraphic);
                    IList extendedGraphics = parentTrav.Field("extendedGraphics").GetValue() as IList;
                    if (extendedGraphics == null)
                    {
                        Log.Error("[Toddlers] Error in copying baby graphics for alien race " + this.def
                            + " : field parentGraphic.extendedGraphics is null"
                            + ", parentGraphic: " + parentGraphic
                            + ", babyGraphic: " + babyGraphic);
                        continue;
                    }
                    extendedGraphics.Add(toddlerGraphic);
                }
            }
        }

        public void AnalyzeGraphicPaths()
        {
            curParentGraphic = null;
            parentStack.Clear();

            Traverse traverse = Traverse.Create(def);   //ThingDef_AlienRace

            object graphicPaths = traverse.Field("alienRace").Field("graphicPaths").GetValue();
            //DebugLog("graphicPaths: " + graphicPaths + 
            //    ", type: " + graphicPaths.GetType());

            Traverse.IterateFields(graphicPaths, IterateExtendedGrapics);

        }

        public void CreateToddlerLifeStageAge()
        {
            lsa_Toddler = (LifeStageAge)Activator.CreateInstance(t_LifeStageAgeAlien);
            Traverse.IterateFields(lsa_Baby, lsa_Toddler, Traverse.CopyFields);

            if (lsa_Baby.def == LifeStageDefOf.HumanlikeBaby)
            {
                lsa_Toddler.def = Toddlers_DefOf.HumanlikeToddler;
                HARLog("[Toddlers] Processing alien race " + def.defName + ": added life stage HumanlikeToddler");
            }
            else
            {
                lsa_Toddler.def = CreateToddlerLifeStageDef();                
                HARLog("[Toddlers] Processing alien race " + def.defName + ": created new toddler LifeStageDef");
            }

            lsa_Toddler.minAge = toddlerMinAge;
        }

        public LifeStageDef CreateToddlerLifeStageDef()
        {
            LifeStageDef lsd = new LifeStageDef();
            Traverse.IterateFields(lsa_Baby.def, lsd, Traverse.CopyFields);

            lsd.defName = def.defName + "_HumanlikeToddler";
            lsd.label = "toddler";
            lsd.workerClass = typeof(LifeStageWorker_HumanlikeToddler);
            lsd.thinkTreeMainOverride = Toddlers_ThinkTreeDefOf.HumanlikeToddler;
            lsd.thinkTreeConstantOverride = Toddlers_ThinkTreeDefOf.HumanlikeToddlerConstant;
            lsd.alwaysDowned = false;
            StatModifier statFactor_MoveSpeed = lsd.statFactors.Find(x => x.stat == StatDefOf.MoveSpeed);
            if (statFactor_MoveSpeed == null)
            {
                lsd.statFactors.Add(new StatModifier() { stat = StatDefOf.MoveSpeed, value = 0.4f });
            }
            else
            {
                statFactor_MoveSpeed.value = 0.4f;
            }

            DefDatabase<LifeStageDef>.Add(lsd);

            return lsd;
        }

        public float CalculateToddlerMinAge()
        {
            return (float)Math.Max(1f, Math.Round(toddlerEndAge / 3f));
        }

        public bool ShouldGiveToddlerLifeStage()
        {
            if (hasToddler)
            {
                HARLog("[Toddlers] Processing alien race " + def.defName + ": found pre-generated life stage HumanlikeToddler");
                return false;
            }
            if (lsa_Baby == null || lsa_Child == null)
            {
                HARLog("[Toddlers] Processing alien race " + def.defName + ": cannot identify baby and child life stages, skipping");
                return false;
            }
            if (toddlerEndAge < 2f)
            {
                HARLog("[Toddlers] Processing alien race " + def.defName + ": no room for at least a year of toddlerhood between Baby and Child, skipping");
                return false;
            }
            return true;
        }

        public void InitLifeStageFields()
        {
            List<LifeStageAge> lsas = def.race.lifeStageAges;
            //first pass to check if there's already a toddler life stage
            //eg copying from Human
            for (int i = 0; i < lsas.Count; i++)
            {
                LifeStageAge lsa = lsas[i];
                if (lsa.def == Toddlers_DefOf.HumanlikeToddler)
                {
                    hasToddler = true;

                    lsa_Baby = i > 0 ? lsas[i - 1] : null;
                    lsa_Toddler = lsa;
                    lsa_Child = i < lsas.Count - 1 ? lsas[i + 1] : null;

                    toddlerMinAge = lsa.minAge;
                    toddlerEndAge = lsa_Child?.minAge ?? def.race.lifeExpectancy;
                }
            }

            //second pass to decide where to insert toddler stage
            for (int i = 0; i < lsas.Count; i++)
            {
                LifeStageAge lsa = lsas[i];

                //no point checking the final life stage as baby
                //and lets us use lsas[i+1] later without worrying
                if (i == lsas.Count - 1) break;

                //several checks to try and identify the lifestage that best matches "baby"
                //if already mobile, don't need a toddler stage
                if (lsa.def == LifeStageDefOf.HumanlikeBaby
                    || ((lsa.def.defName.Contains("Baby") || lsa.def.developmentalStage == DevelopmentalStage.Baby)
                    && lsa.def.alwaysDowned))
                {
                    //check the next life stage to see if it's a decent match for "child"
                    LifeStageAge nextStage = lsas[i + 1];

                    if (nextStage.def == LifeStageDefOf.HumanlikeChild
                        || (nextStage.def.defName.Contains("Child") || nextStage.def.developmentalStage == DevelopmentalStage.Child)
                        && !nextStage.def.alwaysDowned)
                    {
                        lsa_Baby = lsa;
                        babyIndex = i;
                        lsa_Child = nextStage;
                        toddlerEndAge = lsa_Child.minAge;
                        break;
                    }
                }
            }

        }
    
        public void AnalyzeBodyAddons()
        {
            curParentGraphic = null;
            parentStack.Clear();

            Traverse traverseDef = Traverse.Create(def);   //ThingDef_AlienRace

            IList bodyAddons = traverseDef.Field("alienRace").Field("generalSettings").Field("alienPartGenerator")
                .Field("bodyAddons").GetValue() as IList;
            if (bodyAddons == null || bodyAddons.Count <= 0) return;

            foreach (object bodyAddon in bodyAddons)
            {
                Traverse traverseBodyAddon = Traverse.Create(bodyAddon);
                DebugLog("analysing bodyAddon: " + traverseBodyAddon.Property("Name").GetValue());
                IterateExtendedGrapics(traverseBodyAddon);
            }
        }
    }
}
