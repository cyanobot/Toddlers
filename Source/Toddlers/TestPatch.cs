using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using UnityEngine;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Toddlers.ToddlerUtility;

namespace Toddlers
{

    [HarmonyPatch]
    class CaravanForming_TestPatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            //yield return AccessTools.Method(typeof(CaravanFormingUtility), nameof(CaravanFormingUtility.StartFormingCaravan));
            yield return AccessTools.Constructor(typeof(LordJob_FormAndSendCaravan), new Type[] { typeof(List<TransferableOneWay>), typeof(List<Pawn>), typeof(IntVec3), typeof(IntVec3), typeof(int), typeof(int) });
            //yield return AccessTools.Method(typeof(LordMaker), nameof(LordMaker.MakeNewLord));
            yield return AccessTools.Method(typeof(Lord), nameof(Lord.GotoToil));
            //yield return AccessTools.Method(typeof(LordToil_PrepareCaravan_GatherAnimals), nameof(LordToil_PrepareCaravan_GatherAnimals.Init));
            yield return AccessTools.Method(typeof(LordToil_PrepareCaravan_GatherAnimals), nameof(LordToil_PrepareCaravan_GatherAnimals.UpdateAllDuties));
            //yield return AccessTools.Method(typeof(LordToil_PrepareCaravan_GatherItems), nameof(LordToil_PrepareCaravan_GatherItems.Init));
            yield return AccessTools.Method(typeof(LordToil_PrepareCaravan_GatherItems), nameof(LordToil_PrepareCaravan_GatherItems.UpdateAllDuties));
            //yield return AccessTools.Method(typeof(LordToil_PrepareCaravan_GatherDownedPawns), nameof(LordToil_PrepareCaravan_GatherDownedPawns.Init));
            yield return AccessTools.Method(typeof(LordToil_PrepareCaravan_GatherDownedPawns), nameof(LordToil_PrepareCaravan_GatherDownedPawns.UpdateAllDuties));
            yield return AccessTools.Method(typeof(LordToil_PrepareCaravan_GatherDownedPawns), nameof(LordToil_PrepareCaravan_GatherDownedPawns.Notify_PawnJobDone));
            yield return AccessTools.Method(typeof(LordToil_PrepareCaravan_GatherDownedPawns), "CheckAllPawnsArrived");
            yield return AccessTools.DeclaredMethod(typeof(JobGiver_PrepareCaravan_GatherDownedPawns), "TryGiveJob");
            yield return AccessTools.DeclaredMethod(typeof(JobGiver_PrepareCaravan_GatherDownedPawns), "FindDownedPawn");
            yield return AccessTools.Method(typeof(Lord), nameof(Lord.ReceiveMemo));
            //yield return AccessTools.Constructor(typeof(Transition), new Type[] { typeof(LordToil), typeof(LordToil), typeof(bool), typeof(bool) });
        }
        static void Prepare(MethodBase original)
        {
            //Log.Message("Prepare, original: " + original);
        }
        static void Prefix(MethodInfo __originalMethod, object[] __args)
        {
            Log.Message(__originalMethod.DeclaringType + "." + __originalMethod.Name + " firing, args: " + __args.ToStringSafeEnumerable());
        }
    }


    [HarmonyPatch(typeof(LordToil_PrepareCaravan_GatherDownedPawns),"CheckAllPawnsArrived")]
    class OneMethod_TestPatch
    {
        static void Prefix()
        {
            Log.Message("OneMethod_TestPatch firing");
        }

    }

}
