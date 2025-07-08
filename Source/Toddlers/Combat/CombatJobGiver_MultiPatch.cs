using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;
using Verse.AI;
using static Toddlers.ToddlerUtility;

namespace Toddlers
{

    //make sure toddlers never initiate violence
    //for some reason being incapable of it is insufficient 
    [HarmonyPatch]
    class CombatJobGiver_MultiPatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (Type type in new Type[] {
                typeof(JobGiver_AIFightEnemy),
                typeof(JobGiver_AIGotoNearestHostile),
                typeof(JobGiver_AISapper),
                typeof(JobGiver_AIWaitAmbush),
                typeof(JobGiver_ManTurrets)
            })
            {
                MethodInfo method = type.GetMethod("TryGiveJob", BindingFlags.NonPublic | BindingFlags.Instance);
                if (method != null) yield return method;
            }
        }

        static Job Postfix(Job __result, Pawn pawn)
        {
            if (IsToddler(pawn)) return null;
            return __result;
        }
    }

    
}