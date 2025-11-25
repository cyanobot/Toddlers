using AlienRace;
using AlienRace.ExtendedGraphics;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static Toddlers.HARCompat;
using static Toddlers.HARDebug;

namespace Toddlers
{
    public class AlienRaceToddlerInfo
    {
        public ThingDef_AlienRace alienRace;
        public LifeStageAge lsa_Baby;
        public LifeStageAge lsa_Toddler;
        public LifeStageAge lsa_Child;
        public bool hasToddler;
        public int babyIndex;
        public float toddlerMinAge = -1f;
        public float toddlerEndAge = -1f;
        public bool humanlikeGait = true;

        public ConditionAge conditionAge_Toddler;
        public Dictionary<AbstractExtendedGraphic, AlienPartGenerator.ExtendedConditionGraphic> newGraphicsToAdd
            = new Dictionary<AbstractExtendedGraphic, AlienPartGenerator.ExtendedConditionGraphic>();

        //static field for tree traversals
        static AbstractExtendedGraphic currentParentGraphic;

        //debug reporting
        static int curGraphicLevel = 0;
        static Dictionary<int, string> graphicStack;

        public AlienRaceToddlerInfo(ThingDef_AlienRace alienRace)
        {
            if (alienRace == null)
            {
                throw new NullReferenceException();
            }

            if (HAR_DEBUG_LOGGING)
            {
                curGraphicLevel = 0;
                graphicStack = new Dictionary<int, string>();
            }

            HARDebugLog($"Initializing AlienRaceToddlerInfo for {alienRace.defName}");
            this.alienRace = alienRace;

            /*
            string s = "Original life stages: ";
            for (int i = 0; i < alienRace.race.lifeStageAges.Count; i++)
            {
                if (i > 0) s += ", ";
                LifeStageAge lsa = alienRace.race.lifeStageAges[i];
                s += lsa.minAge + ":" + lsa.def.defName;
            }
            */
            //TODO: debug

            InitLifeStageFields();
            if (ShouldGiveToddlerLifeStage())
            {
                toddlerMinAge = CalculateToddlerMinAge();
                CreateToddlerLifeStageAge();
                alienRace.race.lifeStageAges.Insert(babyIndex + 1, lsa_Toddler);
                conditionAge_Toddler = new ConditionAge();
                conditionAge_Toddler.age = lsa_Toddler.def;
                hasToddler = true;
            }

            if (hasToddler)
            {
                ProcessGraphicPaths();
                ProcessPartGenerator();

                foreach (KeyValuePair<AbstractExtendedGraphic, AlienPartGenerator.ExtendedConditionGraphic> kvp in newGraphicsToAdd)
                {
                    HARDebugLog($"Adding new graphic {kvp.Value} to parent {kvp.Key}");
                    kvp.Key.extendedGraphics.Add(kvp.Value);
                }
            }

            humanlikeGait = HasHumanlikeGait(alienRace);
            //DebugLog("humanlikeGait: " + humanlikeGait);

            /*
            s = "Final life stages: ";
            for (int i = 0; i < alienRace.race.lifeStageAges.Count; i++)
            {
                if (i > 0) s += ", ";
                LifeStageAge lsa = alienRace.race.lifeStageAges[i];
                s += lsa.minAge + ":" + lsa.def.defName;
            }
            //DebugLog(s);
            */
        }

        public void ProcessExtendedGraphic(AbstractExtendedGraphic extendedGraphic)
        {
            if (HAR_DEBUG_LOGGING)
            {
                curGraphicLevel++;
                LogGraphicLevel(extendedGraphic.ToString());

            }
            if (extendedGraphic is AlienPartGenerator.ExtendedConditionGraphic conditionGraphic)
            {
                List<Condition> conditionListForReading = conditionGraphic.conditions.ToList();
                HARDebugLog($"conditionListForReading: {conditionListForReading.ToStringSafeEnumerable()}");
                foreach (Condition condition in conditionListForReading)
                {
                    if (condition is ConditionAge conditionAge
                        && conditionAge.age == lsa_Baby.def)
                    {
                        HARDebugLog($"Found baby condition, adding toddler to newGraphicsToAdd");
                        AlienPartGenerator.ExtendedConditionGraphic newGraphic = NewConditionGraphic(conditionGraphic);
                        newGraphicsToAdd.Add(currentParentGraphic, newGraphic);
                    }
                }
            }

            if (extendedGraphic.extendedGraphics.Count > 0) HARDebugLog($"Processing sub-graphics of {extendedGraphic}");
            foreach (AbstractExtendedGraphic subGraphic in extendedGraphic.extendedGraphics)
            {
                currentParentGraphic = extendedGraphic;
                ProcessExtendedGraphic(subGraphic);
                if (HAR_DEBUG_LOGGING)
                {
                    graphicStack.Remove(curGraphicLevel);
                    curGraphicLevel--;
                }
            }
        }

        public AlienPartGenerator.ExtendedConditionGraphic NewConditionGraphic(AlienPartGenerator.ExtendedConditionGraphic sourceGraphic)
        {
            HARDebugLog($"Creating new toddler conditiongraphic");
            AlienPartGenerator.ExtendedConditionGraphic newGraphic = new AlienPartGenerator.ExtendedConditionGraphic();
            
            HARDebugLog($"path: {sourceGraphic.path}");
            newGraphic.path = sourceGraphic.path;

            HARDebugLog($"paths: {sourceGraphic.paths.ToStringSafeEnumerable()}");
            newGraphic.paths = sourceGraphic.paths;

            HARDebugLog($"paths: {sourceGraphic.pathsFallback.ToStringSafeEnumerable()}");
            newGraphic.pathsFallback = sourceGraphic.pathsFallback;

            HARDebugLog($"usingFallback : {sourceGraphic.usingFallback}");
            newGraphic.usingFallback = sourceGraphic.usingFallback;

            HARDebugLog($"extendedGraphics : {sourceGraphic.extendedGraphics.ToStringSafeEnumerable()}");
            newGraphic.extendedGraphics = sourceGraphic.extendedGraphics;

            HARDebugLog($"conditions : {sourceGraphic.conditions.ToStringSafeEnumerable()}");
            List<Condition> newConditions = new List<Condition>();
            foreach (Condition condition in sourceGraphic.conditions)
            {
                if (condition is ConditionAge conditionAge
                    && conditionAge.age == lsa_Baby.def)
                {
                    newConditions.Add(conditionAge_Toddler);
                }
                else
                {
                    newConditions.Add(condition);
                }                    
            }
            newGraphic.conditions = newConditions;
            return newGraphic;
        }

        public void ProcessPartGenerator()
        {
            AlienPartGenerator partGenerator = alienRace.alienRace.generalSettings.alienPartGenerator;
            HARDebugLog($"Proccessing partGenerator: {partGenerator}");

            foreach (AlienPartGenerator.BodyAddon bodyAddon in partGenerator.bodyAddons)
            {
                currentParentGraphic = null;
                if (HAR_DEBUG_LOGGING) ResetGraphicDebugging($"bodyAddon: {bodyAddon.Name}");
                ProcessExtendedGraphic(bodyAddon);
            }
        }

        public void ProcessGraphicPaths()
        {
            GraphicPaths graphicPaths = alienRace.alienRace.graphicPaths;
            HARDebugLog($"Proccessing graphicPaths: {graphicPaths}");

            currentParentGraphic = null;
            if (HAR_DEBUG_LOGGING) ResetGraphicDebugging("body");
            ProcessExtendedGraphic(graphicPaths.body);

            currentParentGraphic = null;
            if (HAR_DEBUG_LOGGING) ResetGraphicDebugging("bodyMasks");
            ProcessExtendedGraphic(graphicPaths.bodyMasks);

            currentParentGraphic = null;
            if (HAR_DEBUG_LOGGING) ResetGraphicDebugging("head");
            ProcessExtendedGraphic(graphicPaths.head);

            currentParentGraphic = null;
            if (HAR_DEBUG_LOGGING) ResetGraphicDebugging("headMasks");
            ProcessExtendedGraphic(graphicPaths.headMasks);

            currentParentGraphic = null;
            if (HAR_DEBUG_LOGGING) ResetGraphicDebugging("skeleton");
            ProcessExtendedGraphic(graphicPaths.skeleton);

            currentParentGraphic = null;
            if (HAR_DEBUG_LOGGING) ResetGraphicDebugging("skull");
            ProcessExtendedGraphic(graphicPaths.skull);

            currentParentGraphic = null;
            if (HAR_DEBUG_LOGGING) ResetGraphicDebugging("stump");
            ProcessExtendedGraphic(graphicPaths.stump);

            currentParentGraphic = null;
            if (HAR_DEBUG_LOGGING) ResetGraphicDebugging("swaddle");
            ProcessExtendedGraphic(graphicPaths.swaddle);
        }

        public void CreateToddlerLifeStageAge()
        {
            lsa_Toddler = new LifeStageAgeAlien();
            Traverse.IterateFields(lsa_Baby, lsa_Toddler, Traverse.CopyFields);

            if (lsa_Baby.def == LifeStageDefOf.HumanlikeBaby)
            {
                lsa_Toddler.def = Toddlers_DefOf.HumanlikeToddler;
                addedHumanlikeLifestage.Add(alienRace);
            }
            else
            {
                lsa_Toddler.def = CreateToddlerLifeStageDef();
                createdNewLifestage.Add(alienRace);
            }

            lsa_Toddler.minAge = toddlerMinAge;
        }

        public LifeStageDef CreateToddlerLifeStageDef()
        {
            LifeStageDef lsd = new LifeStageDef();
            Traverse.IterateFields(lsa_Baby.def, lsd, Traverse.CopyFields);

            lsd.defName = alienRace.defName + "_HumanlikeToddler";
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
            //minimum 1 year (life stage change is only checked at birthdays)
            //approximately 1/3 of the way through Baby lifestage
            //plus minAge of baby lifestage (to account for races that have another lifestage before baby)
            return (float)Math.Max(1f, Math.Round(toddlerEndAge / 3f)) + lsa_Baby.minAge;
        }

        public bool ShouldGiveToddlerLifeStage()
        {
            if (hasToddler)
            {
                skipped.Add(alienRace, AlienRaceSkipReason.AlreadyHasToddler);
                return false;
            }
            if (lsa_Baby == null || lsa_Child == null)
            {
                skipped.Add(alienRace, AlienRaceSkipReason.NotHumanlikeLifestages);
                return false;
            }
            if ((toddlerEndAge - lsa_Baby.minAge) < 2f)
            {
                skipped.Add(alienRace, AlienRaceSkipReason.GrowsTooFast);
                return false;
            }
            return true;
        }

        public void InitLifeStageFields()
        {
            List<LifeStageAge> lsas = alienRace.race.lifeStageAges;
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
                    toddlerEndAge = lsa_Child?.minAge ?? alienRace.race.lifeExpectancy;

                    return;
                }
            }

            //second pass to decide where to insert toddler stage
            //Count -1 because no point checking the final life stage as baby
            //and lets us use lsas[i+1] later without worrying
            for (int i = 0; i < lsas.Count-1; i++)
            {
                LifeStageAge lsa = lsas[i];

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
                        return;
                    }
                }
            }

        }

        //consider a pawn to most likely have humanlike gait
        //if it has exactly two legs and at least two arms
        public static bool HasHumanlikeGait(ThingDef def)
        {
            if (ToddlersHAR_DefOf.HumanlikeGaitOverride.whitelist.Contains(def)) return true;
            if (ToddlersHAR_DefOf.HumanlikeGaitOverride.blacklist.Contains(def)) return false;

            List<BodyPartRecord> parts = def?.race?.body?.AllParts;
            if (parts.NullOrEmpty()) return false;

            int legCount = parts.Where(x => IsLeg(x)).Count();
            if (legCount != 2) return false;
            int armCount = parts.Where(x => IsArm(x)).Count();
            if (armCount < 2) return false;
            return true;
        }

        public static bool IsLeg(BodyPartRecord partRecord)
        {
            if (partRecord.def == BodyPartDefOf.Leg) return true;
            if (partRecord.def.defName.Contains("leg") || partRecord.def.defName.Contains("Leg") || partRecord.def.defName.Contains("LEG")) return true;
            if (partRecord.Label.Contains("leg") || partRecord.Label.Contains("Leg") || partRecord.Label.Contains("LEG")) return true;
            else return false;
        }
        public static bool IsArm(BodyPartRecord partRecord)
        {
            if (partRecord.def == BodyPartDefOf.Arm) return true;
            if (partRecord.def.defName.Contains("arm") || partRecord.def.defName.Contains("Arm") || partRecord.def.defName.Contains("ARM")) return true;
            if (partRecord.Label.Contains("arm") || partRecord.Label.Contains("Arm") || partRecord.Label.Contains("ARM")) return true;
            else return false;
        }

        public static void ResetGraphicDebugging(string topLevelName)
        {
            HARDebugLog($"Processing top level graphic: {topLevelName}");
            curGraphicLevel = 0;
            graphicStack.Clear();
            graphicStack.Add(0, topLevelName);
        }

        public static void LogGraphicLevel(string curString)
        {
            graphicStack.Add(curGraphicLevel, curString);
            string s = $"level {curGraphicLevel}: {curString} (";
            for (int i = 0; i < curGraphicLevel; i++)
            {
                s += $"{i}: {graphicStack[i]}, ";
            }
            s += ")";
            HARDebugLog(s);
        }

    }
}
