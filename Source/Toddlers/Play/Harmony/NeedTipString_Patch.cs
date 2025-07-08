using HarmonyLib;
using RimWorld;
using System.Reflection;
using Verse;
using static Toddlers.ToddlerUtility;
using static Toddlers.ToddlerPlayUtility;

namespace Toddlers
{
    [HarmonyPatch(typeof(Need), nameof(Need.GetTipString))]
    class NeedTipString_Patch
    {
        static string Postfix(string result, ref Need __instance)
        {
            //not interested in needs other than Play
            if (!(__instance is Need_Play)) return result;

            Pawn pawn = (Pawn)typeof(Need).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);

            //not interested if not a toddler
            if (!IsToddler(pawn)) return result;

            string header = (__instance.LabelCap + ": " + __instance.CurLevelPercentage.ToStringPercent()).Colorize(ColoredText.TipSectionTitleColor);
            string body = "NeedTipStringPlay".Translate();
            string lonelyReport = "Loneliness".Translate() + ": " + GetLoneliness(pawn).ToStringPercent();


            return header + "\n" + body + "\n\n" + lonelyReport;
        }
    }
}
