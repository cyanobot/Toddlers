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
    class ToddlerPlayGiver_PlayToys : ToddlerPlayGiver
    {
        public override bool CanDo(Pawn pawn)
        {
            Thing t;
            return base.CanDo(pawn) && (t = this.FindNearbyUseableToyBox(pawn)) != null && pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.None, 1, -1, null, false);
        }

        public override bool CanDoWhileDowned(Pawn pawn)
        {
            return false;
        }

        public override Job TryGiveJob(Pawn pawn)
        {
            Thing t;
            if ((t = this.FindNearbyUseableToyBox(pawn)) != null)
            {
                Job job = JobMaker.MakeJob(this.def.jobDef, t);
                job.count = 1;
                return job;
            }
            return null;
        }

        private bool IsValidToyBox(Thing toybox, Pawn pawn)
        {
            if (toybox == null) return false;
            return toybox.def == ThingDefOf.ToyBox && !toybox.IsForbidden(pawn) && !toybox.IsBurning() 
                && pawn.CanReserveAndReach(toybox, PathEndMode.Touch, Danger.None, 1, -1, null, false) 
                && pawn.Position.DistanceTo(toybox.Position) <= MaxToyBoxDistance;
        }

        private Thing FindNearbyUseableToyBox(Pawn pawn)
        {
            Room room = pawn.GetRoom(RegionType.Set_All);
            if (room != null)
            {
                foreach (Thing thing in room.ContainedThings(ThingDefOf.ToyBox))
                {
                    if (this.IsValidToyBox(thing, pawn))
                    {
                        return thing;
                    }
                }
            }
            return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(ThingDefOf.ToyBox), PathEndMode.OnCell, 
                TraverseParms.For(pawn, Danger.None, TraverseMode.ByPawn, false, false, false), 15.9f, 
                (Thing t) => this.IsValidToyBox(t, pawn), null, 0, -1, false, RegionType.Set_Passable, false);
        }

        private const float MaxToyBoxDistance = 15.9f;
    }
}
