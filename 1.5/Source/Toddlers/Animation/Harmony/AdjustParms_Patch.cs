using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Toddlers
{
    [HarmonyPatch(typeof(PawnRenderTree), "AdjustParms")]
    class AdjustParms_Patch
    {
        static void Postfix(PawnRenderTree __instance, ref PawnDrawParms parms)
        {
            Pawn pawn = __instance.pawn;
            if (!parms.Portrait && parms.facing == Rot4.South && pawn.Spawned
                && pawn.Drawer.renderer.CurAnimation == Toddlers_AnimationDefOf.ToddlerCrawl
                && !pawn.Downed && pawn.pather.Moving)
            {
                parms.facing = Rot4.North;
                parms.flipHead = true;
            }
        }
    }
}
