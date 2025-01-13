using HarmonyLib;
using System;
using Verse;

namespace Toddlers
{
    //when generating toddlers, give age-appropriate hediffs
    [HarmonyPatch(typeof(PawnGenerator),nameof(PawnGenerator.GeneratePawn), new Type[] { typeof(PawnGenerationRequest) })]
    class GeneratePawn_Patch
    {
        static void Postfix(ref Pawn __result)
        {
            if (ToddlerUtility.IsToddler(__result))
            {
                ToddlerLearningUtility.ResetHediffsForAge(__result);
            }
        }
    }

    
}