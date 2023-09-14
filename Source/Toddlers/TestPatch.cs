using RimWorld;
using Verse;
using Verse.AI;
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

    [HarmonyPatch(typeof(RestUtility), nameof(RestUtility.FindBedFor), new Type[] { typeof(Pawn), typeof(Pawn), typeof(bool), typeof(bool), typeof(GuestStatus?)})]
	class FindBedFor_Patch
	{
		static bool Prefix(Pawn sleeper, Pawn traveler, bool checkSocialProperness, bool ignoreOtherReservations, GuestStatus? guestStatus)
		{
			Log.Message("FindBedFor - sleeper: " + sleeper + ", traveler: " + traveler + ", checkSocialProperness: " + checkSocialProperness
				+ ", ignoreOtherReservations: " + ignoreOtherReservations + ", guestStatus: " + guestStatus);

			//aka list
			List<ThingDef> list_medical = (List<ThingDef>)typeof(RestUtility).GetField("bedDefsBestToWorst_Medical", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
			//aka list2
			List<ThingDef> list_rest = (List<ThingDef>)typeof(RestUtility).GetField("bedDefsBestToWorst_RestEffectiveness", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

			Log.Message("ShouldSeekMedicalRest: " + HealthAIUtility.ShouldSeekMedicalRest(sleeper));
			if (HealthAIUtility.ShouldSeekMedicalRest(sleeper))
			{
				string medical = sleeper.InBed() ? sleeper.CurrentBed().Medical.ToString() : "N/A";
				string valid = sleeper.InBed() ? RestUtility.IsValidBedFor(sleeper.CurrentBed(), sleeper, traveler, checkSocialProperness, allowMedBedEvenIfSetToNoCare: false, ignoreOtherReservations, guestStatus).ToString()
					: "N/A";
				Log.Message("InBed: " + sleeper.InBed()
					+ ", CurrentBed.Medical: " + medical + ", IsValidBedFor:" + valid);
				if (sleeper.InBed() && sleeper.CurrentBed().Medical && RestUtility.IsValidBedFor(sleeper.CurrentBed(), sleeper, traveler, checkSocialProperness, allowMedBedEvenIfSetToNoCare: false, ignoreOtherReservations, guestStatus))
				{
					return true;
				}
				Log.Message("Iterating over list_medical...");
				for (int i = 0; i < list_medical.Count; i++)
				{
					ThingDef thingDef = list_medical[i];
					if (!RestUtility.CanUseBedEver(sleeper, thingDef))
					{
						continue;
					}
					for (int j = 0; j < 2; j++)
					{
						Danger maxDanger2 = ((j == 0) ? Danger.None : Danger.Deadly);
						Building_Bed building_Bed = (Building_Bed)GenClosest.ClosestThingReachable(sleeper.Position, sleeper.MapHeld, 
							ThingRequest.ForDef(thingDef), PathEndMode.OnCell, TraverseParms.For(traveler), 9999f, 
							(Thing b) => ((Building_Bed)b).Medical && (int)b.Position.GetDangerFor(sleeper, sleeper.Map) <= (int)maxDanger2 && 
							RestUtility.IsValidBedFor(b, sleeper, traveler, checkSocialProperness, allowMedBedEvenIfSetToNoCare: false, ignoreOtherReservations, guestStatus));
						if (building_Bed != null)
						{
							Log.Message("Iteration selected: " + building_Bed);
							return true;
						}
					}
				}
				Log.Message("Falling through from medical logic...");
			}
			Building_Bed ownedBed = sleeper.ownership == null ? null : sleeper.ownership.OwnedBed;
			Log.Message("sleeper.ownership: " + sleeper.ownership + ", OwnedBed:" + ownedBed
				+ "IsValidBedFor: " + (ownedBed == null ? "N/A" : RestUtility.IsValidBedFor(sleeper.ownership.OwnedBed, sleeper, traveler, checkSocialProperness, allowMedBedEvenIfSetToNoCare: false, ignoreOtherReservations, guestStatus).ToString()));
			if (sleeper.ownership != null && sleeper.ownership.OwnedBed != null && RestUtility.IsValidBedFor(sleeper.ownership.OwnedBed, sleeper, traveler, checkSocialProperness, allowMedBedEvenIfSetToNoCare: false, ignoreOtherReservations, guestStatus))
			{
				return true;
			}
			Log.Message("Ignoring section about relations, iterating over danger...");
			for (int dg = 0; dg < 3; dg++)
			{
				Danger maxDanger = ((dg <= 1) ? Danger.None : Danger.Deadly);
				Log.Message("dg: " + dg + ", maxDanger: " + maxDanger + ", corpse allowed: " + (dg > 0));
				Log.Message("Iterating over list_rest...");
				for (int k = 0; k < list_rest.Count; k++)
				{
					ThingDef thingDef2 = list_rest[k];
					if (!RestUtility.CanUseBedEver(sleeper, thingDef2))
					{
						continue;
					}
					Building_Bed building_Bed2 = (Building_Bed)GenClosest.ClosestThingReachable(sleeper.PositionHeld, sleeper.MapHeld, 
						ThingRequest.ForDef(thingDef2), PathEndMode.OnCell, TraverseParms.For(traveler), 9999f, 
						(Thing b) => !((Building_Bed)b).Medical && 
						(int)b.Position.GetDangerFor(sleeper, sleeper.MapHeld) <= (int)maxDanger && 
						RestUtility.IsValidBedFor(b, sleeper, traveler, checkSocialProperness, allowMedBedEvenIfSetToNoCare: false, ignoreOtherReservations, guestStatus) 
						&& (dg > 0 || !b.Position.GetItems(b.Map).Any((Thing thing) => thing.def.IsCorpse)));
					if (building_Bed2 != null)
					{
						Log.Message("Iteration selected: " + building_Bed2);
					}
				}
			}



			return true;
		}
	}

}
