using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Toddlers
{
    //JobGiver_Kidnap fails if trying to target a non-downed awake pawn
    //new version KidnapToddler does not
    [HarmonyPatch(typeof(JobGiver_Kidnap),"TryGiveJob")]
    class JobGiver_Kidnap_Patch
    {
        static Job Postfix(Job oldjob, Pawn pawn)
        {
            if (oldjob != null)
            {
                Pawn victim = (Pawn)oldjob.targetA;

                if (ToddlerUtility.IsToddler(victim))
                {
                    Job newjob = JobMaker.MakeJob(Toddlers_DefOf.KidnapToddler);
                    newjob.targetA = victim;
                    newjob.targetB = oldjob.targetB;
                    newjob.count = 1;

                    return newjob;
                }
            }

            return oldjob;
        }
    }

    
}