using HarmonyLib;
using RimWorld;
using System.Reflection;
using Verse;
using Verse.AI;

namespace Toddlers
{
    [HarmonyPatch(typeof(JobGiver_GetRest), "TryGiveJob")]
    class GetRest_Patch
    {
        static Job Postfix(Job job, JobGiver_GetRest __instance, Pawn pawn)
        {
            //Log.Message("GetRest_Patch - job: " + job + ", __instance: " + __instance + ", pawn: " + pawn
            //    + ", targetA: " + job?.targetA + ", targetA.Thing: " + job?.targetA.Thing);
            if (ToddlerLearningUtility.IsCrawler(pawn) && job?.targetA.Thing != null && job.targetA.Thing is Building_Bed)
            {
                //Log.Message("Inside if - IsCrawler: " + IsCrawler(pawn) + ", targetA.Cell: " + job.targetA.Cell + ", pawn.Position: " + pawn.Position
                //    + ", GetMethod: " + typeof(JobGiver_GetRest).GetMethod("FindGroundSleepSpotFor", BindingFlags.Instance | BindingFlags.NonPublic));
                if (job.targetA.Cell == pawn.Position) return job;
                MethodInfo m_TryFindGroundSleepSpotFor = typeof(JobGiver_GetRest).GetMethod("TryFindGroundSleepSpotFor", BindingFlags.Instance | BindingFlags.NonPublic);
                object[] parms = new object[] { pawn, null };
                m_TryFindGroundSleepSpotFor.Invoke(__instance, parms);
                job.targetA = (IntVec3)parms[1];
            }
            return job;
        }
    }

    
}