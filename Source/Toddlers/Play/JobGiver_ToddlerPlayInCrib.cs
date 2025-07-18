﻿using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toddlers
{
    class JobGiver_ToddlerPlayInCrib : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            //LogUtil.DebugLog("Fired JobGiver_ToddlerPlayInCrib.TryGiveJob");
            if (pawn.needs?.TryGetNeed<Need_Play>() == null || pawn.needs.play.CurLevelPercentage >= 0.95f || !pawn.Awake())
            {
                return null;
            }

            Job job;

            ToddlerPlayGiver_WatchTelevision worker1 = Toddlers_DefOf.ToddlerWatchTelevision.Worker as ToddlerPlayGiver_WatchTelevision;
            if (worker1.CanDoFromCrib(pawn))
            {
                job = worker1.TryGiveJobFromCrib(pawn);
                if (job != null) return job;
            }

            ToddlerPlayGiver_Skydreaming worker2 = Toddlers_DefOf.ToddlerSkydreaming.Worker as ToddlerPlayGiver_Skydreaming;
            if (worker2.CanDoFromCrib(pawn))
            {
                job = worker2.TryGiveJobFromCrib(pawn);
                if (job != null) return job;
            }
            return null;
        }

        public override float GetPriority(Pawn pawn)
        {
            //LogUtil.DebugLog("Fired JobGiver_ToddlerPlayInCrib.GetPriority");
            Need_Play needPlay = pawn.needs.play;
            if (needPlay == null) return 0f;
            if (needPlay.CurLevel < 0.8f * ToddlerPlayUtility.GetMaxPlay(pawn)) return 8f;
            return 2f;

        }
    }
}
