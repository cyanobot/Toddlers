using RimWorld;
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
    class JobGiver_IdleInCrib : ThinkNode_JobGiver
    {
        public override float GetPriority(Pawn pawn)
        {
            if (!pawn.Awake()) return 0f;
            return 1f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            //Log.Message("Fired JobGiver_IdleInCrib");
            if (pawn.CurJob != null && !(pawn.CurJob.def == JobDefOf.LayDown)) return pawn.CurJob;

            Thing crib = ToddlerUtility.GetCurrentCrib(pawn);
            if (crib == null) return null;

            List<JobDef> Activities = new List<JobDef>
                {
                    Toddlers_DefOf.LayAngleInCrib,
                    Toddlers_DefOf.RestIdleInCrib,
                    Toddlers_DefOf.WiggleInCrib
                };
            JobDef jobDef = Activities.RandomElement<JobDef>();
            //Log.Message("drew activity : " + jobDef.defName);
            return JobMaker.MakeJob(jobDef, crib);
        }
    }
}
