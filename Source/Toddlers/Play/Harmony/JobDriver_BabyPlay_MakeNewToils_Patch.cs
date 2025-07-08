using HarmonyLib;
using RimWorld;
using System.Reflection;
using Verse;
using static Toddlers.ToddlerUtility;
using static Toddlers.ToddlerPlayUtility;
using Verse.AI;
using System;

namespace Toddlers
{
    /*
    [HarmonyPatch(typeof(JobDriver_BabyPlay),"MakeNewToils")]
    public static class JobDriver_BabyPlay_MakeNewToils_Patch
    {
        public static MethodInfo m_SetFinalizerJob = AccessTools.Method(typeof(JobDriver), "SetFinalizerJob");
        public static FieldInfo f_finishedSetup = AccessTools.Field(typeof(JobDriver_BabyPlay), "finishedSetup");

        public static void Postfix(JobDriver_BabyPlay __instance)
        {
            Func<JobCondition, Job> finalizerJobFactor = delegate (JobCondition condition)
            {
                Job job = null;

                bool finishedSetup = (bool)f_finishedSetup.GetValue(__instance);
                if (finishedSetup && condition != JobCondition.InterruptForced)
                {

                }

                return job;
            };
            m_SetFinalizerJob.Invoke(__instance, new object[] { });
        }
    }
    */
}
