using HarmonyLib;
using RimWorld;
using System;
using Verse;
using Verse.AI;

namespace Toddlers
{
    // Make toddlers eat more slowly when they feed themselves,
    // to roughly match time of getting fed by somebody else.
    [HarmonyPatch(typeof(JobDriver_Ingest), nameof(JobDriver_Ingest.ChewDurationMultiplier), MethodType.Getter)]
    class JobDriver_Ingest_Patch
    {
        static float Postfix(float result, JobDriver_Ingest __instance)
        {
            Pawn pawn = __instance.pawn;
            if (ToddlerUtility.IsToddler(pawn))
                result *= 10f;
            return result;
        }
    }
}