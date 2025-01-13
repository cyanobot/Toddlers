using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using static Toddlers.ToddlerUtility;

namespace Toddlers
{
    //carried toddlers otherwise maintain Standing posture and are held upright
    [HarmonyPatch(typeof(JobDriver_Carried),"MakeNewToils")]
    public static class JobDriver_Carried_MakeNewToils_Patch
    {
        public static IEnumerable<Toil> Postfix(IEnumerable<Toil> __result, JobDriver_Carried __instance)
        {
            LogUtil.DebugLog("JobDriver_Carried_MakeNewToils_Patch Postfix, pawn: " + __instance.pawn);
            List<Toil> toils = __result.ToList();
            LogUtil.DebugLog("toils list count: " + toils.Count);
            if (toils.NullOrEmpty()) yield break;

            Pawn carriedPawn = __instance.pawn;
            if (IsToddler(carriedPawn) && carriedPawn.CarriedBy is Pawn)
            {
                Toil carriedToil = toils[toils.Count-1];       //assuming that anything that adds extra toils will insert them at the beginning
                carriedToil.AddPreInitAction(delegate
                {
                    carriedToil.actor.jobs.posture = PawnPosture.LayingMask;
                });
            }       
            
            foreach(Toil toil in toils)
            {
                yield return toil;
            }
        }
    }
}