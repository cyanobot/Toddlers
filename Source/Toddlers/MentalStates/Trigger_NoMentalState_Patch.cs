using HarmonyLib;
using Verse;
using Verse.AI.Group;

namespace Toddlers
{
    [HarmonyPatch(typeof(Trigger_NoMentalState), nameof(Trigger_NoMentalState.ActivateOn))]
    class Trigger_NoMentalState_Patch
    {
        static bool Prefix(ref bool __result, Lord lord, TriggerSignal signal)
        {
            if (signal.type == TriggerSignalType.Tick)
            {
                for (int i = 0; i < lord.ownedPawns.Count; i++)
                {
                    if (lord.ownedPawns[i].InMentalState && lord.ownedPawns[i].DevelopmentalStage != DevelopmentalStage.Baby)
                    {
                        __result = false;
                        return false;
                    }
                }
                __result = true;
                return false;
            }
            __result = false;
            return false;
        }
    }

    
}