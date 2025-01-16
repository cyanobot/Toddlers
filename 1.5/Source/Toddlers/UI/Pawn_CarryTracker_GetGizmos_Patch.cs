using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;


namespace Toddlers
{
    [HarmonyPatch(typeof(Pawn_CarryTracker),nameof(Pawn_CarryTracker.GetGizmos))]
    public static class Pawn_CarryTracker_GetGizmos_Patch
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn_CarryTracker __instance)
        {
            foreach (Gizmo gizmo in __result) yield return gizmo;

            if (!__instance.pawn.Drafted && __instance.CarriedThing is Pawn carriedPawn)
            {
                Command_Action command_Action = new Command_Action();
                command_Action.defaultLabel = "CommandDropPawn".Translate(carriedPawn);
                command_Action.defaultDesc = "CommandDropPawnDesc".Translate();
                command_Action.action = delegate
                {
                    __instance.TryDropCarriedThing(__instance.pawn.Position, ThingPlaceMode.Near, out var _);
                };
                command_Action.icon = TexCommand.DropCarriedPawn;
                yield return command_Action;
            }
        }
    }
}
