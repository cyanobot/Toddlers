using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;

#if RW_1_5
#else
namespace Toddlers
{
    [HarmonyPatch(typeof(RimWorld.FloatMenuOptionProvider_BringBabyToSafety), "GetSingleOptionFor")]
    public static class FloatMenuOptionProvider_BringBabyToSafety_Patch
    {
        //eliminate this provider altogether in favour of our custom one
        public static bool Prefix(ref FloatMenuOption __result)
        {
            __result = null;
            return false;
        }
    }
}
#endif