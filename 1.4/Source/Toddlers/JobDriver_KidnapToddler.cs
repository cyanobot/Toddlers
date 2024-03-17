using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
namespace Toddlers
{
    //basically a copy of JobDriver_Kidnap
    //that allows for kidnapping awake toddlers
    public class JobDriver_KidnapToddler : JobDriver_TakeAndExitMap
    {
		protected Pawn Takee => (Pawn)base.Item;

		public override string GetReport()
		{
			if (Takee == null || pawn.HostileTo(Takee))
			{
				return base.GetReport();
			}
			return JobUtility.GetResolvedJobReport(JobDefOf.Rescue.reportString, Takee);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOn(() => Takee == null || (!ToddlerUtility.IsToddler(Takee)));
			foreach (Toil item in base.MakeNewToils())
			{
				yield return item;
			}
		}
	}
}
