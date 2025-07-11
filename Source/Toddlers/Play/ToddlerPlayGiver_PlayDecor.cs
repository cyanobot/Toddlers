using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toddlers
{
    class ToddlerPlayGiver_PlayDecor: ToddlerPlayGiver
    {
        private const float MaxDecorDistance = 15.9f;

        public override bool CanDo(Pawn pawn)
        {
            Thing t;
            return base.CanDo(pawn) && (t = this.FindNearbyUseableDecor(pawn)) != null && pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.None, 1, -1, null, false);
        }

        public override bool CanDoFromCrib(Pawn pawn)
        {
            return false;
        }

        public override Job TryGiveJob(Pawn pawn)
        {
            Thing t;
            if ((t = this.FindNearbyUseableDecor(pawn)) != null)
            {
                Job job = JobMaker.MakeJob(this.def.jobDef, t);
                job.count = 1;
                return job;
            }
            return null;
        }

        private bool IsValidDecor(Thing decor, Pawn pawn)
        {
            if (decor.def != ThingDefOf.BabyDecoration || decor.IsForbidden(pawn) || decor.IsBurning()
                || pawn.Position.DistanceTo(decor.Position) > MaxDecorDistance)
                return false;
            if (ToddlerPlayUtility.PlayOnCell(decor))
                return pawn.CanReserveAndReach(decor, PathEndMode.OnCell, Danger.None);
            else
                return pawn.CanReserveAndReach(decor, PathEndMode.Touch, Danger.None);
        }

        private Thing FindNearbyUseableDecor(Pawn pawn)
        {
            Room room = pawn.GetRoom(RegionType.Set_All);
            if (room != null)
            {
                foreach (Thing thing in room.ContainedThings(ThingDefOf.ToyBox))
                {
                    if (this.IsValidDecor(thing, pawn))
                    {
                        return thing;
                    }
                }
            }
            return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(ThingDefOf.BabyDecoration), PathEndMode.OnCell, TraverseParms.For(pawn, Danger.None, TraverseMode.ByPawn, false, false, false), 15.9f, (Thing t) => this.IsValidDecor(t, pawn), null, 0, -1, false, RegionType.Set_Passable, false);
        }
    }
}
