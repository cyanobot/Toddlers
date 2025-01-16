using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Toddlers
{

    //small tweak so that the adult and baby more often successfully end up in adjacent squares
    //rather than several squares apart
    //or with the adult standing on the baby
    [HarmonyPatch(typeof(JobDriver_PlayStatic), "Play")]
    class PlayStatic_Patch
    {
        static IEnumerable<Toil> Postfix(IEnumerable<Toil> result, Pawn ___pawn, JobDriver __instance)
        {
            //second Goto toil to shift adult off the baby's square if they're already standing on it
            //or make adult close any distance that baby might have covered since adult started trying to reach them

            Toil toil = ToilMaker.MakeToil("DelayedGoTo");
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                Pawn baby = (Pawn)toil.actor.jobs.curJob.GetTarget(TargetIndex.A).Thing;

                if (RCellFinder.TryFindGoodAdjacentSpotToTouch(___pawn, baby, out IntVec3 dest))
                {
                    actor.pather.StartPath(dest, PathEndMode.OnCell);
                }
                else
                {
                    actor.pather.StartPath(baby, PathEndMode.ClosestTouch);
                }
                    
            };
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            toil.AddPreInitAction(delegate
            {
                Pawn baby = (Pawn)toil.actor.jobs.curJob.GetTarget(TargetIndex.A).Thing;
                Job babyPlay = ChildcareUtility.MakeBabyPlayJob(toil.actor);
                //force baby to stay still for adult to finish getting close
                baby.jobs.StartJob(babyPlay, JobCondition.InterruptForced);
            });

            return result.Prepend(toil);
        }
    }
}
