using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Toddlers
{
    public static class HARCompatBridge
    {
        public static Type t_HARFunctions;
        public static MethodInfo m_HARToddlerMinAge;
        public static MethodInfo m_HARToddlerEndAge;
        public static MethodInfo m_HasHumanlikeGait;


        public static Type t_HARCompat;
        public static MethodInfo m_Init;

        public static void Init()
        {
            try
            {
                t_HARFunctions = AccessTools.TypeByName("Toddlers.HARFunctions");

                m_HARToddlerMinAge = AccessTools.Method(t_HARFunctions, "HARToddlerMinAge");
                m_HARToddlerEndAge = AccessTools.Method(t_HARFunctions, "HARToddlerEndAge");
                m_HasHumanlikeGait = AccessTools.Method(t_HARFunctions, "HasHumanlikeGait");

                t_HARCompat = AccessTools.TypeByName("Toddlers.HARCompat");
                m_Init = AccessTools.Method(t_HARCompat, "Init");

                m_Init.Invoke(null, new object[] { });
            }
            catch (Exception e)
            {
                Log.Error("[Toddlers] Patch for Humanoid Alien Races failed: " + e.Message + ", StackTrace: " + e.StackTrace);
                Toddlers_Mod.HARLoaded = false;
            }
        }

        public static float HARToddlerMinAge(Pawn p)
        {
            return (float)m_HARToddlerMinAge.Invoke(null, new object[] { p });
        }

        public static float HARToddlerEndAge(Pawn p)
        {
            return (float)m_HARToddlerEndAge.Invoke(null, new object[] { p });
        }

        public static bool HasHumanlikeGait(Pawn p)
        {
            return (bool)m_HasHumanlikeGait.Invoke(null, new object[] { p });
        }
    }
}
