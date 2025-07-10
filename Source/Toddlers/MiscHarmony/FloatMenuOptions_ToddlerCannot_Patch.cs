using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    public static class FloatMenuOptions_ToddlerCannot_SingleOption_Patch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            Type[] argTypes_Thing = new Type[] { typeof(Thing), typeof(FloatMenuContext) };
            Type[] argTypes_Pawn = new Type[] { typeof(Pawn), typeof(FloatMenuContext) };

            yield return AccessTools.Method(typeof(FloatMenuOptionProvider_Arrest), "GetSingleOptionFor", argTypes_Pawn);
            yield return AccessTools.Method(typeof(FloatMenuOptionProvider_CapturePawn), "GetSingleOptionFor", argTypes_Pawn);
            yield return AccessTools.Method(typeof(FloatMenuOptionProvider_CarryDeathrestingToCasket), "GetSingleOptionFor", argTypes_Pawn);
            yield return AccessTools.Method(typeof(FloatMenuOptionProvider_CarryMechToCharger), "GetSingleOptionFor", argTypes_Pawn);
            yield return AccessTools.Method(typeof(FloatMenuOptionProvider_CarryPawn), "GetSingleOptionFor", argTypes_Pawn);
            yield return AccessTools.Method(typeof(FloatMenuOptionProvider_CarryPawnToExit), "GetSingleOptionFor", argTypes_Pawn);
            yield return AccessTools.Method(typeof(FloatMenuOptionProvider_CarryToCryptosleepCasket), "GetSingleOptionFor", argTypes_Pawn);
            yield return AccessTools.Method(typeof(FloatMenuOptionProvider_CarryToShuttle), "GetSingleOptionFor", argTypes_Pawn);
            yield return AccessTools.Method(typeof(FloatMenuOptionProvider_DressOtherPawn), "GetSingleOptionFor", argTypes_Thing);
            yield return AccessTools.Method(typeof(FloatMenuOptionProvider_PutOutFireOnPawn), "GetSingleOptionFor", argTypes_Pawn);
            yield return AccessTools.Method(typeof(FloatMenuOptionProvider_RescuePawn), "GetSingleOptionFor", argTypes_Pawn);
            yield return AccessTools.Method(typeof(FloatMenuOptionProvider_ReturnSlaveToBed), "GetSingleOptionFor", argTypes_Pawn);
            yield return AccessTools.Method(typeof(FloatMenuOptionProvider_Strip), "GetSingleOptionFor", argTypes_Pawn);
        }

        public static FloatMenuOption Postfix(FloatMenuOption __result, FloatMenuContext context, MethodBase __originalMethod)
        {
            LogUtil.DebugLog($"FloatMenuOptions_ToddlerCannot_SingleOption_Patch - original: {__originalMethod.DeclaringType}" +
                $", selectedPawn: {context.FirstSelectedPawn}, result: {__result}" +
                $", IsToddler: {IsToddler(context.FirstSelectedPawn)}"
                );
            if (__result != null && IsToddler(context.FirstSelectedPawn)) return null;
            return __result;
        }
    }

    [HarmonyPatch]
    public static class FloatMenuOptions_ToddlerCannot_Options_Patch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            Type[] argTypes_Thing = new Type[] { typeof(Thing), typeof(FloatMenuContext) };
            Type[] argTypes_Pawn = new Type[] { typeof(Pawn), typeof(FloatMenuContext) };

            yield return AccessTools.Method(typeof(FloatMenuOptionProvider_CaptureEntity), "GetOptionsFor", argTypes_Thing);
            yield return AccessTools.Method(typeof(FloatMenuOptionProvider_CarryToBiosculpterPod), "GetOptionsFor", argTypes_Pawn);
            yield return AccessTools.Method(typeof(FloatMenuOptionProvider_Childcare), "GetOptionsFor", argTypes_Pawn);
            yield return AccessTools.Method(typeof(FloatMenuOptionProvider_HandleCorpse), "GetOptionsFor", argTypes_Thing);
            yield return AccessTools.Method(typeof(FloatMenuOptionProvider_LoadCaravan), "GetOptionsFor", argTypes_Thing);
            yield return AccessTools.Method(typeof(FloatMenuOptionProvider_LoadOntoPackAnimal), "GetOptionsFor", argTypes_Thing);
            yield return AccessTools.Method(typeof(FloatMenuOptionProvider_Reload), "GetOptionsFor", argTypes_Thing);
            yield return AccessTools.Method(typeof(FloatMenuOptionProvider_TransferEntity), "GetOptionsFor", argTypes_Thing);
        }

        public static IEnumerable<FloatMenuOption> Postfix(IEnumerable<FloatMenuOption> __result, FloatMenuContext context)
        {
            List<FloatMenuOption> options = __result.ToList();

            if (options.NullOrEmpty()) yield break;
            if (IsToddler(context.FirstSelectedPawn)) yield break;

            foreach (FloatMenuOption option in options) yield return option;
        }
    }
}
#endif