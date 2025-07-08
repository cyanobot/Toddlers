using HarmonyLib;
using Verse;
using Verse.AI.Group;

namespace Toddlers
{
    //babies crying or giggling should not interrupt lord jobs like rituals or caravan formation
    [HarmonyPatch(typeof(Trigger_MentalState), nameof(Trigger_MentalState.ActivateOn))]
    class Trigger_MentalState_Patch
    {
        static bool Prefix(ref bool __result, Lord lord, TriggerSignal signal)
        {
            if (signal.type == TriggerSignalType.Tick)
            {
                for (int i = 0; i < lord.ownedPawns.Count; i++)
                {
                    if (lord.ownedPawns[i].InMentalState && lord.ownedPawns[i].DevelopmentalStage != DevelopmentalStage.Baby)
                    {
                        __result = true;
                        return false;
                    }
                }
            }
            __result = false;
            return false;
        }
    }

    
}