using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using UnityEngine;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;

namespace Toddlers
{
    class WanderDestDebug
    {
		public static IntVec3 RandomWanderDestFor(Pawn pawn, IntVec3 root, float radius, Func<Pawn, IntVec3, IntVec3, bool> validator, Danger maxDanger)
		{
			if (radius > 12f)
			{
				Log.Warning("wanderRadius of " + radius + " is greater than Region.GridSize of " + 12 + " and will break.");
			}
			bool flag = true;
			if (root.GetRegion(pawn.Map) != null)
			{
				int maxRegions = Mathf.Max((int)radius / 3, 13);
				bool careAboutSunlight = pawn.genes != null && !pawn.genes.EnjoysSunlight;
				float num = (pawn.RaceProps.Humanlike ? 1f : 0.5f);
				bool careAboutPollution = ModsConfig.BiotechActive && pawn.GetStatValue(StatDefOf.ToxicEnvironmentResistance) < num;
				List<Region> regions = (List<Region>)typeof(RCellFinder).GetField("regions", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
				CellFinder.AllRegionsNear(regions, root.GetRegion(pawn.Map), maxRegions, TraverseParms.For(pawn), (Region reg) => reg.extentsClose.ClosestDistSquaredTo(root) <= radius * radius);
				if (flag)
				{
					pawn.Map.debugDrawer.FlashCell(root, 0.6f, "root");
				}
				if (regions.Count > 0)
				{
					for (int i = 0; i < 35; i++)
					{
						IntVec3 intVec = IntVec3.Invalid;
						for (int j = 0; j < 5; j++)
						{
							IntVec3 randomCell = regions.RandomElementByWeight((Region reg) => reg.CellCount).RandomCell;
							if ((float)randomCell.DistanceToSquared(root) <= radius * radius)
							{
								intVec = randomCell;
								break;
							}
						}
						if (!intVec.IsValid)
						{
							if (flag)
							{
								pawn.Map.debugDrawer.FlashCell(intVec, 0.32f, "distance");
							}
							continue;
						}
						if (!CanWanderToCell(intVec, pawn, root, validator, i, maxDanger, careAboutSunlight, careAboutPollution))
						{
							if (flag)
							{
								pawn.Map.debugDrawer.FlashCell(intVec, 0.6f, "validation");
							}
							continue;
						}
						if (flag)
						{
							pawn.Map.debugDrawer.FlashCell(intVec, 0.9f, "go!");
						}
						regions.Clear();
						return intVec;
					}
				}
				regions.Clear();
			}
			if (!CellFinder.TryFindRandomCellNear(root, pawn.Map, Mathf.FloorToInt(radius), (IntVec3 c) => c.InBounds(pawn.Map) && pawn.CanReach(c, PathEndMode.OnCell, Danger.None) && !c.IsForbidden(pawn) && (validator == null || validator(pawn, c, root)), out var result) && !CellFinder.TryFindRandomCellNear(root, pawn.Map, Mathf.FloorToInt(radius), (IntVec3 c) => c.InBounds(pawn.Map) && pawn.CanReach(c, PathEndMode.OnCell, Danger.None) && !c.IsForbidden(pawn), out result) && !CellFinder.TryFindRandomCellNear(root, pawn.Map, Mathf.FloorToInt(radius), (IntVec3 c) => c.InBounds(pawn.Map) && pawn.CanReach(c, PathEndMode.OnCell, Danger.Deadly), out result) && !CellFinder.TryFindRandomCellNear(root, pawn.Map, 20, (IntVec3 c) => c.InBounds(pawn.Map) && pawn.CanReach(c, PathEndMode.OnCell, Danger.None) && !c.IsForbidden(pawn), out result) && !CellFinder.TryFindRandomCellNear(root, pawn.Map, 30, (IntVec3 c) => c.InBounds(pawn.Map) && pawn.CanReach(c, PathEndMode.OnCell, Danger.Deadly), out result) && !CellFinder.TryFindRandomCellNear(pawn.Position, pawn.Map, 5, (IntVec3 c) => c.InBounds(pawn.Map) && pawn.CanReach(c, PathEndMode.OnCell, Danger.Deadly), out result))
			{
				result = pawn.Position;
			}
			if (flag)
			{
				pawn.Map.debugDrawer.FlashCell(result, 0.4f, "fallback");
			}
			return result;
		}

		private static bool CanWanderToCell(IntVec3 c, Pawn pawn, IntVec3 root, Func<Pawn, IntVec3, IntVec3, bool> validator, int tryIndex, Danger maxDanger, bool careAboutSunlight, bool careAboutPollution)
		{
			bool flag = false;
			if (!c.WalkableBy(pawn.Map, pawn))
			{
				if (flag)
				{
					pawn.Map.debugDrawer.FlashCell(c, 0f, "walk");
				}
				return false;
			}
			if (c.IsForbidden(pawn))
			{
				if (flag)
				{
					pawn.Map.debugDrawer.FlashCell(c, 0.25f, "forbid");
				}
				return false;
			}
			if (tryIndex < 10 && !c.Standable(pawn.Map))
			{
				if (flag)
				{
					pawn.Map.debugDrawer.FlashCell(c, 0.25f, "stand");
				}
				return false;
			}
			if (!pawn.CanReach(c, PathEndMode.OnCell, maxDanger))
			{
				if (flag)
				{
					pawn.Map.debugDrawer.FlashCell(c, 0.6f, "reach");
				}
				return false;
			}
			if (PawnUtility.KnownDangerAt(c, pawn.Map, pawn))
			{
				if (flag)
				{
					pawn.Map.debugDrawer.FlashCell(c, 0.1f, "trap");
				}
				return false;
			}
			if (careAboutSunlight && tryIndex < 20 && c.InSunlight(pawn.Map))
			{
				if (flag)
				{
					pawn.Map.debugDrawer.FlashCell(c, 0.3f, "sun");
				}
				return false;
			}
			if (careAboutPollution && tryIndex < 20 && c.IsPolluted(pawn.Map))
			{
				if (flag)
				{
					pawn.Map.debugDrawer.FlashCell(c, 0.32f, "pol");
				}
				return false;
			}
			if (tryIndex < 10)
			{
				if (c.GetTerrain(pawn.Map).avoidWander)
				{
					if (flag)
					{
						pawn.Map.debugDrawer.FlashCell(c, 0.39f, "terr");
					}
					return false;
				}
				if (pawn.Map.pathing.For(pawn).pathGrid.PerceivedPathCostAt(c) > 20)
				{
					if (flag)
					{
						pawn.Map.debugDrawer.FlashCell(c, 0.4f, "pcost");
					}
					return false;
				}
				if ((int)c.GetDangerFor(pawn, pawn.Map) > 1)
				{
					if (flag)
					{
						pawn.Map.debugDrawer.FlashCell(c, 0.4f, "danger");
					}
					return false;
				}
			}
			else if (tryIndex < 15 && c.GetDangerFor(pawn, pawn.Map) == Danger.Deadly)
			{
				if (flag)
				{
					pawn.Map.debugDrawer.FlashCell(c, 0.4f, "deadly");
				}
				return false;
			}
			if (!pawn.Map.pawnDestinationReservationManager.CanReserve(c, pawn))
			{
				if (flag)
				{
					pawn.Map.debugDrawer.FlashCell(c, 0.75f, "resvd");
				}
				return false;
			}
			if (validator != null && !validator(pawn, c, root))
			{
				if (flag)
				{
					pawn.Map.debugDrawer.FlashCell(c, 0.15f, "valid");
				}
				return false;
			}
			if (c.GetDoor(pawn.Map) != null)
			{
				if (flag)
				{
					pawn.Map.debugDrawer.FlashCell(c, 0.32f, "door");
				}
				return false;
			}
			if (c.ContainsStaticFire(pawn.Map))
			{
				if (flag)
				{
					pawn.Map.debugDrawer.FlashCell(c, 0.9f, "fire");
				}
				return false;
			}
			return true;
		}

	}
}
