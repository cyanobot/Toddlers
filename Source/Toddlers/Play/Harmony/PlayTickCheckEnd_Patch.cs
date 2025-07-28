using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Toddlers
{
    [HarmonyPatch(typeof(BabyPlayUtility),nameof(BabyPlayUtility.PlayTickCheckEnd))]
    public static class PlayTickCheckEnd_Patch
    {
        public static bool Postfix(bool result, Pawn baby)
        {
            //if play isn't full, don't stop playing
            if (!result) return false;

            //if play is full, also check loneliness before deciding to stop playing
            if (ToddlerPlayUtility.GetLoneliness(baby) > 0.01f) return false;

            return true;
        }
    }
}
