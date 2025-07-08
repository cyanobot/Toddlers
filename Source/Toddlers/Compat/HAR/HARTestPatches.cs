using HarmonyLib;
using LudeonTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Toddlers
{
    /*
    [HarmonyPatch]
    public static class HARTestPatches_ConditionSatisfied
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            List<Type> types = AccessTools.TypeByName("AlienRace.ExtendedGraphics.Condition").AllSubclassesNonAbstract();
            foreach (Type type in types)
            {
                MethodBase method = AccessTools.Method(type, "Satisfied");
                if (method != null) yield return method;
            }
        }

        public static void Prefix(object __instance)
        {
            LogUtil.DebugLog("HARTestPatches_ConditionSatisfied - Condition: "
                + __instance
                );
        }
    }
    */
}
