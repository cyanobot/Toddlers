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
    [HarmonyPatch(typeof(FloatMenuOptionProvider_Ingest), "GetSingleOptionFor", new Type[] { typeof(Thing), typeof(FloatMenuContext) })]
    public static class FloatMenuOptionProvider_Ingest_Patch
    {
        public static FloatMenuOption Postfix(FloatMenuOption result, Thing clickedThing, FloatMenuContext context)
        {
            if (result == null) return null;
            if (result.Disabled) return result;

            Pawn pawn = context.FirstSelectedPawn;
            if (IsToddler(pawn) && !ToddlerLearningUtility.CanFeedSelf(pawn))
            {
                result.Disabled = true;
                result.Label += " : " + "NotOldEnoughToFeedSelf".Translate();
            }

            return result;
        }
    }
}
#endif