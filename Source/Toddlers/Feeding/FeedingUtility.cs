using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;

namespace Toddlers
{
    public static class FeedingUtility
    {

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

        public static void TryMakeMessTick(Pawn feeder, Pawn baby, float filthFactor = 1)
        {
            float filthRate = baby.GetStatValue(StatDefOf.FilthRate, cacheStaleAfterTicks : 60)
                / Math.Max(feeder.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation), 0.1f);
            filthRate *= filthFactor;
            if (!(Rand.Value < filthRate * 0.005f))
                return;
            if (FilthMaker.TryMakeFilth(feeder.Position, feeder.Map, Toddlers_DefOf.Toddlers_Filth_Mess, baby.LabelIndefinite(), 1))
                FilthMonitor.Notify_FilthHumanGenerated();
            if (Toddlers_Mod.DBHLoaded)
            {
                if (Patch_DBH.babyHygiene)
                {
                    Need need_Hygiene = baby.needs?.AllNeeds.Find(n => n.def == DBHDefOf.Hygiene);
                    if (need_Hygiene != null)
                        need_Hygiene.CurLevel = Mathf.Max( 0, need_Hygiene.CurLevel - 0.1f );
                }
                if (feeder != baby && Rand.Bool) // 50% chance the feeder gets dirty as well.
                {
                    Need need_Hygiene = feeder.needs?.AllNeeds.Find(n => n.def == DBHDefOf.Hygiene);
                    if (need_Hygiene != null)
                        need_Hygiene.CurLevel = Mathf.Max( 0, need_Hygiene.CurLevel - 0.1f );
                }
            }
        }
    }
}
