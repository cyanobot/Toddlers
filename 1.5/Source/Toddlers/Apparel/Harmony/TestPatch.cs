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
using static Toddlers.LogUtil;

namespace Toddlers
{
    [HarmonyPatch(typeof(JobDriver), "TryActuallyStartNextToil")]
    public class TestPatch
    {
        public static Toil savedToil = null;
        public static List<Toil> savedToilList = null;

        public static void Prefix(JobDriver __instance, Pawn ___pawn, int ___curToilIndex, 
            List<Toil> ___toils, Job ___job)
        {
            if (!(__instance is JobDriver_Strip driver))
            {
                return;
            }
            DebugLog("JobDriver_Strip TryActuallyStartNextToil Prefix");
            int nextToilIndex = ___curToilIndex + 1;
            bool haveCurToil = (___curToilIndex >= 0 && ___curToilIndex < ___toils.Count && ___job != null)
                && (___pawn.CurJob == ___job);
            Toil curToil = ___curToilIndex >= 0 && ___curToilIndex < ___toils.Count ? ___toils[___curToilIndex] : null;
            Toil nextToil = nextToilIndex >= 0 && nextToilIndex < ___toils.Count ? ___toils[nextToilIndex] : null;
            DebugLog("pawn.stances?.FullBodyBusy: " + ___pawn.stances?.FullBodyBusy
                + ", curToilIndex: " + ___curToilIndex
                + ", toils: " + ___toils.ToStringSafeEnumerable()
                + ", nextToil: " + nextToil
                + ", nextToil.atomicWithPrevious: " + nextToil?.atomicWithPrevious
                );
            DebugLog("HaveCurToil: " + haveCurToil
                + ", curToil: " + curToil
                + ", curToil.finishActions: " + curToil?.finishActions.ToStringSafeEnumerable()
                );
            DebugLog("nextToil: " + nextToil
                + ", nextToil.preInitActions: " + nextToil?.preInitActions.ToStringSafeEnumerable()
                + ", savedToil: " + savedToil
                + ", savedToil.preInitActions: " + savedToil?.preInitActions.ToStringSafeEnumerable()
                + ", nextToil == savedToil: " + (nextToil == savedToil)
                );
        }
        public static void Postfix(JobDriver __instance)
        {
            if (!(__instance is JobDriver_Strip driver))
            {
                return;
            }
            DebugLog("JobDriver_Strip TryActuallyStartNextToil Postfix");
        }
    }
}
