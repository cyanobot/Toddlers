using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using HarmonyLib;
using Verse.AI;

namespace Toddlers
{
    [HarmonyPatch(typeof(JobDriver_Carried), "CarryToil")]
    public static class JobDriver_Carried_Patch
    {
        public static void Postfix(Toil __result)
        {
            __result.AddPreInitAction(delegate ()
            {
                if (ToddlerUtility.IsToddler(__result.actor))
                {
                    __result.actor.jobs.posture = RimWorld.PawnPosture.LayingOnGroundNormal;
                }
            });
        }
    }
}
