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
    class ToddlerPlayGiver_Bugwatching : ToddlerPlayGiver
    {
        public override bool CanDo(Pawn pawn)
        {
            IntVec3 intVec;
            return base.CanDo(pawn)
                && TryFindBugwatchCell(pawn.Position, pawn, out intVec)
                && (JoyUtility.EnjoyableOutsideNow(pawn)
                 || !intVec.GetRoom(pawn.Map).PsychologicallyOutdoors);
        }

        public override bool CanDoWhileDowned(Pawn pawn)
        {
            return false;
        }

        public override Job TryGiveJob(Pawn pawn)
        {
            IntVec3 c;
            if (!TryFindBugwatchCell(pawn.Position, pawn, out c))
            {
                return null;
            }
            return JobMaker.MakeJob(this.def.jobDef, c);
        }

        public static bool TryFindBugwatchCell(IntVec3 root, Pawn searcher, out IntVec3 result)
        {
            Predicate<IntVec3> cellValidator = delegate (IntVec3 c) {
                return !c.GetTerrain(searcher.Map).avoidWander
                    && searcher.SafeTemperatureAtCell(c, searcher.MapHeld)
                    && !c.IsForbidden(searcher);
                };
            Predicate<Region> validator = delegate (Region r)
            {
                IntVec3 intVec;
                return r.Room.PsychologicallyOutdoors && !r.IsForbiddenEntirely(searcher) 
                    && r.TryFindRandomCellInRegionUnforbidden(searcher, cellValidator, out intVec);
            };
            TraverseParms traverseParms = TraverseParms.For(searcher, Danger.None, TraverseMode.ByPawn, false, false, false);
            Region root2;
            if (!CellFinder.TryFindClosestRegionWith(root.GetRegion(searcher.Map,RegionType.Set_Passable), traverseParms, validator,100, out root2, RegionType.Set_Passable))
            {
                result = root;
                return false;
            }
            return CellFinder.RandomRegionNear(root2, 4, traverseParms, validator, searcher, RegionType.Set_Passable).TryFindRandomCellInRegionUnforbidden(searcher, cellValidator, out result);
        }
    }
}
