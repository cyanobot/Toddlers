using HarmonyLib;
using System.Reflection;
using Verse;
using Verse.AI;

namespace Toddlers
{
    [HarmonyPatch(typeof(Pawn_MindState))]
    class IsIdle_Patch
    {
        public static MethodBase TargetMethod()
        {
            return typeof(Pawn_MindState).GetProperty(nameof(Pawn_MindState.IsIdle), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(false);
        }
        static bool Postfix(bool result, Pawn ___pawn)
        {
            if (ToddlerUtility.IsToddler(___pawn)) return false;
            return result;
        }
    }

    
}