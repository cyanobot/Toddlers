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
    class ToddlerPlayGiver_WatchTelevision : ToddlerPlayGiver
    {
        public override bool CanDo(Pawn pawn)
        {
            return base.CanDo(pawn) && this.FindNearbyInteractableTelevision(pawn) != null;
        }
        public override bool CanDoFromCrib(Pawn pawn)
        {
            //LogUtil.DebugLog("Fired ToddlerPlayGiver_WatchTelevision.CanDoFromCrib");

            return base.CanDoFromCrib(pawn) && this.FindTelevisionFromCrib(pawn) != null;
        }

        public override Job TryGiveJob(Pawn pawn)
        {
            Thing t;
            if ((t = this.FindNearbyInteractableTelevision(pawn)) != null)
            {
                if (!WatchBuildingUtility.TryFindBestWatchCell(t, pawn, false, out var result, out var chair))
                {
                    return null;
                }
                Job job = JobMaker.MakeJob(this.def.jobDef, t, result, chair);
                job.count = 1;
                return job;
            }
            return null;
        }

        public Job TryGiveJobFromCrib(Pawn pawn)
        {
            //LogUtil.DebugLog("Fired ToddlerPlayGiver_WatchTelevision.TryGiveJobFromCrib");

            Thing tv = FindTelevisionFromCrib(pawn);
            if (tv == null) return null;
            Job job;
            if (pawn.InBed()) job = JobMaker.MakeJob(this.def.jobDef, tv, pawn.Position, pawn.CurrentBed());
            else job = JobMaker.MakeJob(this.def.jobDef, tv, pawn.Position, pawn.Position);
            job.count = 1;
            return job;
        }

        private Thing FindTelevisionFromCrib(Pawn pawn)
        {
            Room room = pawn.GetRoom(RegionType.Set_All);
            if (room != null)
            {
                //LogUtil.DebugLog("Found room");
                foreach (Thing thing in room.ContainedAndAdjacentThings)
                {
                    if (ToddlerPlayUtility.TelevisionDefs.Contains(thing.def) && CanInteractWith(pawn, thing))
                    {
                        //LogUtil.DebugLog("Found television");

                        object[] prms = { pawn.Position, thing.Position, pawn.Map, true, thing.def };
                        if ((bool)typeof(WatchBuildingUtility).GetMethod("EverPossibleToWatchFrom", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, prms))
                        {
                            //LogUtil.DebugLog("EverPossibleToWatchFrom : true");
                            return thing;
                        }
                        //LogUtil.DebugLog("EverPossibleToWatchFrom : false");
                    }
                }
            }
            return null;
        }

        private Thing FindNearbyInteractableTelevision(Pawn pawn)
        {
            Room room = pawn.GetRoom(RegionType.Set_All);
            if (room != null)
            {
                foreach (Thing thing in room.ContainedAndAdjacentThings)
                {
                    if (ToddlerPlayUtility.TelevisionDefs.Contains(thing.def) && CanInteractWith(pawn, thing)) return thing;
                }
            }
            List<Thing> candidates = new List<Thing>();
            foreach (ThingDef televisionDef in ToddlerPlayUtility.TelevisionDefs)
            {
                candidates.AddRange(pawn.Map.listerThings.ThingsOfDef(televisionDef));
            }
            if (candidates.Count == 0) return null;
            Predicate<Thing> predicate = (Thing t) => CanInteractWith(pawn, t);
            Thing result = GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, candidates, Verse.AI.PathEndMode.OnCell, TraverseParms.For(pawn), MaxTelevisionDistance, predicate);
            return result;
        }

        protected virtual bool CanInteractWith(Pawn pawn, Thing t)
        {
            if (t.IsForbidden(pawn) || t.IsBurning()) return false;
            if (!pawn.CanReserve(t, ToddlerPlayUtility.TelevisionMaxParticipants))
            {
                return false;
            }
            CompPowerTrader compPowerTrader = t.TryGetComp<CompPowerTrader>();
            if (compPowerTrader != null && !compPowerTrader.PowerOn)
            {
                return false;
            }
            return true;
        }

        private const float MaxTelevisionDistance = 15.9f;
    }
}
