using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;
using RimWorld;
using static Toddlers.LogUtil;
using Verse;

namespace Toddlers
{
    [HarmonyPatch]
    public static class HatPairValidator_Patch
    {
        public static MethodBase TargetMethod()
        {
            Type t_PossibleApparelSet = AccessTools.Inner(typeof(PawnApparelGenerator), "PossibleApparelSet");
            //DebugLog("t_PossibleApparelSet: " + t_PossibleApparelSet);

            IEnumerable<object> members = t_PossibleApparelSet.GetMembers(BindingFlags.Static | BindingFlags.NonPublic);            
            foreach (object member in members)
            {
                //DebugLog("member: " + member + ", type: " + member.GetType());
                if (member is Type memberType)
                {
                    //DebugLog("memberType: " + memberType);
                    //methods.AddRange(memberType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic));
                    foreach (MethodBase method in memberType.GetRuntimeMethods())
                    {
                        if (method.Name.Contains("HatPairValidator")) return method;
                    }
                }
            }

            return null;
        }

        public static bool Postfix(bool __result, Pawn ___pawn, ThingStuffPair pa)
        {
            if (!__result) return false;
            if (!pa.thing.apparel.CorrectAgeForWearing(___pawn))
            {
                return false;
            }

            return __result;
        }
    }
}
