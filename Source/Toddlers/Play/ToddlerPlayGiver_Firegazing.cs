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
    class ToddlerPlayGiver_Firegazing : ToddlerPlayGiver
    {
        public static List<ThingDef> validFireDefs = null;

        public static void InitValidFireDefs()
        {
            /*
            validFireDefs = new List<ThingDef>();
            validFireDefs.Add(ThingDefOf.Campfire);
            if (ModsConfig.RoyaltyActive) validFireDefs.Add(ThingDefOf.Brazier);
            if (ModsConfig.RoyaltyActive && ModsConfig.IdeologyActive)
                validFireDefs.Add(DefDatabase<ThingDef>.GetNamed("DarklightBrazier"));
            */
            validFireDefs = Toddlers_DefOf.FiregazingTargets.whitelist;

            LogUtil.DebugLog("validFireDefs: " + validFireDefs.ToStringSafeEnumerable());
        }

        public override bool CanDo(Pawn pawn)
        {
            if (!base.CanDo(pawn)) return false;
            Thing fire = FindNearbyUseableFire(pawn);
            if (fire == null | !pawn.CanReach(fire, PathEndMode.Touch, Danger.None)) return false;
            IntVec3 standCell = FindFiregazingSpot(pawn,fire);
            if (standCell == IntVec3.Invalid | !pawn.CanReserveAndReach(standCell, PathEndMode.OnCell, Danger.None)) return false;
            return true;
        }

        public override bool CanDoFromCrib(Pawn pawn)
        {
            return false;
        }

        public override Job TryGiveJob(Pawn pawn)
        {
            Thing fire = this.FindNearbyUseableFire(pawn);
            if (fire == null) return null;
            IntVec3 standCell = this.FindFiregazingSpot(pawn, fire);
            if (standCell == IntVec3.Invalid) return null;
            
            Job job = JobMaker.MakeJob(this.def.jobDef, fire, standCell);
            job.count = 1;
            return job;
        }

        private bool IsValidFire(Thing fire, Pawn pawn)
        {
            if (fire.IsForbidden(pawn) || !pawn.CanReach(fire, PathEndMode.Touch, Danger.Some) || pawn.Position.DistanceTo(fire.Position) >= MaxFireDistance)
            {
                return false;
            }
            if (validFireDefs == null) InitValidFireDefs();
            if (!validFireDefs.Contains(fire.def)) return false;

            CompRefuelable compRefuelable = fire.TryGetComp<CompRefuelable>();
            if (compRefuelable != null && !compRefuelable.HasFuel) return false;

            //patch for Extinguish Refuelables - can't gaze into a fire that's not lit
            //if (Toddlers_Mod.extinguishRefuelablesLoaded)
            //{
                CompFlickable compFlickable = fire.TryGetComp<CompFlickable>(); ;
                if (compFlickable == null) return true;
                if (!compFlickable.SwitchIsOn) return false;
            //}

            return true;
        }

        private Thing FindNearbyUseableFire(Pawn pawn)
        {
            //Log.Message("Fired FindNearbyUseableFire");
            if (validFireDefs != null && validFireDefs.Count == 0) return null;
            Room room = pawn.GetRoom(RegionType.Set_All);
            if (room != null)
            {
                if (validFireDefs == null) InitValidFireDefs();
                foreach (Thing thing in room.ContainedThingsList(validFireDefs))
                {
                    if (this.IsValidFire(thing, pawn))
                    {
                        return thing;
                    }
                }
            }
            return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.OnCell, TraverseParms.For(pawn, Danger.None, TraverseMode.ByPawn, false, false, false), 15.9f, (Thing t) => this.IsValidFire(t, pawn), null, 0, -1, false, RegionType.Set_Passable, false);
        }

        private IntVec3 FindFiregazingSpot(Pawn pawn, Thing fire)
        {
            foreach(IntVec3 cell in GenAdj.CellsAdjacent8Way(fire).InRandomOrder())
            {
                if (pawn.CanReserveAndReach(cell,PathEndMode.OnCell,Danger.None)) return cell;
            }
            return IntVec3.Invalid;
        }

        private const float MaxFireDistance = 15.9f;
    }
}
