﻿using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using UnityEngine;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Toddlers
{
    public class LordJob_ToddlerLoiter : LordJob
    {
        private IntVec3 loc;
		private Pawn toddler;
		public override bool AddFleeToil => true;

		public LordJob_ToddlerLoiter() {}

        public LordJob_ToddlerLoiter(Pawn toddler, IntVec3 root)
        {
			//Log.Message("Calling LordJob_ToddlerLoiter");

			this.toddler = toddler;
			//Log.Message("toddler: " + toddler + ", def.race.FenceBlocked: " + toddler.def.race.FenceBlocked + ", roping: " + toddler.roping);
			if (toddler.SpawnedOrAnyParentSpawned)
			{
				this.loc = RCellFinder.RandomWanderDestFor(toddler,root,12,null,Danger.Deadly);
			}
			else this.loc = root;
        }

		public override StateGraph CreateGraph()
		{
			//Log.Message("Calling LordJob_ToddlerLoiter.CreateGraph");

			StateGraph stateGraph = new StateGraph();

			LordToil_ToddlerLoiter lordToil_ToddlerLoiter = new LordToil_ToddlerLoiter(loc);
			stateGraph.AddToil(lordToil_ToddlerLoiter);
			stateGraph.StartingToil = lordToil_ToddlerLoiter;

			return stateGraph;
		}
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref loc, "loc");
			Scribe_References.Look(ref toddler, "toddler");
		}
	}
}
