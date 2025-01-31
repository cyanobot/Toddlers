using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Jobs;
using Verse;
using Verse.AI;
using static Toddlers.ToddlerUtility;

namespace Toddlers
{
    [HarmonyPatch(typeof(JobDriver_Strip), "MakeNewToils")]
    public static class JobDriver_Strip_MakeNewToils_Patch
    {
        public static IEnumerable<Toil> Postfix(IEnumerable<Toil> __result, Job ___job, List<Toil> ___toils)
        {           
            Pawn targetPawn = ___job.targetA.Pawn;
            List<Toil> result = __result.ToList();
            //LogUtil.DebugLog("JobDriver_Strip_MakeNewToils_Patch Postfix - targetPawn: " + targetPawn
            //    + ", result: " + result.ToStringSafeEnumerable()
            //    );
            foreach(Toil toil in result)
            {
                if (IsLiveToddler(targetPawn) && toil.debugName == "Wait")
                {
                    //LogUtil.DebugLog("found wait toil");
                    //LogUtil.DebugLog("toil.preInitActions: " + toil.preInitActions.ToStringSafeEnumerable());
                    toil.AddPreInitAction(delegate
                    {
                        //LogUtil.DebugLog("firing toil preinit action");
                        Pawn baby = (Pawn)toil.actor.jobs.curJob.GetTarget(TargetIndex.A).Thing;
                        //LogUtil.DebugLog("baby: " + baby);
                        if (!baby.Downed && baby.Awake() && !CribUtility.InCrib(baby))
                        {
                            LogUtil.DebugLog("attempting to assign BeDressed job to baby: " + baby);
                            Job beDressedJob = JobMaker.MakeJob(Toddlers_DefOf.BeDressed, toil.actor);
                            beDressedJob.count = 1;
                            baby.jobs.StartJob(beDressedJob, JobCondition.InterruptForced);
                        }

                    });
                    //LogUtil.DebugLog("toil.preInitActions: " + toil.preInitActions.ToStringSafeEnumerable()); 
                    toil.AddFinishAction(delegate
                    {
                        //LogUtil.DebugLog("firing toil finish action");
                        Pawn baby = (Pawn)toil.actor.jobs.curJob.GetTarget(TargetIndex.A).Thing;
                        if (baby?.CurJobDef == Toddlers_DefOf.BeDressed)
                        {
                            baby.jobs.EndCurrentJob(JobCondition.Succeeded);
                        }
                    });
                }
                yield return toil;
            }
        }
    }
}
