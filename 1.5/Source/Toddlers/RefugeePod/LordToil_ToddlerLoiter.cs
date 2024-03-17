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
using System.Linq;
using System.Text;
namespace Toddlers
{
    public class LordToil_ToddlerLoiter : LordToil
    {
		private IntVec3 location;
		public override bool AllowSatisfyLongNeeds => true;

        public LordToil_ToddlerLoiter(IntVec3 location) 
		{
			this.location = location;
		}

		public override void UpdateAllDuties()
		{
			for (int i = 0; i < lord.ownedPawns.Count; i++)
			{
				PawnDuty duty = new PawnDuty(Toddlers_DefOf.ToddlerLoiter, location);
				lord.ownedPawns[i].mindState.duty = duty;
			}
		}

	}
}
