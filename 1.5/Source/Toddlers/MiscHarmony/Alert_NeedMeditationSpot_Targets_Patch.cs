using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Toddlers
{
    [HarmonyPatch]
    public static class Alert_NeedMeditationSpot_Targets_Patch
    {
        public static MethodInfo TargetMethod()
        {
            return AccessTools.PropertyGetter(typeof(Alert_NeedMeditationSpot), "Targets");
        }

        public static void Postfix(List<GlobalTargetInfo> __result)
        {
            __result.RemoveAll(t => t.Pawn != null && t.Pawn.DevelopmentalStage == Verse.DevelopmentalStage.Baby);
        }
    }
}
