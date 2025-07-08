using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Toddlers
{
    //overwrite play job given to baby while being played with
    //with our own
    [HarmonyPatch(typeof(ChildcareUtility),nameof(ChildcareUtility.MakeBabyPlayJob))]
    class MakeBabyPlayJob_Patch
    {
        static bool Prefix(ref Job __result, Pawn feeder)
        {
            Job job = JobMaker.MakeJob(Toddlers_DefOf.BePlayedWith, feeder);
            job.count = 1;
            __result = job;
            return false;
        }
    }
}
