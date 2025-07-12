using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;

#if RW_1_5
#else

namespace Toddlers
{
    [HarmonyPatch]
    public static class WorkGiver_washChild_Patch
    {
        public static bool Prepare()
        {
            return Toddlers_Mod.DBHLoaded;
        }

        public static MethodBase TargetMethod()
        {
            return AccessTools.TypeByName("DubsBadHygiene.WorkGiver_washChild").GetMethod("HasJobOnThing");
        }

        //just scrap this workgiver because the new WorkGiver_WashBaby is more thorough
        public static bool Prefix()
        {
            return false;
        }
    }
}
#endif