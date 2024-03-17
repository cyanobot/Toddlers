using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.AI;
using UnityEngine;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Toddlers
{
    class QuestNode_Root_RefugeePodCrash_Baby : QuestNode_Root_WandererJoin
	{
		private const float ChanceToTryGenerateParent = 0.5f;

		private const string HasParentFlagName = "hasParent";

		protected override bool TestRunInt(Slate slate)
		{
			return Find.Storyteller.difficulty.ChildrenAllowed;
		}

		public override Pawn GeneratePawn()
		{
			Find.FactionManager.TryGetRandomNonColonyHumanlikeFaction(out var faction, tryMedievalOrBetter: true);
			PawnGenerationRequest request = new PawnGenerationRequest(PawnKindDefOf.SpaceRefugee, faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: true, allowDead: false, allowDowned: true, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, forceRecruitable: true);
			request.AllowedDevelopmentalStages = DevelopmentalStage.Baby;
			Pawn pawn = PawnGenerator.GeneratePawn(request);
			PawnComponentsUtility.AddComponentsForSpawn(pawn);       //calling this early in the spawn process, so as to initialise mood, etc
			pawn.needs?.mood?.thoughts?.memories.TryGainMemory(Toddlers_DefOf.Toddlers_TraumaticCrash);
			pawn.ageTracker.AgeChronologicalTicks = pawn.ageTracker.AgeBiologicalTicks;
			return pawn;
		}

		protected override void AddSpawnPawnQuestParts(Quest quest, Map map, Pawn pawn)
		{
			//Log.Message("Calling AddSpawnPawnQuestParts");

			bool deadParent = false;
			Pawn parent = null;

			List<Thing> list = new List<Thing> { pawn };
			if (Rand.Value < 0.5f)
			{
				Pawn mother = pawn.GetMother();
				bool flag_motherless = mother == null || Find.WorldPawns.GetSituation(mother) == WorldPawnSituation.None;
				Pawn father = pawn.GetFather();
				bool flag_fatherless = father == null || Find.WorldPawns.GetSituation(father) == WorldPawnSituation.None;
				if (flag_motherless || flag_fatherless)
				{
					deadParent = true;
					QuestGen.slate.Set("hasParent", var: true);
					PawnGenerationRequest request = new PawnGenerationRequest(PawnKindDefOf.SpaceRefugee, pawn.Faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: true, allowDead: true, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, forcedXenotype: pawn.genes?.Xenotype ?? null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: true);
					if (flag_motherless && !flag_fatherless)
					{
						request.FixedGender = Gender.Female;
					}
					else if (flag_fatherless && !flag_motherless)
					{
						request.FixedGender = Gender.Male;
					}
					else if (Rand.Value < 0.5f)
					{
						request.FixedGender = Gender.Female;
					}
					else
					{
						request.FixedGender = Gender.Male;
					}
					parent = PawnGenerator.GeneratePawn(request);
					pawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, parent);
					list.Add(parent.Corpse);
				}
			}
			QuestGen.slate.Set("hasParent", var: deadParent);
			quest.DropPods(map.Parent, list, null, null, null, null, false, useTradeDropSpot: false, joinPlayer: false, makePrisoners: false, null, null, QuestPart.SignalListenMode.OngoingOnly, null, destroyItemsOnCleanup: true, dropAllInSamePod: true);

			if (ToddlerUtility.IsLiveToddler(pawn))
			{
				//Log.Message("Calling toddler code");
				ToddlerLoiter(quest, map.Parent, new Pawn[] { pawn }, pawn.Faction, null);				
			}

		}

		//AS VANILLA
		public override void SendLetter(Quest quest, Pawn pawn)
		{
			TaggedString title = "LetterLabelRefugeePodCrash".Translate();
			TaggedString letterText = "RefugeePodCrashBaby".Translate(pawn.Named("PAWN")).AdjustedFor(pawn);
			if (QuestGen.slate.Get("hasParent", defaultValue: false))
			{
				letterText += "\n\n" + "RefugeePodCrashBabyHasParent".Translate(pawn.Named("PAWN")).AdjustedFor(pawn);
			}
			QuestNode_Root_WandererJoin_WalkIn.AppendCharityInfoToLetter("JoinerCharityInfo".Translate(pawn), ref letterText);
			PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref letterText, ref title, pawn);
			Find.LetterStack.ReceiveLetter(title, letterText, LetterDefOf.NeutralEvent, new TargetInfo(pawn));
		}

		public static QuestPart_ToddlerLoiter ToddlerLoiter(Quest quest, MapParent mapParent, IEnumerable<Pawn> pawns, Faction faction,  string inSignal = null)
		{
			string spawnedSignal_tag = "Quest" + quest.id + "." + "pawn" + "." + QuestUtility.QuestTargetSignalPart_Spawned;

			QuestPart_ToddlerLoiter questPart_ToddlerLoiter = new QuestPart_ToddlerLoiter();
			questPart_ToddlerLoiter.inSignal = inSignal ?? spawnedSignal_tag;
			questPart_ToddlerLoiter.pawns.AddRange(pawns);
			questPart_ToddlerLoiter.mapParent = mapParent;
			questPart_ToddlerLoiter.faction = faction;
			quest.AddPart(questPart_ToddlerLoiter);
			return questPart_ToddlerLoiter;
		}
	}

}
