using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toddlers
{
    class Hediff_LearningToWalk : Hediff_ToddlerLearning
    {
        private const float maxWobblyPenalty = -0.5f;

        public override string SettingName { get => "learningFactor_Walk"; }

        public float Progress
        {
            get
            {
                if (CurStageIndex != 1) return 0f;
                return (Severity - CurStage.minSeverity) / (1f - CurStage.minSeverity);
            }
        }

        public override void OnUpdate(int stageIndex)
        {
            if (stageIndex == 1)
            {
                CurStage.capMods.Clear();
                CurStage.capMods.Add(this.WobblyCapacityModifier);
            }
        }

        public override void OnStageUp(int newStageIndex)
        {
            if (newStageIndex == 1 && !(pawn.ParentHolder is Building_GrowthVat))
            {
                Find.LetterStack.ReceiveLetter("first steps", "{PAWN_labelShort} is ready to take {PAWN_possessive} first steps. {PAWN_pronoun} can now open doors and escape from {PAWN_possessive} crib.".Formatted(pawn.Named("PAWN")), LetterDefOf.NeutralEvent, pawn);
            }
        }

        public PawnCapacityModifier WobblyCapacityModifier
        {
            get
            {
                if (CurStageIndex != 1) return null;

                PawnCapacityModifier result= new PawnCapacityModifier();

                result.capacity = PawnCapacityDefOf.Moving;
                result.offset = maxWobblyPenalty * (1f - Progress);

                return result;
            }
        }
    }
}
