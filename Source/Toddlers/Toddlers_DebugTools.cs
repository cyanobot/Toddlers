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
using LudeonTK;

namespace Toddlers
{
    class Toddlers_DebugTools
    {
        [DebugAction(category: "Spawning", name: null, 
            requiresRoyalty: false, requiresIdeology: false, requiresBiotech: true, requiresAnomaly: false,
            displayPriority: 1000, hideInSubMenu: false, 
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static List<DebugActionNode> SpawnBaby()
        {
            return (List<DebugActionNode>)typeof(DebugToolsSpawning)
                .GetMethod("SpawnAtDevelopmentalStages", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { DevelopmentalStage.Baby });
        }

        [DebugAction(category: "Pawns", name: null, 
            requiresRoyalty: false, requiresIdeology: false, requiresBiotech: true, requiresAnomaly: false,
            displayPriority: 1000, hideInSubMenu: false, 
            actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ResetToddlerHediffs(Pawn p)
        {
            ToddlerLearningUtility.ResetHediffsForAge(p);
        }

    }
}
