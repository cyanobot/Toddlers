using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using UnityEngine;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Toddlers
{
    class QuestPart_ToddlerLoiter : QuestPart_MakeLord
	{
		public Pawn LeadToddler => pawns[0];

		protected override Lord MakeLord()
		{
			//Log.Message("Calling QuestPart_ToddlerLoiter.MakeLord, pawns: " + pawns.ToStringSafeEnumerable());

			IntVec3 loc = LeadToddler.PositionHeld;
	
			LordJob_ToddlerLoiter lordJob = new LordJob_ToddlerLoiter(LeadToddler, loc);
			return LordMaker.MakeNewLord(LeadToddler.Faction, lordJob, mapParent.Map);
		}

		public override void ExposeData()
		{
			base.ExposeData();
		}

		public override void Notify_QuestSignalReceived(Signal signal)
		{
			//Log.Message("QuestPart_ToddlerLoiter received signal: " + signal + ", args: " + signal.args.Args.ToStringSafeEnumerable());
			//Log.Message("Looking for signal: " + inSignal);
			if (signal.tag == inSignal)
			{
				bool foundPawn = false;
				foreach (NamedArgument arg in signal.args.Args)
                {
					if (arg.label == SignalArgsNames.Subject && pawns.Contains(arg.arg))
                    {
						//Log.Message("Notify_QuestSignalReceived found relevant pawn: " + arg.arg);
						foundPawn = true;
						break;
                    }
                }
				if (foundPawn)
				{
					base.Notify_QuestSignalReceived(signal);
				}
			}
			else if (signal.tag == inSignalRemovePawn)
            {
				base.Notify_QuestSignalReceived(signal);
			}
		}
	}
}
