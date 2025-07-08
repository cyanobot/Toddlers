using HarmonyLib;
using RimWorld;
using System.Reflection;
using Verse;

namespace Toddlers
{
    [HarmonyPatch(typeof(ITab_Pawn_Gear))]
    class ITab_Pawn_Gear_Patch
    {
        public static MethodBase TargetMethod()
        {
            return typeof(ITab_Pawn_Gear).GetProperty(nameof(ITab_Pawn_Gear.IsVisible), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(false);
        }

        static bool Postfix(bool result, ITab_Pawn_Gear __instance)
        {
            if (result == true) return true;

            Pawn selPawnForGear = (Pawn)typeof(ITab_Pawn_Gear).GetProperty("SelPawnForGear", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            if (selPawnForGear.ageTracker.CurLifeStage == LifeStageDefOf.HumanlikeBaby)
            {
                object[] prms = new object[] { selPawnForGear };
                if (!(bool)typeof(ITab_Pawn_Gear).GetMethod("ShouldShowInventory", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, prms) && !(bool)typeof(ITab_Pawn_Gear).GetMethod("ShouldShowApparel", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, prms))
                {
                    return (bool)typeof(ITab_Pawn_Gear).GetMethod("ShouldShowEquipment", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, prms);
                }
                return true;
            }

            return result;
        }
    }
}
