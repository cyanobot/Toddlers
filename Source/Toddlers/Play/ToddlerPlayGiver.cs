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
    class ToddlerPlayGiver
	{
		public virtual bool CanDo(Pawn pawn)
		{
			return ToddlerUtility.IsToddler(pawn) && !pawn.Downed && !PawnUtility.WillSoonHaveBasicNeed(pawn, 0.1f) && pawn.needs.play != null && pawn.needs.play.CurLevel < 0.9f;
		}

		public virtual bool CanDoWhileDowned(Pawn pawn)
        {
			return ToddlerUtility.IsToddler(pawn) && !PawnUtility.WillSoonHaveBasicNeed(pawn, 0.1f) && pawn.needs.play != null && pawn.needs.play.CurLevel < 0.9f;
		}

		public virtual Job TryGiveJob(Pawn pawn)
		{
			return JobMaker.MakeJob(this.def.jobDef);
		}

		public ToddlerPlayDef def;
	}
}
