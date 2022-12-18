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
    class ToddlerPlayGiver_Floordrawing : ToddlerPlayGiver
	{
		//copied directly from ILSpy's decompile of LearningGiver_Floordrawing
		public static bool TryFindFloordrawingSpots(Pawn pawn, out IntVec3 drawFrom, out IntVec3 drawOn)
		{
			TraverseParms traverseParams = TraverseParms.For(pawn, Danger.None);
			IntVec3 innerDrawFrom = IntVec3.Invalid;
			IntVec3 innerDrawOn = IntVec3.Invalid;
			FindDrawCell(desperate: false);
			if (innerDrawFrom == IntVec3.Invalid)
			{
				FindDrawCell(desperate: true);
			}
			drawFrom = innerDrawFrom;
			drawOn = innerDrawOn;
			if (drawFrom != IntVec3.Invalid && drawOn != IntVec3.Invalid)
			{
				return true;
			}
			return false;
			void FindDrawCell(bool desperate)
			{
				RegionTraverser.BreadthFirstTraverse(pawn.Position, pawn.Map, (Region from, Region r) => r.Allows(traverseParams, isDestination: false), delegate (Region r)
				{
					if (r.IsForbiddenEntirely(pawn))
					{
						return false;
					}
					IntVec3 result;
					return r.DangerFor(pawn) != Danger.Deadly && r.TryFindRandomCellInRegion(CellValidator, out result);
				}, desperate ? 200 : 40);
				bool CellValidator(IntVec3 c)
				{
					if (!CanFloorDrawFrom(c, pawn))
					{
						return false;
					}
					bool canDrawOnRoot = CanDrawOn(c, pawn.Map, desperate);
					IntVec3[] array = offsets;
					foreach (IntVec3 intVec in array)
					{
						IntVec3 intVec2 = c + intVec;
						if (CanFloorDrawFrom(intVec2, pawn) && TryFloorDrawCellPair(intVec2, canDrawOnRoot, pawn, desperate, out var shouldDrawOnRoot))
						{
							innerDrawOn = (shouldDrawOnRoot ? c : intVec2);
							innerDrawFrom = (shouldDrawOnRoot ? intVec2 : c);
							return true;
						}
					}
					return false;
				}
			}
		}

		public static bool CanFloorDrawFrom(IntVec3 spot, Pawn drawer)
		{
			Map map = drawer.Map;
			return spot.InBounds(map) && spot.Standable(map) && !spot.IsForbidden(drawer) && map.areaManager.Home[spot] && spot.GetDoor(map) == null && drawer.CanReserve(spot, 1, -1, null, false);
		}

		private static bool CanDrawOn(IntVec3 cell, Map map, bool desperate)
		{
			return FilthMaker.CanMakeFilth(cell, map, ThingDefOf.Filth_Floordrawing, FilthSourceFlags.Pawn) && (desperate || (cell.GetFirstItem(map) == null && cell.GetFirstBuilding(map) == null && cell.GetFirstThing<Thing>(map) == null));
		}

		public static bool TryFloorDrawCellPair(IntVec3 companionCell, bool canDrawOnRoot, Pawn drawer, bool desperate, out bool shouldDrawOnRoot)
		{
			shouldDrawOnRoot = false;
			if (!ToddlerPlayGiver_Floordrawing.CanFloorDrawFrom(companionCell, drawer))
			{
				return false;
			}
			bool flag = ToddlerPlayGiver_Floordrawing.CanDrawOn(companionCell, drawer.Map, desperate);
			if (!canDrawOnRoot && !flag)
			{
				return false;
			}
			if (!canDrawOnRoot)
			{
				return true;
			}
			if (!flag)
			{
				shouldDrawOnRoot = true;
				return true;
			}
			shouldDrawOnRoot = Rand.Bool;
			return true;
		}

		public override bool CanDo(Pawn pawn)
		{
			IntVec3 intVec;
			IntVec3 intVec2;
			return base.CanDo(pawn) && ToddlerUtility.CanDressSelf(pawn) && ToddlerPlayGiver_Floordrawing.TryFindFloordrawingSpots(pawn, out intVec, out intVec2);
		}

		public override bool CanDoWhileDowned(Pawn pawn)
		{
			return false;
		}

		public override Job TryGiveJob(Pawn pawn)
		{
			IntVec3 c;
			IntVec3 c2;
			if (!ToddlerPlayGiver_Floordrawing.TryFindFloordrawingSpots(pawn, out c, out c2))
			{
				return null;
			}
			return JobMaker.MakeJob(this.def.jobDef, c, c2);
		}

		private static readonly IntVec3[] offsets = new IntVec3[]
		{
			IntVec3.East,
			IntVec3.South
		};
	}
}
