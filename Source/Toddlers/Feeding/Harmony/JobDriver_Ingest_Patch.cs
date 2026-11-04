using HarmonyLib;
using RimWorld;
using System;
using Verse;
using Verse.AI;

namespace Toddlers
{
    // Make toddlers eat more slowly when they feed themselves,
    // to roughly match time of getting fed by somebody else.
    // 1.7: ChewDurationMultiplier became private - nameof() no longer compiles across assemblies.
#if RW_1_7
    [HarmonyPatch(typeof(JobDriver_Ingest), "ChewDurationMultiplier", MethodType.Getter)]
#else
    [HarmonyPatch(typeof(JobDriver_Ingest), nameof(JobDriver_Ingest.ChewDurationMultiplier), MethodType.Getter)]
#endif
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