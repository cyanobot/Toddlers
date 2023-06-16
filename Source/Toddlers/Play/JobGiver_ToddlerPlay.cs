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
                //Log.Message("Trying playDef: " + playDef);
                if (playDef.Worker.CanDo(pawn))
                {
                    //Log.Message("CanDo, attempting to give job");
                    Job job = playDef.Worker.TryGiveJob(pawn);
                    if (job != null)
                    {
                        //Log.Message("Returning job");
                        JobGiver_ToddlerPlay.tmpRandomPlay.Clear();
                        return job;
                    }
                    //Log.Message("Failed to give job");
                }
                //Log.Message("!CanDo");
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
