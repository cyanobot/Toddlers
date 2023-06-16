using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;
using HarmonyLib;
using UnityEngine;

namespace Toddlers
{
	[HarmonyPatch()]
	class Seek_Patch_NonVoid
	{
		static Type[] targetTypes = new Type[]
		{
			typeof(RimWorld.PawnApparelGenerator)
		};

		static IEnumerable<MethodBase> TargetMethods()
		{

			foreach (Type type in targetTypes)
			{
				Log.Message("Attempting to patch type " + type.Name);
				MethodBase newMethod;
				foreach (MethodBase methodBase in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic
					| BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
				{
					//leave void methods for second patch
					if (methodBase is MethodInfo && ((MethodInfo)methodBase).ReturnType == typeof(void))
						continue;

					newMethod = AccessTools.TypeByName("Toddlers." + type.Name).GetMethod(methodBase.Name,
						BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
					//Log.Message("newMethod: " + newMethod);
					if (newMethod != null)
					{
						Log.Message("Attempting to patch method " + methodBase.Name);
						yield return methodBase;
					}
					else
					{
						Log.Message("No newMethod provided, not patching method " + methodBase.Name);
					}
				}
			}
		}

		static bool Prepare()
        {
			if (TargetMethods().EnumerableNullOrEmpty()) 
				return false;
			else return true;
        }

		static bool Prefix(object[] __args, MethodBase __originalMethod, ref object __result, object __instance)
		{
			Log.Message("Firing Prefix for " + __originalMethod.Name);
			string methodName = __originalMethod.Name;
			string className = "Toddlers." + __originalMethod.DeclaringType.Name;
			MethodBase newMethod = AccessTools.TypeByName(className).GetMethod(methodName,
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

			if (newMethod == null)
			{
				Log.Message("Averting prefix for " + __originalMethod.Name + " because new method not found");
				return true;
			}

			object[] args = (object[])__args.Clone();

			if (!__originalMethod.IsStatic)
			{
				//Log.Message("Static, prepending " + __instance);
				args = args.Prepend(__instance).ToArray();
			}
			/*
			string message = "Attempting to call " + __newMethod.Name + " with args: ";
			foreach (object arg in args)
			{
				message += arg.ToString();
				message += ", ";
			}
			Log.Message(message);
			*/

			__result = newMethod.Invoke(null, args);
			return false;

		}


		[HarmonyPatch()]
		class Seek_Patch_Void
		{
			static Type[] targetTypes = new Type[]
			{
			typeof(RimWorld.PawnApparelGenerator)
			};

			static IEnumerable<MethodBase> TargetMethods()
			{

				foreach (Type type in targetTypes)
				{
					Log.Message("Attempting to patch type " + type.Name);
					MethodBase newMethod;
					foreach (MethodBase methodBase in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic
						| BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
					{
						//leave non-void methods for first patch
						if (!(methodBase is MethodInfo && ((MethodInfo)methodBase).ReturnType == typeof(void)))
							continue;

						newMethod = AccessTools.TypeByName("Toddlers." + type.Name).GetMethod(methodBase.Name,
							BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
						//Log.Message("newMethod: " + newMethod);
						if (newMethod != null)
						{
							Log.Message("Attempting to patch method " + methodBase.Name);
							yield return methodBase;
						}
						else
						{
							Log.Message("No newMethod provided, not patching method " + methodBase.Name);
						}
					}
				}
			}

			static bool Prepare()
			{
				if (TargetMethods().EnumerableNullOrEmpty())
					return false;
				else return true;
			}

			static bool Prefix(object[] __args, MethodBase __originalMethod, object __instance)
			{
				Log.Message("Firing Prefix for " + __originalMethod.Name);
				string methodName = __originalMethod.Name;
				string className = "Toddlers." + __originalMethod.DeclaringType.Name;
				MethodBase newMethod = AccessTools.TypeByName(className).GetMethod(methodName,
					BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

				if (newMethod == null)
				{
					Log.Message("Averting prefix for " + __originalMethod.Name + " because new method not found");
					return true;
				}

				object[] args = (object[])__args.Clone();

				if (!__originalMethod.IsStatic)
				{
					//Log.Message("Static, prepending " + __instance);
					args = args.Prepend(__instance).ToArray();
				}
				/*
				string message = "Attempting to call " + __newMethod.Name + " with args: ";
				foreach (object arg in args)
				{
					message += arg.ToString();
					message += ", ";
				}
				Log.Message(message);
				*/

				newMethod.Invoke(null, args);
				return false;

			}
		}
	}

	//put copies here
	//make classes static
	//give non-static methods a new first argument: object instance
	//make those methods static
	//wherever "this" would be used, replace with instance

	public static class PawnApparelGenerator
	{
		private static List<ThingStuffPair> allApparelPairs;

		private static float freeWarmParkaMaxPrice;

		private static float freeWarmHatMaxPrice;

		private static float freeToxicEnvironmentResistanceApparelMaxPrice;

		private static PossibleApparelSet workingSet;

		private static StringBuilder debugSb;

		private const int PracticallyInfinity = 9999999;

		private const float MinMapPollutionForFreeToxicResistanceApparel = 0.05f;

		private static List<ThingStuffPair> tmpApparelCandidates;

		private static List<ThingStuffPair> usableApparel;

		private class PossibleApparelSet
		{
			private List<ThingStuffPair> aps = new List<ThingStuffPair>();

			private HashSet<ApparelUtility.LayerGroupPair> lgps = new HashSet<ApparelUtility.LayerGroupPair>();

			private BodyDef body;

			private ThingDef raceDef;

			private Pawn pawn;

			private const float StartingMinTemperature = 12f;

			private const float TargetMinTemperature = -40f;

			private const float StartingMaxTemperature = 32f;

			private const float TargetMaxTemperature = 30f;

			private const float MinToxicEnvironmentResistanceForFreeApparel = 0.25f;

			private const float MinToxicEnvironmentResistanceImprovement = 0.15f;

			private static readonly SimpleCurve ToxicEnvironmentResistanceOverPollutionCurve = new SimpleCurve
			{
				new CurvePoint(0f, 0f),
				new CurvePoint(0.5f, 0.5f),
				new CurvePoint(1f, 0.85f)
			};

			public int Count => aps.Count;

			public float TotalPrice => aps.Sum((ThingStuffPair pa) => pa.Price);

			public float TotalInsulationCold => aps.Sum((ThingStuffPair a) => a.InsulationCold);

			public List<ThingStuffPair> ApparelsForReading => aps;

			public void Reset(BodyDef body, ThingDef raceDef)
			{
				aps.Clear();
				lgps.Clear();
				this.body = body;
				this.raceDef = raceDef;
				pawn = null;
			}

			public void Reset(Pawn pawn)
			{
				aps.Clear();
				lgps.Clear();
				this.pawn = pawn;
				body = pawn?.RaceProps?.body;
				raceDef = pawn?.def;
			}

			public void Add(ThingStuffPair pair)
			{
				aps.Add(pair);
				for (int i = 0; i < pair.thing.apparel.layers.Count; i++)
				{
					ApparelLayerDef layer = pair.thing.apparel.layers[i];
					BodyPartGroupDef[] interferingBodyPartGroups = pair.thing.apparel.GetInterferingBodyPartGroups(body);
					for (int j = 0; j < interferingBodyPartGroups.Length; j++)
					{
						lgps.Add(new ApparelUtility.LayerGroupPair(layer, interferingBodyPartGroups[j]));
					}
				}
			}

			public bool PairOverlapsAnything(ThingStuffPair pair)
			{
				if (!lgps.Any())
				{
					return false;
				}
				for (int i = 0; i < pair.thing.apparel.layers.Count; i++)
				{
					ApparelLayerDef layer = pair.thing.apparel.layers[i];
					BodyPartGroupDef[] interferingBodyPartGroups = pair.thing.apparel.GetInterferingBodyPartGroups(body);
					for (int j = 0; j < interferingBodyPartGroups.Length; j++)
					{
						if (lgps.Contains(new ApparelUtility.LayerGroupPair(layer, interferingBodyPartGroups[j])))
						{
							return true;
						}
					}
				}
				return false;
			}

			public bool CoatButNoShirt()
			{
				bool flag = false;
				bool flag2 = false;
				for (int i = 0; i < aps.Count; i++)
				{
					if (!aps[i].thing.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso))
					{
						continue;
					}
					for (int j = 0; j < aps[i].thing.apparel.layers.Count; j++)
					{
						ApparelLayerDef apparelLayerDef = aps[i].thing.apparel.layers[j];
						if (apparelLayerDef == ApparelLayerDefOf.OnSkin)
						{
							flag2 = true;
						}
						if (apparelLayerDef == ApparelLayerDefOf.Shell || apparelLayerDef == ApparelLayerDefOf.Middle)
						{
							flag = true;
						}
					}
				}
				if (flag)
				{
					return !flag2;
				}
				return false;
			}

			public bool Covers(BodyPartGroupDef bp)
			{
				for (int i = 0; i < aps.Count; i++)
				{
					if ((bp != BodyPartGroupDefOf.Legs || !aps[i].thing.apparel.legsNakedUnlessCoveredBySomethingElse) && aps[i].thing.apparel.bodyPartGroups.Contains(bp))
					{
						return true;
					}
				}
				return false;
			}

			public bool IsNaked(Gender gender)
			{
				switch (gender)
				{
					case Gender.Male:
						return !Covers(BodyPartGroupDefOf.Legs);
					case Gender.Female:
						if (Covers(BodyPartGroupDefOf.Legs))
						{
							return !Covers(BodyPartGroupDefOf.Torso);
						}
						return true;
					case Gender.None:
						return false;
					default:
						return false;
				}
			}

			public bool SatisfiesNeededWarmth(NeededWarmth warmth, bool mustBeSafe = false, float mapTemperature = 21f)
			{
				if (warmth == NeededWarmth.Any)
				{
					return true;
				}
				if (mustBeSafe && !SafeTemperature(mapTemperature))
				{
					return false;
				}
				switch (warmth)
				{
					case NeededWarmth.Cool:
						return aps.Sum((ThingStuffPair a) => a.InsulationHeat) >= -2f;
					case NeededWarmth.Warm:
						return aps.Sum((ThingStuffPair a) => a.InsulationCold) >= 52f;
					default:
						throw new NotImplementedException();
				};
			}

			private bool SafeTemperature(float temp)
			{
				if (pawn != null)
				{
					return pawn.SafeTemperatureRange(aps).Includes(temp);
				}
				return GenTemperature.SafeTemperatureRange(raceDef, aps).Includes(temp);
			}

			public void AddFreeWarmthAsNeeded(NeededWarmth warmth, float mapTemperature, Pawn pawn)
			{
				if (warmth == NeededWarmth.Any || warmth == NeededWarmth.Cool)
				{
					return;
				}
				if (DebugViewSettings.logApparelGeneration)
				{
					debugSb.AppendLine();
					debugSb.AppendLine("Trying to give free warm layer.");
				}
				for (int i = 0; i < 3; i++)
				{
					if (!SatisfiesNeededWarmth(warmth, mustBeSafe: true, mapTemperature))
					{
						if (DebugViewSettings.logApparelGeneration)
						{
							debugSb.AppendLine("Checking to give free torso-cover at max price " + freeWarmParkaMaxPrice);
						}
						Predicate<ThingStuffPair> parkaPairValidator = delegate (ThingStuffPair pa)
						{
							if (pa.Price > freeWarmParkaMaxPrice)
							{
								return false;
							}
							if (pa.InsulationCold <= 0f)
							{
								return false;
							}
							if (!pa.thing.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso))
							{
								return false;
							}
							if (!pa.thing.apparel.canBeGeneratedToSatisfyWarmth)
							{
								return false;
							}
							if (!pa.thing.apparel.CorrectAgeForWearing(pawn))
							{
								return false;
							}
							return (!(GetReplacedInsulationCold(pa) >= pa.InsulationCold)) ? true : false;
						};
						for (int j = 0; j < 2; j++)
						{
							ThingStuffPair candidate;
							if (j == 0)
							{
								if (!allApparelPairs.Where((ThingStuffPair pa) => parkaPairValidator(pa) && pa.InsulationCold < 40f).TryRandomElementByWeight((ThingStuffPair pa) => pa.Commonality / (pa.Price * pa.Price), out candidate))
								{
									continue;
								}
							}
							else if (!allApparelPairs.Where((ThingStuffPair pa) => parkaPairValidator(pa)).TryMaxBy((ThingStuffPair x) => x.InsulationCold - GetReplacedInsulationCold(x), out candidate))
							{
								continue;
							}
							if (DebugViewSettings.logApparelGeneration)
							{
								debugSb.AppendLine(string.Concat("Giving free torso-cover: ", candidate, " insulation=", candidate.InsulationCold));
								foreach (ThingStuffPair item in aps.Where((ThingStuffPair a) => !ApparelUtility.CanWearTogether(a.thing, candidate.thing, body)))
								{
									debugSb.AppendLine("    -replaces " + item.ToString() + " InsulationCold=" + item.InsulationCold);
								}
							}
							aps.RemoveAll((ThingStuffPair pa) => !ApparelUtility.CanWearTogether(pa.thing, candidate.thing, body));
							aps.Add(candidate);
							break;
						}
					}
					if (SafeTemperature(mapTemperature))
					{
						break;
					}
				}
				if (!SatisfiesNeededWarmth(warmth, mustBeSafe: true, mapTemperature))
				{
					if (DebugViewSettings.logApparelGeneration)
					{
						debugSb.AppendLine("Checking to give free hat at max price " + freeWarmHatMaxPrice);
					}
					Predicate<ThingStuffPair> hatPairValidator = delegate (ThingStuffPair pa)
					{
						if (pa.Price > freeWarmHatMaxPrice)
						{
							return false;
						}
						if (pa.InsulationCold < 7f)
						{
							return false;
						}
						if (!pa.thing.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.FullHead) && !pa.thing.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.UpperHead))
						{
							return false;
						}
						return (!(GetReplacedInsulationCold(pa) >= pa.InsulationCold)) ? true : false;
					};
					if (allApparelPairs.Where((ThingStuffPair pa) => hatPairValidator(pa)).TryRandomElementByWeight((ThingStuffPair pa) => pa.Commonality / (pa.Price * pa.Price), out var hatPair))
					{
						if (DebugViewSettings.logApparelGeneration)
						{
							debugSb.AppendLine(string.Concat("Giving free hat: ", hatPair, " insulation=", hatPair.InsulationCold));
							foreach (ThingStuffPair item2 in aps.Where((ThingStuffPair a) => !ApparelUtility.CanWearTogether(a.thing, hatPair.thing, body)))
							{
								debugSb.AppendLine("    -replaces " + item2.ToString() + " InsulationCold=" + item2.InsulationCold);
							}
						}
						aps.RemoveAll((ThingStuffPair pa) => !ApparelUtility.CanWearTogether(pa.thing, hatPair.thing, body));
						aps.Add(hatPair);
					}
				}
				if (DebugViewSettings.logApparelGeneration)
				{
					debugSb.AppendLine("New TotalInsulationCold: " + TotalInsulationCold);
				}
			}

			public bool SatisfiesNeededToxicEnvironmentResistance(float pollution)
			{
				if (pollution <= 0f)
				{
					return true;
				}
				return aps.Sum((ThingStuffPair ap) => ap.ToxicEnvironmentResistance) >= ToxicEnvironmentResistanceOverPollutionCurve.Evaluate(pollution);
			}

			public void AddFreeToxicEnvironmentResistanceAsNeeded(float pollution, Func<ThingStuffPair, bool> extraValidator = null)
			{
				Predicate<ThingStuffPair> pollutionApparelValidator = delegate (ThingStuffPair pa)
				{
					if (!pa.thing.apparel.canBeGeneratedToSatisfyToxicEnvironmentResistance)
					{
						return false;
					}
					if (pa.ToxicEnvironmentResistance <= 0.25f)
					{
						return false;
					}
					if (pa.Price > freeToxicEnvironmentResistanceApparelMaxPrice)
					{
						return false;
					}
					if (extraValidator != null && !extraValidator(pa))
					{
						return false;
					}
					for (int j = 0; j < aps.Count; j++)
					{
						if (!ApparelUtility.CanWearTogether(aps[j].thing, pa.thing, body) && aps[j].ToxicEnvironmentResistance >= 0.25f && Mathf.Abs(aps[j].ToxicEnvironmentResistance - pa.ToxicEnvironmentResistance) <= 0.15f)
						{
							return false;
						}
					}
					return true;
				};
				for (int i = 0; i < 5; i++)
				{
					if (SatisfiesNeededToxicEnvironmentResistance(pollution))
					{
						break;
					}
					if (!allApparelPairs.Where((ThingStuffPair pa) => pollutionApparelValidator(pa)).TryRandomElementByWeight((ThingStuffPair pa) => pa.Commonality / (pa.Price * pa.Price), out var pollutionPair))
					{
						continue;
					}
					if (DebugViewSettings.logApparelGeneration)
					{
						debugSb.AppendLine(string.Concat("Giving free toxic environment resistance: ", pollutionPair, " ToxicEnvironmentResistance=", pollutionPair.ToxicEnvironmentResistance));
						foreach (ThingStuffPair item in aps.Where((ThingStuffPair a) => !ApparelUtility.CanWearTogether(a.thing, pollutionPair.thing, body)))
						{
							debugSb.AppendLine("    -replaces " + item.ToString() + " ToxicEnvironmentResistance=" + item.ToxicEnvironmentResistance);
						}
					}
					aps.RemoveAll((ThingStuffPair pa) => !ApparelUtility.CanWearTogether(pa.thing, pollutionPair.thing, body));
					aps.Add(pollutionPair);
				}
				if (DebugViewSettings.logApparelGeneration)
				{
					debugSb.AppendLine("New ToxicEnvironmentResistance: " + aps.Sum((ThingStuffPair a) => a.ToxicEnvironmentResistance));
				}
			}

			public void GiveToPawn(Pawn pawn)
			{
				for (int i = 0; i < aps.Count; i++)
				{
					Apparel apparel = (Apparel)ThingMaker.MakeThing(aps[i].thing, aps[i].stuff);
					PawnGenerator.PostProcessGeneratedGear(apparel, pawn);
					if (ApparelUtility.HasPartsToWear(pawn, apparel.def))
					{
						pawn.apparel.Wear(apparel, dropReplacedApparel: false);
					}
				}
				for (int j = 0; j < aps.Count; j++)
				{
					for (int k = 0; k < aps.Count; k++)
					{
						if (j != k && !ApparelUtility.CanWearTogether(aps[j].thing, aps[k].thing, pawn.RaceProps.body))
						{
							Log.Error(string.Concat(pawn, " generated with apparel that cannot be worn together: ", aps[j], ", ", aps[k]));
							return;
						}
					}
				}
			}

			public float GetReplacedInsulationCold(ThingStuffPair newAp)
			{
				float num = 0f;
				for (int i = 0; i < aps.Count; i++)
				{
					if (!ApparelUtility.CanWearTogether(aps[i].thing, newAp.thing, body))
					{
						num += aps[i].InsulationCold;
					}
				}
				return num;
			}

			public override string ToString()
			{
				string text = "[";
				for (int i = 0; i < aps.Count; i++)
				{
					text = text + aps[i].ToString() + ", ";
				}
				return text + "]";
			}
		}

		public static void GenerateStartingApparelFor(Pawn pawn, PawnGenerationRequest request)
		{
			Log.Message("Firing GenerateStartingApparelFor");
			if (!pawn.RaceProps.ToolUser || !pawn.RaceProps.IsFlesh)
			{
				return;
			}
			pawn.apparel.DestroyAll();
			pawn.outfits?.forcedHandler?.Reset();
			float randomInRange = pawn.kindDef.apparelMoney.RandomInRange;
			MethodInfo method_neededWarmth = typeof(PawnApparelGenerator).GetMethod("ApparelWarmthNeededNow", BindingFlags.Static | BindingFlags.NonPublic);
			object[] parms = new object[] { pawn, request, null };
			NeededWarmth neededWarmth = (NeededWarmth)method_neededWarmth.Invoke(null, parms);
			float mapTemperature = (float)parms[2];
			bool allowHeadgear = Rand.Value < pawn.kindDef.apparelAllowHeadgearChance;
			MethodInfo method_toxicEnvironment = typeof(PawnApparelGenerator).GetMethod("ApparelToxicEnvironmentToAddress", BindingFlags.Static | BindingFlags.NonPublic);
			float num = (float)method_toxicEnvironment.Invoke(null, new object[] { pawn, request });
			if (DebugViewSettings.logApparelGeneration)
			{
				debugSb = new StringBuilder();
				debugSb.AppendLine("Generating apparel for " + pawn);
				debugSb.AppendLine("Money: " + randomInRange.ToString("F0"));
				debugSb.AppendLine("Needed warmth: " + neededWarmth);
				debugSb.AppendLine("Needed toxic environment resistance: " + num);
				debugSb.AppendLine("Headgear allowed: " + allowHeadgear);
			}
			int @int = Rand.Int;
			MethodInfo method_canUsePair = typeof(PawnApparelGenerator).GetMethod("CanUsePair", BindingFlags.Static | BindingFlags.NonPublic);
			for (int i = 0; i < allApparelPairs.Count; i++)
			{
				ThingStuffPair thingStuffPair = allApparelPairs[i];

				if ((bool)method_canUsePair.Invoke(null, new object[] { thingStuffPair, pawn, randomInRange, allowHeadgear, @int }))
				{
					tmpApparelCandidates.Add(thingStuffPair);
				}
			}
			MethodInfo method_generateWorking = typeof(PawnApparelGenerator).GetMethod("GenerateWorkingPossibleApparelSetFor", BindingFlags.Static | BindingFlags.NonPublic);
			if (randomInRange < 0.001f)
			{
				method_generateWorking.Invoke(null, new object[] { pawn, randomInRange, tmpApparelCandidates });
			}
			else
			{
				int num2 = 0;
				while (true)
				{
					method_generateWorking.Invoke(null, new object[] { pawn, randomInRange, tmpApparelCandidates });
					if (DebugViewSettings.logApparelGeneration)
					{
						debugSb.Append(num2.ToString().PadRight(5) + "Trying: " + workingSet.ToString());
					}
					if (num2 < 10 && Rand.Value < 0.85f && randomInRange < 9999999f)
					{
						float num3 = Rand.Range(0.45f, 0.8f);
						float totalPrice = workingSet.TotalPrice;
						if (totalPrice < randomInRange * num3)
						{
							if (DebugViewSettings.logApparelGeneration)
							{
								debugSb.AppendLine(" -- Failed: Spent $" + totalPrice.ToString("F0") + ", < " + (num3 * 100f).ToString("F0") + "% of money.");
							}
							goto IL_045c;
						}
					}
					if (num2 < 20 && Rand.Value < 0.97f && !workingSet.Covers(BodyPartGroupDefOf.Torso))
					{
						if (DebugViewSettings.logApparelGeneration)
						{
							debugSb.AppendLine(" -- Failed: Does not cover torso.");
						}
					}
					else if (num2 < 30 && Rand.Value < 0.8f && workingSet.CoatButNoShirt())
					{
						if (DebugViewSettings.logApparelGeneration)
						{
							debugSb.AppendLine(" -- Failed: Coat but no shirt.");
						}
					}
					else
					{
						if (num2 < 50)
						{
							bool mustBeSafe = num2 < 17;
							if (!workingSet.SatisfiesNeededWarmth(neededWarmth, mustBeSafe, mapTemperature))
							{
								if (DebugViewSettings.logApparelGeneration)
								{
									debugSb.AppendLine(" -- Failed: Wrong warmth.");
								}
								goto IL_045c;
							}
						}
						if (ModsConfig.BiotechActive && num2 < 10 && !workingSet.SatisfiesNeededToxicEnvironmentResistance(num))
						{
							if (DebugViewSettings.logApparelGeneration)
							{
								debugSb.AppendLine(" -- Failed: Wrong toxic environment resistance.");
							}
						}
						else
						{
							if (num2 >= 80 || !workingSet.IsNaked(pawn.gender))
							{
								break;
							}
							if (DebugViewSettings.logApparelGeneration)
							{
								debugSb.AppendLine(" -- Failed: Naked.");
							}
						}
					}
					goto IL_045c;
				IL_045c:
					num2++;
				}
				if (DebugViewSettings.logApparelGeneration)
				{
					debugSb.Append(" -- Approved! Total price: $" + workingSet.TotalPrice.ToString("F0") + ", TotalInsulationCold: " + workingSet.TotalInsulationCold);
				}
			}
			if ((!pawn.kindDef.apparelIgnoreSeasons || request.ForceAddFreeWarmLayerIfNeeded) && !workingSet.SatisfiesNeededWarmth(neededWarmth, mustBeSafe: true, mapTemperature))
			{
				workingSet.AddFreeWarmthAsNeeded(neededWarmth, mapTemperature, pawn);
			}
			if (ModsConfig.BiotechActive && !pawn.kindDef.apparelIgnorePollution && num > 0.05f && !workingSet.SatisfiesNeededToxicEnvironmentResistance(num))
			{
				workingSet.AddFreeToxicEnvironmentResistanceAsNeeded(num, delegate (ThingStuffPair pa)
				{
					if (!pa.thing.apparel.CorrectAgeForWearing(pawn))
					{
						return false;
					}
					if (pawn.kindDef.apparelIgnoreSeasons && !request.ForceAddFreeWarmLayerIfNeeded)
					{
						return true;
					}
					return (!(workingSet.GetReplacedInsulationCold(pa) > pa.InsulationCold)) ? true : false;
				});
			}
			if (DebugViewSettings.logApparelGeneration)
			{
				Log.Message(debugSb.ToString());
			}
			workingSet.GiveToPawn(pawn);
			workingSet.Reset(null, null);
			MethodInfo method_postProcess = typeof(PawnApparelGenerator).GetMethod("PostProcessApparel", BindingFlags.Static | BindingFlags.NonPublic);
			foreach (Apparel item in pawn.apparel.WornApparel)
			{
				method_postProcess.Invoke(null, new object[] { item, pawn });
				CompBiocodable compBiocodable = item.TryGetComp<CompBiocodable>();
				if (compBiocodable != null && !compBiocodable.Biocoded && Rand.Chance(request.BiocodeApparelChance))
				{
					compBiocodable.CodeFor(pawn);
				}
			}
		}
	}

}