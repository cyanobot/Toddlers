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
    class Toddlers_DebugTools
    {
        [DebugAction("Spawning", null, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = 1000, requiresBiotech = true)]
        private static List<DebugActionNode> SpawnBaby()
        {
            return (List<DebugActionNode>)typeof(DebugToolsSpawning)
                .GetMethod("SpawnAtDevelopmentalStages", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { DevelopmentalStage.Baby });
        }

        [DebugAction("Pawns", null, false, false, false, 0, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = 1000)]
        private static void ResetToddlerHediffs(Pawn p)
        {
            ToddlerUtility.ResetHediffsForAge(p);
        }
    }
}
