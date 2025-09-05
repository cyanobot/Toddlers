using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;

#if RW_1_5
#else

namespace Toddlers
{
    [HarmonyPatch(typeof(FloatMenuOptionProvider_WorkGivers), "GetWorkGiverOption")]
    public static class FloatMenuOptionProvider_WorkGivers_DBHPatch
    {
        public static bool Prepare()
        {
            return Toddlers_Mod.DBHLoaded;
        }

        //don't need the menu option for wash patient if the patient is also a baby
        public static bool Prefix(Pawn pawn, WorkGiverDef workGiver, LocalTargetInfo target, FloatMenuContext context)
        {
            if (workGiver == DBHDefOf.washPatient && (target.Pawn?.DevelopmentalStage == DevelopmentalStage.Baby)) return false;
            return true;
        }
    }
}
#endif
