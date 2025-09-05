using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Toddlers
{
    public static class FeedingUtility
    {
        public const float BASE_MESS_RATE = 0.6f;

        public static bool IsToddlerEatingUrgently(Pawn baby)
        {
            JobDef curJob = baby.CurJobDef;
            if ((baby.needs != null && baby.needs.food != null
                    && baby.needs.food.CurLevelPercentage < baby.needs.food.PercentageThreshUrgentlyHungry)
                    && (curJob == JobDefOf.Ingest || (curJob == JobDefOf.TakeFromOtherInventory
                    && baby.CurJob.targetA.HasThing && FoodUtility.WillEat(baby, baby.CurJob.targetA.Thing))))
            {
                return true;
            }
            return false;
        }

        public static void TryMakeMess(Pawn feeder, Pawn baby, float filthFactor = 1)
        {
            float filthRate = BASE_MESS_RATE * filthFactor
            * baby.GetStatValue(StatDefOf.FilthRate, cacheStaleAfterTicks: 60)
            / Math.Max(feeder.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation), 0.1f);

            LogUtil.DebugLog($"filthRate: {filthRate}");

            if (Rand.Value < filthRate * 0.005f)
            {
                if (Toddlers_Settings.feedingMakesMess
                    && FilthMaker.TryMakeFilth(feeder.Position, feeder.Map, Toddlers_DefOf.Toddlers_Filth_BabyFood, outFilth: out Filth filth))
                {
                    FilthMonitor.Notify_FilthHumanGenerated();
                    if (feeder != baby && !feeder.WorkTypeIsDisabled(WorkTypeDefOf.Cleaning))
                    {
                        Job cleanJob = feeder.jobs.jobQueue.FirstOrFallback(qj => qj.job.def == JobDefOf.Clean)?.job;
                        if (cleanJob == null)
                        {
                            cleanJob = JobMaker.MakeJob(JobDefOf.Clean, 120);
                            feeder.jobs.jobQueue.EnqueueFirst(cleanJob, JobTag.MiscWork);
                        }
                        cleanJob.AddQueuedTarget(TargetIndex.A, filth);
                    }
                }

                if (Toddlers_Mod.DBHLoaded)
                {
                    float hygieneFall = filthRate * 0.2f; 
                    if (feeder != baby)
                    {
                        Need need_Hygiene = WashBabyUtility.HygieneNeedFor(feeder);
                        if (need_Hygiene != null && Rand.Bool)
                            need_Hygiene.CurLevel = Mathf.Max(0, need_Hygiene.CurLevel - hygieneFall);
                    }
                    if (Patch_DBH.babyHygiene)
                    {
                        Need need_Hygiene = WashBabyUtility.HygieneNeedFor(baby);
                        if (need_Hygiene != null)
                            need_Hygiene.CurLevel = Mathf.Max(0, need_Hygiene.CurLevel - hygieneFall);
                    }
                }
            }
            
        } 
    }
}
