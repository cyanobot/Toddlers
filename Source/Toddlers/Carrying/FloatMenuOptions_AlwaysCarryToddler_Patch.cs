using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;
using static Toddlers.ToddlerUtility;

#if RW_1_5
#else

namespace Toddlers
{
    [HarmonyPatch]
    public static class FloatMenuOptions_AlwaysCarryToddler_Patch
    {
        public static MethodInfo m_get_Downed = AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.Downed));
        public static MethodInfo m_DownedOrToddler = AccessTools.Method(typeof(FloatMenuOptions_AlwaysCarryToddler_Patch),
            nameof(FloatMenuOptions_AlwaysCarryToddler_Patch.DownedOrToddler));

        public static IEnumerable<MethodBase> TargetMethods()
        {
            Type[] argTypes = new Type[] { typeof(Pawn), typeof(FloatMenuContext) };

            yield return AccessTools.Method(typeof(FloatMenuOptionProvider_CarryPawn), "GetSingleOptionFor", argTypes);
            yield return AccessTools.Method(typeof(FloatMenuOptionProvider_CarryPawnToExit), "GetSingleOptionFor", argTypes);
            yield return AccessTools.Method(typeof(FloatMenuOptionProvider_CarryToShuttle), "CanBeCarriedToShuttle");

            foreach (Type type in new Type[] { typeof(JobDriver_CarryDownedPawn), typeof(JobDriver_TakeAndEnterPortal), typeof(JobDriver_Kidnap), typeof(JobDriver_CarryToBiosculpterPod) })
            {
                MemberInfo[] memberInfos = type.GetMembers(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                foreach (MemberInfo member in memberInfos)
                {
                    //LogUtil.DebugLog($"member: {member}, MemberType: {member.MemberType}");
                    if (member.MemberType == MemberTypes.Method && member.Name.Contains("MakeNewToils")) yield return member as MethodInfo; 
                }
            }

        }

        public static bool DownedOrToddler(Pawn pawn)
        {
            //LogUtil.DebugLog($"DownedOrToddler - pawn: {pawn}");
            if (pawn.Downed) return true;
            if (IsToddler(pawn)) return true;
            return false;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            //LogUtil.DebugLog($"FloatMenuOptions_AlwaysCarryToddler_Patch Transpiler starting for: {__originalMethod?.DeclaringType}.{__originalMethod}");
            foreach (CodeInstruction cur in instructions)
            {
                if (cur.Calls(m_get_Downed))
                {
                    //LogUtil.DebugLog($"FloatMenuOptions_AlwaysCarryToddler_Patch Transpiler found insert point in: {__originalMethod?.DeclaringType}.{__originalMethod}");
                    yield return new CodeInstruction(OpCodes.Call, m_DownedOrToddler);
                }
                else
                {
                    yield return cur;
                }
            }
        }
    }
}
#endif