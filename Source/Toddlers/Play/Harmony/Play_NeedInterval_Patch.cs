using HarmonyLib;
using RimWorld;
using System.Reflection;
using Verse;
using static Toddlers.ToddlerUtility;

namespace Toddlers
{
    [HarmonyPatch(typeof(Need_Play), nameof(Need_Play.NeedInterval))]
    class Play_NeedInterval_Patch
    {
        static bool Prefix(ref Need_Play __instance)
        {
            bool isFrozen = (bool)typeof(Need_Play).GetProperty("IsFrozen", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            if (!isFrozen)
            {
                Pawn pawn = (Pawn)typeof(Need_Play).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
                float factor = IsToddler(pawn) ? Toddlers_Settings.playFallFactor_Toddler : Toddlers_Settings.playFallFactor_Baby;
                __instance.CurLevel -= Need_Play.BaseFallPerInterval * factor;
            }
            return false;
        }
    }
}
