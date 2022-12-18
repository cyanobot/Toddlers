using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toddlers
{
    class JobGiver_ToddlerPlay  : ThinkNode_JobGiver
    {
        private static List<ToddlerPlayDef> tmpRandomPlay = new List<ToddlerPlayDef>();

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.needs.play.CurLevelPercentage >= 0.95f)
            {
                return null;
            }

            JobGiver_ToddlerPlay.tmpRandomPlay.Clear();
            List<ToddlerPlayDef> list = DefDatabase<ToddlerPlayDef>.AllDefsListForReading;
            JobGiver_ToddlerPlay.tmpRandomPlay.AddRange(DefDatabase<ToddlerPlayDef>.AllDefsListForReading.InRandomOrder(null));
            foreach (ToddlerPlayDef playDef in tmpRandomPlay)
            {
                if (playDef.Worker.CanDo(pawn))
                {
                    Job job = playDef.Worker.TryGiveJob(pawn);
                    if (job != null)
                    {
                        JobGiver_ToddlerPlay.tmpRandomPlay.Clear();
                        return job;
                    }
                }
            }
            JobGiver_ToddlerPlay.tmpRandomPlay.Clear();
            return null;
        }

        public override float GetPriority(Pawn pawn)
        {
            Need_Play needPlay = pawn.needs.play;
            if (needPlay == null) return 0f;
            if (needPlay.CurLevel < 0.7f * ToddlerPlayUtility.GetMaxPlay(pawn)) return 6f;
            return 2f;
        }
    }
}
