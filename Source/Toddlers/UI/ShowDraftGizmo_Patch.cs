using HarmonyLib;
using RimWorld;
using System.Reflection;
using Verse;

namespace Toddlers
{
    [HarmonyPatch(typeof(Pawn_DraftController))]
    class ShowDraftGizmo_Patch
    {
        public static MethodBase TargetMethod()
        {
            return typeof(Pawn_DraftController).GetProperty(nameof(Pawn_DraftController.ShowDraftGizmo), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(false);
        }

        static bool Postfix(bool result, Pawn_DraftController __instance, Pawn ___pawn)
        {
            if (!__instance.Drafted && ToddlerUtility.IsToddler(___pawn) && !Toddlers_Settings.canDraftToddlers)
            {
                return false;
            }
            return result;
        }
    }

    
}