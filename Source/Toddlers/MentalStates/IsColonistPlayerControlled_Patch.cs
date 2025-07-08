using HarmonyLib;
using System.Reflection;
using Verse;

namespace Toddlers
{
    //crying/giggling toddlers still count as PlayerControlled
    //which allows them to eg be given orders, drafted
    [HarmonyPatch(typeof(Pawn))]
    class IsColonistPlayerControlled_Patch
    {
        public static MethodBase TargetMethod()
        {
            return typeof(Pawn).GetProperty(nameof(Pawn.IsColonistPlayerControlled), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(false);
        }

        static bool Postfix(bool result, Pawn __instance)
        {
            if (result == false && __instance.Spawned && __instance.IsColonist
                && (__instance.HostFaction == null || __instance.IsSlave)
                && ToddlerUtility.IsLiveToddler(__instance))
            {
                result = true;
            }
            return result;
        }
    }

    
}