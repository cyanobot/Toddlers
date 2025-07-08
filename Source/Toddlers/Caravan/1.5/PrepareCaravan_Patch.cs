using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI.Group;
#if RW_1_5
namespace Toddlers
{
    //treat toddlers (in some ways) as downed pawns when trying to form caravans
    [HarmonyPatch]
    class PrepareCaravan_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(LordToil_PrepareCaravan_GatherDownedPawns), nameof(LordToil_PrepareCaravan_GatherDownedPawns.UpdateAllDuties));
            yield return AccessTools.Method(typeof(LordToil_PrepareCaravan_GatherDownedPawns), "CheckAllPawnsArrived");
            yield return AccessTools.Method(typeof(JobGiver_PrepareCaravan_GatherDownedPawns), "FindDownedPawn");
            yield return AccessTools.Method(typeof(LordToil_PrepareCaravan_RopeAnimals), nameof(LordToil_PrepareCaravan_GatherDownedPawns.UpdateAllDuties));
            yield return AccessTools.Method(typeof(LordToil_PrepareCaravan_GatherItems), nameof(LordToil_PrepareCaravan_GatherDownedPawns.UpdateAllDuties));
            yield return AccessTools.Method(typeof(LordToil_PrepareCaravan_GatherItems), nameof(LordToil_PrepareCaravan_GatherDownedPawns.LordToilTick));
        }

        static List<Pawn> FindDownedAndToddlers(LordJob_FormAndSendCaravan lordJob)
        {
            return lordJob.downedPawns.Concat(lordJob.lord.ownedPawns.Where(x => ToddlerUtility.IsToddler(x))).ToList();
        }

        static MethodInfo m_IsColonist = AccessTools.Property(typeof(Pawn), nameof(Pawn.IsColonist)).GetGetMethod();
        static MethodInfo m_Downed = AccessTools.Property(typeof(Pawn), nameof(Pawn.Downed)).GetGetMethod();
        static MethodInfo m_IsToddler = AccessTools.Method(typeof(ToddlerUtility), nameof(ToddlerUtility.IsToddler));
        static MethodInfo m_FindDownedAndToddlers = AccessTools.Method(typeof(PrepareCaravan_Patch), nameof(PrepareCaravan_Patch.FindDownedAndToddlers));

        static FieldInfo f_lord = AccessTools.Field(typeof(LordToil), nameof(LordToil.lord));
        static FieldInfo f_downedPawns = AccessTools.Field(typeof(LordJob_FormAndSendCaravan), nameof(LordJob_FormAndSendCaravan.downedPawns));

        /*
        static void Prefix(object[] __args, MethodBase __originalMethod)
        {
            Log.Message(__originalMethod.Name + " firing, args: " + __args.ToStringSafeEnumerable());
        }
        */
        
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction prevInstruction = null;
            foreach (var instruction in instructions)
            { 
                //convert all instances of
                //pawn.IsColonist
                //to
                //pawn.IsColonist && !ToddlerUtility.IsToddler(pawn)
                if (instruction.Calls(m_IsColonist))
                {
                    yield return instruction;
                    yield return prevInstruction;
                    yield return new CodeInstruction(OpCodes.Callvirt, m_IsToddler);
                    yield return new CodeInstruction(OpCodes.Not);
                    yield return new CodeInstruction(OpCodes.And);
                }
                //convert
                //pawn.Downed
                //to
                //pawn.Downed || ToddlerUtility.IsToddler(pawn)
                else if (instruction.Calls(m_Downed))
                {
                    yield return instruction;
                    yield return prevInstruction;
                    yield return new CodeInstruction(OpCodes.Callvirt, m_IsToddler);
                    yield return new CodeInstruction(OpCodes.Or);
                }
                //convert
                //downedPawns = ((LordJob_FormAndSendCaravan)lord.LordJob).downedPawns;
                //to
                //downedPawns = ((LordJob_FormAndSendCaravan)lord.LordJob).downedPawns.Concat(FindToddlers(lord)).ToList();
                else if (instruction.LoadsField(f_downedPawns))
                {
                    yield return new CodeInstruction(OpCodes.Call, m_FindDownedAndToddlers);
                }
                else
                {
                    yield return instruction;
                }

                prevInstruction = instruction;
            }
        }
        
    }

    
}
#endif