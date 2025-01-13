using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toddlers
{
    public class LifeStageWorker_HumanlikeToddler : LifeStageWorker
    {
		//public static string LetterTitle = "{PAWN_labelShort} became a toddler";
		//public static string LetterText = "{PAWN_nameFull} is ready to start exploring the world. {PAWN_pronoun} can't work yet, but {PAWN_pronoun} needs less adult attention and over time {PAWN_pronoun} will become more mobile and more capable of attending to {PAWN_possessive} own needs.";

		private static readonly List<BackstoryCategoryFilter> ToddlerBackstoryFilters = new List<BackstoryCategoryFilter>
		{
			new BackstoryCategoryFilter
			{
				categories = new List<string> { "Toddler" }
			}
		};

		public  override void Notify_LifeStageStarted(Pawn pawn, LifeStageDef previousLifeStage)
        {
            base.Notify_LifeStageStarted(pawn, previousLifeStage);
			if (Current.ProgramState != ProgramState.Playing)
			{
				return;
			}
			if (previousLifeStage == null || !previousLifeStage.developmentalStage.Baby())
			{
				return;
			}
			if (pawn.story.bodyType != BodyTypeDefOf.Baby)
			{
				Pawn_ApparelTracker apparel2 = pawn.apparel;
				if (apparel2 != null)
				{
					apparel2.DropAllOrMoveAllToInventory((Apparel apparel) => !apparel.def.apparel.developmentalStageFilter.Has(DevelopmentalStage.Baby));
				}
				BodyTypeDef bodyTypeFor = PawnGenerator.GetBodyTypeFor(pawn);
				pawn.story.bodyType = bodyTypeFor;
				pawn.Drawer.renderer.SetAllGraphicsDirty();
			}
			ToddlerLearningUtility.ResetHediffsForAge(pawn);
			if (PawnUtility.ShouldSendNotificationAbout(pawn))
            {
				ChoiceLetter let = LetterMaker.MakeLetter("LetterTitleBecameToddler".Translate(pawn.Named("PAWN")), "LetterTextBecameToddler".Translate(pawn.Named("PAWN")), LetterDefOf.PositiveEvent, pawn, null, null, null);
				Find.LetterStack.ReceiveLetter(let, null);

				if (pawn.Spawned)
				{
					EffecterDefOf.Birthday.SpawnAttached(pawn, pawn.Map, 1f);
				}
			}
			PawnBioAndNameGenerator.FillBackstorySlotShuffled(pawn, BackstorySlot.Childhood, ToddlerBackstoryFilters, null);
			pawn.Notify_DisabledWorkTypesChanged();
		}
    }
}
