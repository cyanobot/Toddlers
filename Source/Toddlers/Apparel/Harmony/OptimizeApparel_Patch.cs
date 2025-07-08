using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Toddlers
{

    [HarmonyPatch(typeof(JobGiver_OptimizeApparel),"TryGiveJob")]
    class OptimizeApparel_Patch
    {
        static void Postfix(Job __result, Pawn pawn)
        {
            if (ToddlerUtility.IsToddler(pawn) && __result != null)
                __result.haulDroppedApparel = false;
        }
    }
}
