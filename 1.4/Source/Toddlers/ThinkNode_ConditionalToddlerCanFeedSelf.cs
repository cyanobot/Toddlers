using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toddlers
{
    class ThinkNode_ConditionalToddlerCanFeedSelf : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            if (ToddlerUtility.CanFeedSelf(pawn)) return true;
            return false;
        }

        public override float GetPriority(Pawn pawn)
        {
            return subNodes[0].GetPriority(pawn);
        }
    }
}
