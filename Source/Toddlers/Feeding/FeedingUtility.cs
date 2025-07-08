using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

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
    }
}
