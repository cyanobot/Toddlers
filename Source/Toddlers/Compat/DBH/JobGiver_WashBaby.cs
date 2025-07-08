using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using static UnityEngine.GraphicsBuffer;
using static Toddlers.Patch_DBH;
using static Toddlers.LogUtil;
using static Toddlers.WashBabyUtility;

namespace Toddlers
{
    class JobGiver_WashBaby : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            DebugLog("JobGiver_WashBaby.TryGiveJob - pawn: " + pawn);

            if (!pawn.CanReserve(pawn))
            {
                return null;
            }
            Pawn baby = FindDirtyBaby(pawn);
            DebugLog("baby: " + baby);
            if (baby == null)
            {
                return null;
            }

            return GetWashJob(pawn, baby);
        }

        public override float GetPriority(Pawn pawn)
        {
            return 9.3f;
        }

    }
}
