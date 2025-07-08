using System;
using System.Collections.Generic;
using System.Linq;
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
    [HarmonyPatch(typeof(FloatMenuOptionProvider_Wear), "GetSingleOptionFor", new Type[] { typeof(Thing), typeof(FloatMenuContext) })]
    public static class FloatMenuOptionProvider_Wear_Patch
    {
        public static FloatMenuOption Postfix(FloatMenuOption result, FloatMenuContext context)
        {
            if (result == null) return null;
            if (result.Disabled) return result;

            Pawn pawn = context.FirstSelectedPawn;
            if (IsToddler(pawn) && !ToddlerLearningUtility.CanDressSelf(pawn))
            {
                result.Disabled = true;
                result.Label += " : " + "NotOldEnoughToDressSelf".Translate();
            }

            return result;
        }
    }
}
#endif
