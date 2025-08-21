using HarmonyLib;
using RimWorld;
using System;
using Verse;
using Verse.AI;

namespace Toddlers
{
    // Make toddlers create mess filth when feeding themselves.
    [HarmonyPatch(typeof(Toils_Ingest), nameof(Toils_Ingest.ChewIngestible))]
    class Toils_Ingest_Patch
    {
        static Toil Postfix(Toil result, Pawn chewer)
        {
            if (ToddlerUtility.IsToddler(chewer) && chewer.Map != null)
                result.AddPreTickAction(() => FeedingUtility.TryMakeMessTick(chewer, chewer));
            return result;
        }
    }
}