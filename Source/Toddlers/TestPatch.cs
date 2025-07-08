using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using UnityEngine;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Toddlers.ToddlerUtility;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Toddlers
{
    /*
	[HarmonyPatch(typeof(MentalStateHandler),nameof(MentalStateHandler.TryStartMentalState))]
	class TestPatch_TryStartMentalState
    {
        public static void Prefix(MentalStateHandler __instance, Pawn ___pawn, MentalStateDef stateDef, bool forced)
        {
            Log.Message("TryStartMentalState - pawn: " + ___pawn + ", stateDef: " + stateDef + ", forced: " + forced
                + ", MentalBreaksBlocked: " + __instance.MentalBreaksBlocked() );
        }
    }

    */

}
