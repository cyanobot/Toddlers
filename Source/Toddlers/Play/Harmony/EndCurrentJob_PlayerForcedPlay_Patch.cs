using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;

namespace Toddlers
{
    //finalizer job for baby play is take baby to safety
    //ending any playerforced job sets the finalizer job to also be playerforced
    //playerforced babytosafety displays a message if it can't find a better place than current location
    //which means without this patch that message will almost always play at the end of a playerforced play job

    [HarmonyPatch(typeof(Pawn_JobTracker),nameof(Pawn_JobTracker.EndCurrentJob))]
    public static class EndCurrentJob_PlayerForcedPlay_Patch
    {
        public static void Prefix(Job ___curJob, JobDriver ___curDriver)
        {
            if (___curJob != null && ___curDriver != null
                && ___curDriver is JobDriver_BabyPlay)
            {
                ___curJob.playerForced = false;
            }
        }
    }
}
