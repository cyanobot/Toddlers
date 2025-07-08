using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Toddlers
{
    //makes young toddlers eat on the floor
    [HarmonyPatch(typeof(Toils_Ingest), nameof(Toils_Ingest.CarryIngestibleToChewSpot))]
    class CarryIngestibleToChewSpot_Patch
    {
        static Toil Postfix(Toil result, Pawn pawn, TargetIndex ingestibleInd)
        {
            if (!ToddlerUtility.IsLiveToddler(pawn) || !ToddlerLearningUtility.EatsOnFloor(pawn)) return result;

            result.initAction = delegate
            {
                Pawn actor = result.actor;
                IntVec3 cell = IntVec3.Invalid;
                Thing food = actor.CurJob.GetTarget(ingestibleInd).Thing;

                cell = RCellFinder.SpotToChewStandingNear(actor, actor.CurJob.GetTarget(ingestibleInd).Thing, (IntVec3 c) => actor.CanReserveSittableOrSpot(c) && c.GetDangerFor(actor, actor.Map) == Danger.None);
                actor.ReserveSittableOrSpot(cell, actor.CurJob);
                actor.Map.pawnDestinationReservationManager.Reserve(actor, actor.CurJob, cell);
                actor.pather.StartPath(cell, PathEndMode.OnCell);
            };
            return result;
        }
    }

    
}