using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;

namespace Toddlers
{
    [HarmonyPatch]
    public static class Building_AssignableFixture_Patch
    {
        public static bool Prepare()
        {
            return Toddlers_Mod.DBHLoaded;
        }

        public static MethodBase TargetMethod()
        {
            return AccessTools.TypeByName("DubsBadHygiene.Building_AssignableFixture").GetMethod("GetFloatMenuOptions");
        }

        public static IEnumerable<FloatMenuOption> Postfix(IEnumerable<FloatMenuOption> result, Pawn selPawn)
        {
            List<FloatMenuOption> opts = result.ToList();
            bool isToddler = ToddlerUtility.IsToddler(selPawn);

            foreach (FloatMenuOption option in opts)
            {
                if (isToddler && (option.Label.Contains("Wash".Translate())
                    || option.Label.Contains("StockpileWater".Translate("10"))
                    || option.Label.Contains("StockpileWater".Translate("20")))) continue;

                yield return option;
            }
        }
    }
}
