using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Toddlers
{
    [HarmonyPatch(typeof(LordJob_FormAndSendCaravan),nameof(LordJob_FormAndSendCaravan.Notify_PawnAdded))]
    public static class LordJob_FormAndSendCaravan_Notify_PawnAdded_Patch
    {
        public static void Postfix(LordJob_FormAndSendCaravan __instance, Pawn p)
        {
            if (ToddlerUtility.IsToddler(p))
            {
                __instance.downedPawns.Add(p);
            }
        }
    }
}
