using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Toddlers
{
    public static class Patch_DBH
    {
        public static void GeneratePatches(Harmony harmony)
        {
            /*
            Type class_NeedsUtil = (from asm in AppDomain.CurrentDomain.GetAssemblies()
                                    from type in asm.GetTypes()
                                    where type.Namespace == "DubsBadHygiene" && type.IsClass && type.Name == "NeedsUtil"
                                    select type).Single();
            */
            Type t_NeedsUtil = AccessTools.TypeByName("DubsBadHygiene.NeedsUtil");
            LogUtil.DebugLog("class_NeedsUtil: " + t_NeedsUtil);
            if (t_NeedsUtil == null) return;
            
            MethodInfo m_ShouldHaveNeed = AccessTools.Method(t_NeedsUtil, "ShouldHaveNeed", new Type[] { typeof(Pawn), typeof(NeedDef) });
            LogUtil.DebugLog("m_ShouldHaveNeed: " + m_ShouldHaveNeed);
            if (m_ShouldHaveNeed == null) return;
            
            harmony.Patch(t_NeedsUtil.GetMethod("ShouldHaveNeed", BindingFlags.Public | BindingFlags.Static),
                postfix: new HarmonyMethod(typeof(Patch_DBH),nameof(ShouldHaveNeed_Postfix)));
        }

        public static bool ShouldHaveNeed_Postfix(bool result, Pawn pawn)
        {
            if (result == true && ToddlerUtility.IsToddler(pawn))
                result = false;
            return result;
        }
    }
}
