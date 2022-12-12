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
    class ThinkNode_ConditionalInCrib : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn) => ToddlerUtility.InCrib(pawn);
    }
}
