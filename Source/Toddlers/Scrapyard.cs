using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//code that did not work or was otherwise removed
//here so that I can reuse parts of it in future







public partial class AlienRace
{
	public bool bodyAccountedFor_Baby = false;
	public bool bodyAccountedFor_Child = false;
	public BodyTypeDef bodyType_Baby = null;
	public BodyTypeDef bodyType_Child = null;


	public object bodyGraphic_obj;

	public void InitBodyTypes()
	{
		Log.Message("Starting InitBodyTypes");
		//if both child- and baby-specific bodies are defined using the HAR ageGraphics format
		//then we don't need to worry about bodyTypes for them
		if (bodyAccountedFor_Baby && bodyAccountedFor_Child) return;

		object bodyTypes_obj = alienPartGenerator.GetType().GetField("bodyTypes", BindingFlags.Public | BindingFlags.Instance).GetValue(alienPartGenerator);
		Log.Message("bodyTypes_obj: " + (bodyTypes_obj as IEnumerable).ToStringSafeEnumerable());

		List<BodyTypeDef> bodyTypes = bodyTypes_obj as List<BodyTypeDef>;

		foreach (BodyTypeDef bodyTypeDef in bodyTypes)
		{
			if (bodyTypeDef == BodyTypeDefOf.Baby) bodyType_Baby = BodyTypeDefOf.Baby;
			else if (bodyTypeDef.defName.Contains("baby") || bodyTypeDef.defName.Contains("Baby")) bodyType_Baby = bodyTypeDef;

			if (bodyTypeDef == BodyTypeDefOf.Child) bodyType_Child = BodyTypeDefOf.Child;
			else if (bodyTypeDef.defName.Contains("child") || bodyTypeDef.defName.Contains("Child")) bodyType_Child = bodyTypeDef;

			if (bodyType_Baby != null && bodyType_Child != null) break;
		}
		Log.Message("Body type search complete, bodyType_Baby: " + bodyType_Baby + ", bodyType_Child: " + bodyType_Child);

		//List<BodyTypeDef> vanillaBodies = new List<BodyTypeDef> { BodyTypeDefOf.Fat, BodyTypeDefOf.Female, BodyTypeDefOf.Hulk, BodyTypeDefOf.Male, BodyTypeDefOf.Thin};
		//if the race has not been allowed the baby/child body types, add those in
		if (bodyType_Baby == null)
		{
			bodyTypes.Add(BodyTypeDefOf.Baby);
			bodyType_Baby = BodyTypeDefOf.Baby;
		}
		if (bodyType_Child == null)
		{
			bodyTypes.Add(BodyTypeDefOf.Child);
			bodyType_Child = BodyTypeDefOf.Child;
		}
		Log.Message("new bodyTypes_obj: " + (bodyTypes_obj as IEnumerable).ToStringSafeEnumerable());

		//Note: bodyGraphic_obj is identified earlier in InitGraphicField when it loops through graphicPaths
		if (bodyGraphic_obj == null || !Patch_HAR.class_AbstractExtendedGraphic.IsAssignableFrom(bodyGraphic_obj.GetType()))
		{
			Log.Error("Toddlers HAR Patch failed to find extended body graphic for race: " + def.defName);
			return;
		}

		string path = (string)bodyGraphic_obj.GetType().GetField("path").GetValue(bodyGraphic_obj);
		Log.Message("path: " + path);

		//nested loop to find all nested bodyTypeGraphics objects that will need updating
		List<object> bodyTypeGraphicses = new List<object>();

		FindNestedBodyTypeVariants(bodyGraphic_obj, ref bodyTypeGraphicses);
		Log.Message("bodyTypeGraphicses: " + bodyTypeGraphicses.ToStringSafeEnumerable());

		foreach (object bodyTypeGraphics_obj in bodyTypeGraphicses)
		{
			Log.Message("bodyTypeGraphics_obj: " + bodyTypeGraphics_obj);
			IEnumerable bodyTypeGraphics_ienum = bodyTypeGraphics_obj as IEnumerable;
			Log.Message("bodyTypeGraphics_ienum: " + bodyTypeGraphics_ienum.ToStringSafeEnumerable());

			//if there aren't any graphics listed per body type, skip over this
			if (bodyTypeGraphics_ienum == null || bodyTypeGraphics_ienum.EnumerableCount() <= 0) continue;

			//establish whether there are already working graphics for baby/child
			object graphicBaby_obj = null;
			object graphicChild_obj = null;

			Log.Message("Looping through bodyTypeGraphics, first pass");
			foreach (object bodyTypeGraphic in bodyTypeGraphics_ienum)
			{
				BodyTypeDef bodyTypeDef = (BodyTypeDef)Patch_HAR.field_BodyTypeGraphic_bodyType.GetValue(bodyTypeGraphic);
				string typePath = (string)Patch_HAR.class_BodyTypeGraphic.GetMethod("GetPath", new Type[] { }).Invoke(bodyTypeGraphic, new object[] { });
				Log.Message("bodyTypeDef: " + bodyTypeDef + ", typePath: " + typePath);

				//if we've found the entry for the baby or child body type
				if (!bodyAccountedFor_Baby && bodyTypeDef == bodyType_Baby)
				{
					graphicBaby_obj = bodyTypeGraphic;
					if (!CheckTextures(path))
					{
						//Log.Message("Found broken graphic entry at " + path + " for baby");
						continue;
					}
					else
					{
						//Log.Message("Found a graphic for bodyType_Baby, no need to make a new one.");
						bodyAccountedFor_Baby = true;
					}
				}
				if (!bodyAccountedFor_Child && bodyTypeDef == bodyType_Child)
				{
					graphicChild_obj = bodyTypeGraphic;
					if (!CheckTextures(path))
					{
						//Log.Message("Found broken graphic entry at " + path + " for child");
						continue;
					}
					else
					{
						//Log.Message("Found a graphic for bodyType_Child, no need to make a new one.");
						bodyAccountedFor_Child = true;
					}
				}
				//if we've found working graphics for both babies and children, we're done here
				if (bodyAccountedFor_Baby && bodyAccountedFor_Child) continue;
			}
			Log.Message("graphicBaby_obj: " + graphicBaby_obj + ", graphicChild_obj: " + graphicChild_obj);


			//if we haven't found an existing graphic for babies/children
			//make a new one
			if (!bodyAccountedFor_Baby && graphicBaby_obj == null)
			{
				graphicBaby_obj = Activator.CreateInstance(Patch_HAR.class_BodyTypeGraphic);
				bodyTypeGraphics_obj.GetType().GetMethod("Add", new Type[] { Patch_HAR.class_BodyTypeGraphic })
						.Invoke(bodyTypeGraphics_obj, new object[] { graphicBaby_obj });
				Patch_HAR.field_BodyTypeGraphic_bodyType.SetValue(graphicBaby_obj, bodyType_Baby);
			}
			if (!bodyAccountedFor_Child && graphicChild_obj == null)
			{
				graphicChild_obj = Activator.CreateInstance(Patch_HAR.class_BodyTypeGraphic);
				bodyTypeGraphics_obj.GetType().GetMethod("Add", new Type[] { Patch_HAR.class_BodyTypeGraphic })
						.Invoke(bodyTypeGraphics_obj, new object[] { graphicChild_obj });
				Patch_HAR.field_BodyTypeGraphic_bodyType.SetValue(graphicChild_obj, bodyType_Child);
			}

			//if the race uses vanilla textures
			//then we can very easily set the baby/child graphics to the correct ones
			//Log.Message("path: " + path + ", vanilla: " + VANILLA_BODY_PATH + ", eq: " + (path == VANILLA_BODY_PATH));
			if (path == VANILLA_BODY_PATH)
			{
				Log.Message("path == VANILLA_BODY_PATH");
				if (!bodyAccountedFor_Baby)
				{
					Patch_HAR.field_BodyTypeGraphic_path.SetValue(graphicBaby_obj, VANILLA_BODY_PATH + "Naked_Child");
				}
				if (!bodyAccountedFor_Child)
				{
					Patch_HAR.field_BodyTypeGraphic_path.SetValue(graphicChild_obj, VANILLA_BODY_PATH + "Naked_Child");
				}
				continue;
			}

			//if we're not using vanilla textures it gets a little more complicated
			//loop through the available body type graphics defined in this subgraphic for the race
			//and try to pick one that will be the least strange as a baby/child body
			int priority = 0;
			int bestPriority_Baby = 0;
			object bestGraphic_Baby = null;
			int bestPriority_Child = 0;
			object bestGraphic_Child = null;

			//Log.Message("Looping through bodyTypeGraphics, second pass");
			foreach (object bodyTypeGraphic in bodyTypeGraphics_ienum)
			{
				BodyTypeDef bodyTypeDef = (BodyTypeDef)Patch_HAR.field_BodyTypeGraphic_bodyType.GetValue(bodyTypeGraphic);
				path = (string)Patch_HAR.class_BodyTypeGraphic.GetMethod("GetPath", new Type[] { }).Invoke(bodyTypeGraphic, new object[] { });
				//Log.Message("bodyTypeDef: " + bodyTypeDef + ", path: " + path);

				//if the graphic path doesn't resolve, can't use this graphic for anything
				if (!CheckTextures(path))
				{
					//Log.Message("Found no graphic at " + path + ", continuing");
					continue;
				}

				//if we're still looking for a good substitute for baby or child
				//consider the current graphic
				//and compare to the last best one we've found
				if (!bodyAccountedFor_Baby)
				{
					priority = BodyTypePriority_Baby(bodyTypeDef);
					if (priority > bestPriority_Baby)
					{
						bestPriority_Baby = priority;
						bestGraphic_Baby = bodyTypeGraphic;
					}
				}
				if (!bodyAccountedFor_Child)
				{
					priority = BodyTypePriority_Child(bodyTypeDef);
					if (priority > bestPriority_Child)
					{
						bestPriority_Child = priority;
						bestGraphic_Child = bodyTypeGraphic;
					}
				}
			}
			//Log.Message("bestGraphic_Baby: " + bestGraphic_Baby + ", priority: " + bestPriority_Baby
			//    + ", bestGraphic_Child: " + bestGraphic_Child + ", priority: " + bestPriority_Child);

			if (!bodyAccountedFor_Baby)
			{
				//if we've identified a candidate from the race-specific graphics
				//use that
				if (bestGraphic_Baby != null)
				{
					//Log.Message("Copying from bestGraphic");
					//copy all fields
					foreach (FieldInfo fieldInfo in bestGraphic_Baby.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
					{
						fieldInfo.SetValue(graphicBaby_obj, fieldInfo.GetValue(bestGraphic_Baby));
					}

					//then set bodyType (back) to what it should be
					Patch_HAR.field_BodyTypeGraphic_bodyType.SetValue(graphicBaby_obj, bodyType_Baby);
				}
				//if we haven't, default back to vanilla
				else
				{
					//Log.Message("Copying from vanilla");
					Patch_HAR.field_BodyTypeGraphic_bodyType.SetValue(graphicBaby_obj, bodyType_Baby);
					graphicBaby_obj.GetType().GetField("path").SetValue(graphicBaby_obj, VANILLA_BODY_PATH);
				}
			}

			if (!bodyAccountedFor_Child)
			{
				//if we've identified a candidate from the race-specific graphics
				//use that
				if (bestGraphic_Child != null)
				{
					//Log.Message("Copying from bestGraphic");
					//copy all fields
					foreach (FieldInfo fieldInfo in bestGraphic_Child.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
					{
						fieldInfo.SetValue(graphicChild_obj, fieldInfo.GetValue(bestGraphic_Child));
					}

					//then set bodyType (back) to what it should be
					Patch_HAR.field_BodyTypeGraphic_bodyType.SetValue(graphicChild_obj, bodyType_Child);
				}
				//if we haven't, default back to vanilla
				else
				{
					//Log.Message("Copying from vanilla");
					Patch_HAR.field_BodyTypeGraphic_bodyType.SetValue(graphicChild_obj, bodyType_Child);
					graphicChild_obj.GetType().GetField("path").SetValue(graphicChild_obj, VANILLA_BODY_PATH);
				}
			}

		}
	}

	public void FindNestedBodyTypeVariants(object graphic, ref List<object> collector)
	{
		Log.Message("FindNestedBodyTypeVariants, graphic: " + graphic);
		if (!Patch_HAR.class_AbstractExtendedGraphic.IsAssignableFrom(graphic.GetType())) return;

		IEnumerable<object> subgraphics = Patch_HAR.fields_subgraphics.Select<FieldInfo, object>(x => x.GetValue(graphic));
		Log.Message("fields_subgraphics: " + Patch_HAR.fields_subgraphics.ToStringSafeEnumerable());
		Log.Message("subgraphics: " + subgraphics.ToStringSafeEnumerable());
		if (subgraphics.EnumerableNullOrEmpty()) return;

		foreach (object subgraphic in subgraphics)
		{
			Log.Message("subgraphic: " + subgraphic + ", " + (subgraphic as IEnumerable).ToStringSafeEnumerable());
			//if the list is not empty we will need to iterate through it
			if (!((subgraphic as IEnumerable).EnumerableCount() == 0))
			{
				//if the type of the list is List<AlienPartGenerator.ExtendedBodytypeGraphic>
				//we want to "return" it as one of the objects we're looking for
				Type[] types = subgraphic.GetType().GetGenericArguments();
				if (types.Length == 1 && types[0] == Patch_HAR.class_BodyTypeGraphic)
				{
					collector.Add(subgraphic);
				}
				FindNestedBodyTypeVariants(subgraphic, ref collector);
			}
		}
	}

	

	public int BodyTypePriority_Baby(BodyTypeDef def)
	{
		if (def == BodyTypeDefOf.Baby || def.defName.Contains("Baby") || def.defName.Contains("baby")) return 100;
		if (def == BodyTypeDefOf.Child || def.defName.Contains("Child") || def.defName.Contains("child")) return 10;
		if (def == BodyTypeDefOf.Thin || def.defName.Contains("Thin") || def.defName.Contains("thin")) return 9;
		if (def.defName.Contains("Main") || def.defName.Contains("main")
			|| def.defName.Contains("Norm") || def.defName.Contains("norm")
			|| def.defName.Contains("Stand") || def.defName.Contains("stand")
			|| def.defName.Contains("Std") || def.defName.Contains("std")
			|| def.defName.Contains("Default") || def.defName.Contains("default")
			|| def.defName.Contains("Base") || def.defName.Contains("base")
			|| def.defName.Contains("Basic") || def.defName.Contains("basic")
			) return 8;
		if (def == BodyTypeDefOf.Male || def.defName.Contains("Male") || def.defName.Contains("male")) return 7;
		return 1;
	}
	public int BodyTypePriority_Child(BodyTypeDef def)
	{
		if (def == BodyTypeDefOf.Child || def.defName.Contains("Child") || def.defName.Contains("child")) return 100;
		if (def == BodyTypeDefOf.Baby || def.defName.Contains("Baby") || def.defName.Contains("baby")) return 10;
		if (def == BodyTypeDefOf.Thin || def.defName.Contains("Thin") || def.defName.Contains("thin")) return 9;
		if (def.defName.Contains("Main") || def.defName.Contains("main")
			|| def.defName.Contains("Norm") || def.defName.Contains("norm")
			|| def.defName.Contains("Stand") || def.defName.Contains("stand")
			|| def.defName.Contains("Std") || def.defName.Contains("std")
			|| def.defName.Contains("Default") || def.defName.Contains("default")
			|| def.defName.Contains("Base") || def.defName.Contains("base")
			|| def.defName.Contains("Basic") || def.defName.Contains("basic")
			) return 8;
		if (def == BodyTypeDefOf.Male || def.defName.Contains("Male") || def.defName.Contains("male")) return 7;
		return 1;
	}

}



public partial class AlienRace
{

	public Dictionary<object, BodyAddon> bodyAddons = new Dictionary<object, BodyAddon>();
	public Dictionary<string, string> babyAgeGraphics = new Dictionary<string, string>();
	public Dictionary<string, string> bodyAddonAgeGraphics = new Dictionary<string, string>();

	public void InitGraphicField(object graphic, string key, ref Dictionary<string, string> outputDict)
	{
		if (key == "body")
		{
			bodyGraphic_obj = graphic;
		}

		//if there are no age graphic variants we don't need to touch this graphic
		FieldInfo ageGraphics_field = graphic.GetType().GetField("ageGraphics", BindingFlags.Instance | BindingFlags.Public);
		if (ageGraphics_field == null) return;

		object ageGraphics_obj = ageGraphics_field.GetValue(graphic);
		IEnumerable ageGraphics_ienum = ageGraphics_obj as IEnumerable;
		if (ageGraphics_ienum.EnumerableCount() == 0) return;

		string path = "";
		//loop over the life stages registered in the ageGraphic
		foreach (object ageGraphic in ageGraphics_ienum)
		{
			path = (string)ageGraphic.GetType().GetMethod("GetPath", types: new Type[] { }).Invoke(ageGraphic, new object[] { });
			LifeStageDef lifeStageDef = (LifeStageDef)ageGraphic.GetType().GetField("age", BindingFlags.Instance | BindingFlags.Public).GetValue(ageGraphic);

			//if we've found a child-specific body graphic, make a note of it
			if (lifeStageDef == lifeStageChild.def && key == "body") bodyAccountedFor_Child = true;
			if (lifeStageDef == lifeStageBaby.def)
			{
				//if we've found a child-specific body graphic, make a note of it
				if (key == "body") bodyAccountedFor_Baby = true;
				//found the most relevant path: the one for baby
				break;
			}
		}
		//if we found no baby-specific path, we'll just make a note of the last path we looked at

		Log.Message("Final path for " + key + ": " + path);
		if (path.NullOrEmpty()) return;

		outputDict.Add(key, path);
	}

	public void InitGraphicFields()
	{
		Log.Message("Firing InitGraphicFields, def: " + def);

			if (Patch_HAR.class_ThingDef_AlienRace.IsAssignableFrom(def.GetType()))
			{
				if (Patch_HAR.class_GraphicPaths.IsAssignableFrom(graphicPaths.GetType()))
				{
					List<FieldInfo> graphicFields = (from field
													in graphicPaths.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)
													 where Patch_HAR.class_AbstractExtendedGraphic.IsAssignableFrom(field.FieldType)
													 select field).ToList();
					//Log.Message("graphicFields: " + graphicFields);
					foreach (FieldInfo field in graphicFields)
					{
						string fieldName = field.Name;
						object graphic = field.GetValue(graphicPaths);

						InitGraphicField(graphic, fieldName, ref babyAgeGraphics);

					}
				}
				//else Log.Message("Failed GraphicPaths.IsAssignableFrom(graphicPaths)");

				foreach (KeyValuePair<object, BodyAddon> kvp in bodyAddons)
				{
					object orig = kvp.Key;
					BodyAddon wrapper = kvp.Value;

					string name = wrapper.name;
					InitGraphicField(orig, name, ref bodyAddonAgeGraphics);

					//Log.Message("orig: " + orig.ToString());
				}
			}
		//else Log.Message("Failed ThingDef_AlienRace.IsAssignableFrom(def)");
	}

	public void UpdateAgeGraphics()
	{
		//Log.Message("Started UpdateAgeGraphics");
		//Log.Message("babyAgeGraphics: " + babyAgeGraphics + ", Count: " + babyAgeGraphics.Count);
		//Log.Message("bodyAddons: " + bodyAddons + ", Count: " + bodyAddons.Count);

		foreach (KeyValuePair<string, string> kvp in babyAgeGraphics)
		{
			Log.Message("UpdateAgeGraphics: " + kvp);

			string fieldName = kvp.Key;
			string path = kvp.Value;

			object graphic = graphicPaths.GetType().GetField(fieldName).GetValue(graphicPaths);

			FieldInfo ageGraphics_field = graphic.GetType().GetField("ageGraphics", BindingFlags.Instance | BindingFlags.Public);
			if (ageGraphics_field == null)
			{
				Log.Message("Graphic field " + fieldName + " has no ageGraphics");
				continue;
			}
			object ageGraphics_obj = ageGraphics_field.GetValue(graphic);

			object ageGraphic_toddler = Activator.CreateInstance(Patch_HAR.class_ExtendedAgeGraphic);
			ageGraphic_toddler.GetType().GetField("age").SetValue(ageGraphic_toddler, lifeStageToddler.def);
			ageGraphic_toddler.GetType().GetField("path").SetValue(ageGraphic_toddler, path);
			ageGraphic_toddler.GetType().GetField("variantCount").SetValue(ageGraphic_toddler, 1);

			ageGraphics_obj.GetType().GetMethod("Add").Invoke(ageGraphics_obj, new object[] { ageGraphic_toddler });

			/*
			Log.Message("ageGraphics_obj: " + ageGraphics_obj);
			IEnumerable ageGraphics_ienum = ageGraphics_obj as IEnumerable;
			foreach (object item in ageGraphics_ienum)
			{
				Log.Message("age: " + item.GetType().GetField("age").GetValue(item)
					+ ", path: " + item.GetType().GetField("path").GetValue(item)
					);
				;
			}
			*/
		}

		foreach (KeyValuePair<object, BodyAddon> kvp in bodyAddons as IEnumerable)
		{
			//Log.Message("UpdateAgeGraphics: " + kvp);

			object orig = kvp.Key;
			BodyAddon wrapper = kvp.Value;

			string name = wrapper.name;
			string path;

			//Log.Message("name: " + name);

			if (bodyAddonAgeGraphics.ContainsKey(name))
			{
				path = bodyAddonAgeGraphics[name];
				//Log.Message("path: " + path);

				//FieldInfo ageGraphics_field = bodyAddon.GetType().GetField("ageGraphics", BindingFlags.Instance | BindingFlags.Public);
				if (wrapper.ageGraphics == null)
				{
					Log.Message("BodyAddon " + wrapper.name + " has no ageGraphics");
					continue;
				}
				//object ageGraphics_obj = ageGraphics_field.GetValue(bodyAddon);

				object ageGraphic_toddler = Activator.CreateInstance(Patch_HAR.class_ExtendedAgeGraphic);
				ageGraphic_toddler.GetType().GetField("age").SetValue(ageGraphic_toddler, lifeStageToddler.def);
				ageGraphic_toddler.GetType().GetField("path").SetValue(ageGraphic_toddler, path);
				ageGraphic_toddler.GetType().GetField("variantCount").SetValue(ageGraphic_toddler, 1);

				wrapper.ageGraphics.GetType().GetMethod("Add").Invoke(wrapper.ageGraphics, new object[] { ageGraphic_toddler });

				//Log.Message("ageGraphics: " + wrapper.ageGraphics);
				/*
				IEnumerable ageGraphics_ienum = wrapper.ageGraphics as IEnumerable;

				foreach (object item in ageGraphics_ienum)
				{
					Log.Message("age: " + item.GetType().GetField("age").GetValue(item)
						+ ", path: " + item.GetType().GetField("path").GetValue(item)
						);
					;
				}
				*/
			}
		}

		//Log.Message("Finished UpdateAgeGraphics");
	}

	public void InitBodyAddons()
	{
		object bodyAddons_obj = alienPartGenerator.GetType().GetField("bodyAddons", BindingFlags.Public | BindingFlags.Instance).GetValue(alienPartGenerator);
		//Log.Message("bodyAddons_obj: " + bodyAddons_obj + ", Count: " + (bodyAddons_obj as IEnumerable).EnumerableCount());

		foreach (object bodyAddon_origType in bodyAddons_obj as IEnumerable)
		{
			//Log.Message("bodyAddon_origType: " + bodyAddon_origType + ", Type: " + bodyAddon_origType.GetType());
			BodyAddon bodyAddon = new BodyAddon(bodyAddon_origType);
			bodyAddons.Add(bodyAddon_origType, bodyAddon);
			Log.Message("Adding BodyAddon " + bodyAddon.name + " to list for " + def.defName);
		}
	}

}







class JobGiver_LeaveCrib : ThinkNode_JobGiver
{
	public override float GetPriority(Pawn pawn)
	{
		if (pawn.needs == null) return 0f;
		if (ToddlerUtility.IsCrawler(pawn)) return -99f;
		float priority = 1f;
		if (pawn.needs.food != null)
		{
			if (pawn.needs.food.CurLevelPercentage < pawn.needs.food.PercentageThreshUrgentlyHungry) priority += 9f;
			else if (pawn.needs.food.CurLevelPercentage < pawn.needs.food.PercentageThreshHungry) priority += 6f;
		}
		if (pawn.needs.play != null)
		{
			if (pawn.needs.play.CurLevel < 0.7f) priority += 5f;
		}
		return priority;
	}

	protected override Job TryGiveJob(Pawn pawn)
	{
		Building_Bed crib = pawn.CurrentBed();
		if (crib == null) return null;

		IntVec3 exitCell;
		if (!TryFindExitCell(pawn, out exitCell))
		{
			return null;
		}
		return JobMaker.MakeJob(Toddlers_DefOf.LeaveCrib, crib, exitCell);
	}

	private bool TryFindExitCell(Pawn pawn, out IntVec3 exitCell)
	{
		foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(pawn).InRandomOrder())
		{
			if (pawn.CanReserveAndReach(cell, PathEndMode.OnCell, Danger.None))
			{
				exitCell = cell;
				return true;
			}
		}
		exitCell = IntVec3.Invalid;
		return false;
	}
}



class JobDriver_GetIntoCrib : JobDriver
{
	public Building_Bed Bed => TargetA.Thing as Building_Bed;

	public virtual bool CanSleep => true;

	public virtual bool CanRest => true;

	public virtual bool LookForOtherJobs => true;

	public Vector3 fullVector;
	public int ticksRequired = 180;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		if (Bed != null && !pawn.Reserve(Bed, job, Bed.SleepingSlotsCount, 0, null, errorOnFailed))
		{
			return false;
		}
		return true;
	}

	public override Vector3 ForcedBodyOffset
	{
		get
		{
			if (this.CurToilString != "ClimbIn") return Vector3.zero;
			float percentDone = (float)(ticksRequired - ticksLeftThisToil) / (float)ticksRequired;
			Vector3 outVector = percentDone * fullVector;
			return outVector;
		}
	}
	protected override IEnumerable<Toil> MakeNewToils()
	{
		AddFailCondition(() => pawn.Downed || !(pawn.ParentHolder is Map));

		Building_Bed crib = TargetA.Thing as Building_Bed;
		AddFailCondition(() => crib.DestroyedOrNull() || !crib.Spawned || !pawn.CanReach(crib, PathEndMode.OnCell, Danger.Deadly));

		yield return Toils_Bed.ClaimBedIfNonMedical(TargetIndex.A);

		IntVec3 entryCell;
		RCellFinder.TryFindGoodAdjacentSpotToTouch(pawn, crib, out entryCell);
		AddFailCondition(() => entryCell == null || !pawn.CanReach(entryCell, PathEndMode.OnCell, Danger.Some));
		yield return Toils_Goto.GotoCell(entryCell, PathEndMode.OnCell);

		fullVector = crib.Position.ToVector3() - entryCell.ToVector3();

		Toil climbIn = ToilMaker.MakeToil("ClimbIn");
		climbIn.defaultCompleteMode = ToilCompleteMode.Delay;
		climbIn.defaultDuration = ticksRequired;
		climbIn.handlingFacing = true;
		climbIn.initAction = delegate
		{
		};
		climbIn.tickAction = delegate
		{
			this.pawn.rotationTracker.FaceCell(crib.Position);
		};
		climbIn.AddFinishAction(delegate
		{
			pawn.SetPositionDirect(crib.Position);
			this.pawn.jobs.posture = PawnPosture.LayingInBed;
			pawn.Drawer.tweener.ResetTweenedPosToRoot();
		});
		yield return climbIn;

		//yield return Toils_LayDown.LayDown(TargetIndex.A, true, true);
	}
}

class JobDriver_LeaveCrib : JobDriver
{
	public Vector3 fullVector;
	public int ticksRequired = 180;

	public override Vector3 ForcedBodyOffset
	{
		get
		{
			float percentDone = (float)(ticksRequired - ticksLeftThisToil) / (float)ticksRequired;
			Vector3 outVector = percentDone * fullVector;
			return outVector;
		}
	}

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return true;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		//Log.Message("Fired LeaveCrib MakeNewToils");

		Building_Bed crib = TargetA.Thing as Building_Bed;
		IntVec3 exitCell = TargetB.Cell;
		AddFailCondition(() => pawn.Downed || !(pawn.ParentHolder is Map));
		AddFailCondition(() => crib.DestroyedOrNull() || !crib.Spawned);
		AddFailCondition(() => !pawn.CanReach(exitCell, PathEndMode.OnCell, Danger.Deadly));

		//Log.Message("crib : " + crib.ToString());

		fullVector = exitCell.ToVector3() - crib.Position.ToVector3();

		Toil climbOut = ToilMaker.MakeToil("LeaveCrib");
		climbOut.defaultCompleteMode = ToilCompleteMode.Delay;
		climbOut.defaultDuration = ticksRequired;
		climbOut.handlingFacing = true;
		climbOut.initAction = delegate
		{
		};
		climbOut.tickAction = delegate
		{
			this.pawn.rotationTracker.FaceCell(exitCell);
		};
		climbOut.AddFinishAction(delegate
		{
			this.pawn.jobs.posture = PawnPosture.Standing;
			pawn.SetPositionDirect(exitCell);
			pawn.Drawer.tweener.ResetTweenedPosToRoot();
		});
		yield return climbOut;

		yield break;
	}


}






//must be capable of manipulation to do anything to a toddler
if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
			   {
				   foreach (Thing t in c.GetThingList(pawn.Map))
				   {
					   Pawn toddler = t as Pawn;
					   if (toddler == null || !ToddlerUtility.IsLiveToddler(toddler)) continue;

					   //drafted pawns can pick babies up
					   if (pawn.Drafted)
					   {
						   FloatMenuOption pickUp = (pawn.CanReach(toddler, PathEndMode.ClosestTouch, Danger.Deadly) ? 
							   new FloatMenuOption("Carry".Translate(toddler), delegate {
								   Job job = JobMaker.MakeJob(Toddlers_DefOf.CarryToddler, toddler);
								   job.count = 1;
								   pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
							   },MenuOptionPriority.RescueOrCapture) :
							   new FloatMenuOption("CannotCarry".Translate(toddler) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
						   opts.Add(pickUp);
					   }

					   //pawns can babies and toddlers to their cribs
					   if (toddler.InBed() || !pawn.CanReserveAndReach(toddler, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, ignoreOtherReservations: true))
					   {
						   continue;
					   }
					   //check for is already rescuable goes here
					   Building_Bed crib = RestUtility.FindBedFor(toddler, pawn, checkSocialProperness: false);
					   //if (crib == null) crib = RestUtility.FindBedFor(toddler, pawn, checkSocialProperness: false, ignoreOtherReservations: true);

					   FloatMenuOption putInCrib = new FloatMenuOption("Put " + toddler.NameShortColored + " in crib", delegate {
						   Job job = JobMaker.MakeJob(Toddlers_DefOf.PutInCrib, toddler, crib);
						   job.count = 1;
						   pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
					   }, MenuOptionPriority.RescueOrCapture, null, toddler);
					   if (crib == null)
					   {
						   putInCrib.Label += " : No crib available";
						   putInCrib.Disabled = true;
					   }
					   if (!pawn.CanReach(toddler,PathEndMode.ClosestTouch,Danger.Deadly) || !pawn.CanReach(crib, PathEndMode.Touch, Danger.Deadly))
					   {
						   putInCrib.Label += ": " + "NoPath".Translate().CapitalizeFirst();
						   putInCrib.Disabled = true;
					   }
					   opts.Add(putInCrib);

				   }
			   }



class JobDriver_UndressBaby : JobDriver
{
	private Pawn Baby => TargetA.Pawn;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		//Log.Message("Fired UndressBaby.PreToilReservations");
		return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
	}
	public override void Notify_Starting()
	{
		base.Notify_Starting();
		job.count = 1;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		AddFailCondition(() => !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation));
		AddFailCondition(() => Baby.DestroyedOrNull() || !Baby.Spawned || Baby.DevelopmentalStage != DevelopmentalStage.Baby || !ChildcareUtility.CanSuckle(Baby, out var _));
		AddFailCondition(() => Baby.apparel == null);

		//Go to baby
		Toil goToBaby = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
		goToBaby.AddFinishAction(delegate
		{
			Pawn baby = (Pawn)goToBaby.actor.jobs.curJob.GetTarget(TargetIndex.A).Thing;
			if (baby.Awake() && ToddlerUtility.IsLiveToddler(baby) && !ToddlerUtility.InCrib(baby))
			{
				Job beDressedJob = JobMaker.MakeJob(Toddlers_DefOf.BeDressed, goToBaby.actor);
				job.count = 1;
				baby.jobs.StartJob(beDressedJob, JobCondition.InterruptForced);
			}
		});
		yield return goToBaby;

		foreach (Apparel apparel in Baby.apparel.WornApparel)
		{
			Toil wait = new Toil()
			{
				defaultCompleteMode = ToilCompleteMode.Delay,
				defaultDuration = (int)(apparel.GetStatValue(StatDefOf.EquipDelay) * 60f),
			};
			wait.WithProgressBarToilDelay(TargetIndex.A);
			wait.FailOnDespawnedOrNull(TargetIndex.A);
			yield return wait;

			yield return Toils_General.Do(delegate
			{
				if (Baby.apparel.WornApparel.Contains(apparel))
				{
					if (Baby.apparel.TryDrop(apparel, out var resultingAp))
					{
					}
					else
					{
						EndJobWith(JobCondition.Incompletable);
					}
				}
				else
				{
					EndJobWith(JobCondition.Incompletable);
				}
			});

		}

		Toil finish = new Toil();
		finish.defaultCompleteMode = ToilCompleteMode.Instant;
		finish.AddFinishAction(delegate
		{
			Pawn baby = (Pawn)finish.actor.jobs.curJob.GetTarget(TargetIndex.A).Thing;
			if (baby.CurJobDef == Toddlers_DefOf.BeDressed)
			{
				baby.jobs.EndCurrentJob(JobCondition.Succeeded);
			}
		});
		yield return finish;

	}
}


/*
//for logging purposes
[HarmonyPatch(typeof(SkillDef), nameof(SkillDef.IsDisabled))]
class IsDisabled_Patch
{
	static bool Prefix(ref bool __result, SkillDef __instance, WorkTags combinedDisabledWorkTags, IEnumerable<WorkTypeDef> disabledWorkTypes, WorkTags ___disablingWorkTags, bool ___neverDisabledBasedOnWorkTypes)
	{
		bool logFlag = false;

		if (__instance == SkillDefOf.Social)
		{
			Log.Message("Fired IsDisabled for Social");
			logFlag = true;
		}

		if (logFlag) Log.Message("combinedDisabledWorkTags: " + combinedDisabledWorkTags.ToString());
		if (logFlag) Log.Message("disablingWorkTags: " + ___disablingWorkTags.ToString());

		if ((combinedDisabledWorkTags & ___disablingWorkTags) != 0)
		{
			__result = true;
			return false;
		}

		if (logFlag) Log.Message("neverDisabledBasedOnWorkTypes: " + ___neverDisabledBasedOnWorkTypes.ToString());
		if (___neverDisabledBasedOnWorkTypes)
		{
			__result = false;
			return false;
		}

		if (logFlag) Log.Message("disabledWorkTypes: " + String.Concat(disabledWorkTypes.ToList()));
		List<WorkTypeDef> allDefsListForReading = DefDatabase<WorkTypeDef>.AllDefsListForReading;
		bool flag = false;
		for (int i = 0; i < allDefsListForReading.Count; i++)
		{
			WorkTypeDef workTypeDef = allDefsListForReading[i];
			for (int j = 0; j < workTypeDef.relevantSkills.Count; j++)
			{
				if (workTypeDef.relevantSkills[j] == __instance)
				{
					if (logFlag) Log.Message("relevant workType: " + workTypeDef.defName);
					if (!disabledWorkTypes.Contains(workTypeDef))
					{
						__result = false;
						return false;
					}
					flag = true;
				}
			}
		}
		if (!flag)
		{
			__result = false;
			return false;
		}
		__result = true;
		return false;
	}
}

//for logging purposes
[HarmonyPatch(typeof(SkillRecord))]
class TotallyDisabled_Patch
{
	public static MethodBase TargetMethod()
	{
		return typeof(SkillRecord).GetProperty(nameof(SkillRecord.TotallyDisabled), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(false);
	}

	static void Postfix(SkillRecord __instance, Pawn ___pawn, SkillDef ___def, BoolUnknown ___cachedTotallyDisabled)
	{
		if (___def == SkillDefOf.Social && ToddlerUtility.IsToddler(___pawn))
		{
			Log.Message("Fired TotallyDisabled for Social for toddler: " + ___pawn);
			Log.Message("cachedTotallyDisabled: " + ___cachedTotallyDisabled);
		}
	}
}


//for logging purposes
[HarmonyPatch(typeof(SkillRecord), "CalculateTotallyDisabled")]
class CalculateTotallyDisabled_Patch
{
	static void Postfix(SkillRecord __instance, Pawn ___pawn, SkillDef ___def, bool __result)
	{
		if (___def == SkillDefOf.Social && ToddlerUtility.IsToddler(___pawn))
		{
			Log.Message("Fired CalculateTotallyDisabled for Social for toddler: " + ___pawn);
			Log.Message("result: " + __result);
		}
	}
}
*/





[HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
class FloatMenu_Patch
{
	//copied from Injured Carry with modifications
	private static TargetingParameters ForToddler(Pawn pawn)
	{
		return new TargetingParameters
		{
			canTargetPawns = true,
			canTargetBuildings = false,
			neverTargetIncapacitated = true,
			neverTargetHostileFaction = true,
			mapObjectTargetsMustBeAutoAttackable = false,
			validator = delegate (TargetInfo targ)
			{
				if (!targ.HasThing) return false;
				Pawn toddler = targ.Thing as Pawn;
				if (toddler == null || toddler == pawn) return false;
				return ToddlerUtility.IsLiveToddler(toddler);
			}
		};
	}

	//copied from Dress Patient with modifications
	private static TargetingParameters ForBaby(Pawn pawn)
	{
		return new TargetingParameters
		{
			canTargetPawns = true,
			canTargetBuildings = false,
			neverTargetIncapacitated = false,
			neverTargetHostileFaction = true,
			mapObjectTargetsMustBeAutoAttackable = false,
			validator = delegate (TargetInfo targ)
			{
				if (!targ.HasThing) return false;
				Pawn baby = targ.Thing as Pawn;
				if (baby == null || baby == pawn) return false;
				return baby.DevelopmentalStage == DevelopmentalStage.Baby && ChildcareUtility.CanSuckle(baby, out var _);
			}
		};
	}

	//copied from Dress Patient with modifications
	private static TargetingParameters ForApparel(LocalTargetInfo targetBaby)
	{
		return new TargetingParameters
		{
			canTargetItems = true,
			mapObjectTargetsMustBeAutoAttackable = false,
			validator = delegate (TargetInfo targ)
			{
				if (!targ.HasThing) return false;
				Apparel apparel = targ.Thing as Apparel;
				//Log.Message("apparel : " + apparel.Label);
				if (apparel == null) return false;
				if (!targetBaby.HasThing) return false;
				Pawn baby = targetBaby.Thing as Pawn;
				//Log.Message("baby : " + baby.Name);
				if (baby == null) return false;
				//Log.Message("HasPartsToWear : " + ApparelUtility.HasPartsToWear(baby, apparel.def));
				if (!apparel.PawnCanWear(baby) || !ApparelUtility.HasPartsToWear(baby, apparel.def)) return false;
				return true;
			}
		};
	}

	static void Postfix(ref List<FloatMenuOption> opts, Pawn pawn, Vector3 clickPos)
	{
		Log.Message("opts.Count = " + opts.Count);
		IntVec3 c = IntVec3.FromVector3(clickPos);
		//for non-toddlers
		if (!ToddlerUtility.IsLiveToddler(pawn))
		{
			//have to be able to manipulate to do anything to a baby
			if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
			{
				foreach (LocalTargetInfo localTargetInfo1 in GenUI.TargetsAt(clickPos, ForToddler(pawn), thingsOnly: true))
				{
					Pawn toddler = (Pawn)localTargetInfo1.Thing;

					//option to let crawlers out of their cribs
					if (ToddlerUtility.InCrib(toddler) && ToddlerUtility.IsCrawler(toddler))
					{
						FloatMenuOption letOutOfCrib = new FloatMenuOption("Let " + toddler.Label + " out of crib", delegate
						{
							Building_Bed crib = ToddlerUtility.GetCurrentCrib(toddler);
							if (crib == null) return;
							Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("LetOutOfCrib"), toddler, crib);
							job.count = 1;
							pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
						}, MenuOptionPriority.Default);
						opts.Add(FloatMenuUtility.DecoratePrioritizedTask(letOutOfCrib, pawn, toddler));
					}

					//option to pick up toddlers and take them to their bed

					//patch for Injured Carry to avoid duplicate menu options
					//checks the same logic as Injured Carry
					if (Toddlers_Mod.injuredCarryLoaded)
					{
						if (HealthAIUtility.ShouldSeekMedicalRest(toddler)
							&& !toddler.IsPrisonerOfColony && !toddler.IsSlaveOfColony
							&& (!toddler.InMentalState || toddler.health.hediffSet.HasHediff(HediffDefOf.Scaria))
							&& !toddler.IsColonyMech
							&& (toddler.Faction == Faction.OfPlayer || toddler.Faction == null || !toddler.Faction.HostileTo(Faction.OfPlayer)))
							continue;
					}

					if (!toddler.InBed()
						&& pawn.CanReserveAndReach(toddler, PathEndMode.OnCell, Danger.None, 1, -1, null, ignoreOtherReservations: true)
						&& !toddler.mindState.WillJoinColonyIfRescued
					)
					{
						FloatMenuOption putInCrib = new FloatMenuOption("Put " + toddler.Label + " in crib", delegate
						{
							Building_Bed building_Bed = RestUtility.FindBedFor(toddler, pawn, checkSocialProperness: false);
							if (building_Bed == null)
							{
								building_Bed = RestUtility.FindBedFor(toddler, pawn, checkSocialProperness: false, ignoreOtherReservations: true);
							}
							if (building_Bed == null)
							{
								string t = (!toddler.RaceProps.Animal) ? ((string)"NoNonPrisonerBed".Translate()) : ((string)"NoAnimalBed".Translate());
								Messages.Message("CannotRescue".Translate() + ": " + "No bed", toddler, MessageTypeDefOf.RejectInput, historical: false);
							}
							else
							{
								Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("PutInCrib"), toddler, building_Bed);
								job.count = 1;
								pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
							}
						}, MenuOptionPriority.RescueOrCapture, null, toddler);
						if (RestUtility.FindBedFor(toddler, pawn, checkSocialProperness: false, ignoreOtherReservations: true) == null)
						{
							putInCrib.Label += " : No crib available";
							putInCrib.Disabled = true;
						}
						opts.Add(FloatMenuUtility.DecoratePrioritizedTask(putInCrib, pawn, toddler));
					}
				}
				//options for dressing and undressing babies and toddlers
				foreach (LocalTargetInfo localTargetInfo1 in GenUI.TargetsAt(clickPos, ForBaby(pawn), thingsOnly: true))
				{
					Pawn baby = (Pawn)localTargetInfo1.Thing;

					//patch for Dress Patients to avoid duplicate menu options
					//check the same logic as Dress Patients to figure out if that mod will be generating a menu option 
					if (Toddlers_Mod.dressPatientsLoaded)
					{
						if (baby.InBed()
							 && (baby.Faction == Faction.OfPlayer || baby.HostFaction == Faction.OfPlayer)
							 && (baby.guest != null ? pawn.guest.interactionMode != PrisonerInteractionModeDefOf.Execution : true)
							 && HealthAIUtility.ShouldSeekMedicalRest(baby))
							continue;
					}

					if (!pawn.CanReserveAndReach(baby, PathEndMode.ClosestTouch, Danger.None, 1, -1, null, ignoreOtherReservations: true))
						continue;

					//option to dress baby
					FloatMenuOption dressBaby = new FloatMenuOption("Dress " + baby.Label, delegate ()
					{
						Find.Targeter.BeginTargeting(ForApparel(baby), (LocalTargetInfo targetApparel) =>
						{
							//Log.Message("pawn : " + pawn.Name);
							//Log.Message("baby : " + baby.Name);
							//Log.Message("apparel : " + targetApparel.Label);
							Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("DressBaby"), baby, targetApparel);
							targetApparel.Thing.SetForbidden(false);
							pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
						});
					}, MenuOptionPriority.High);
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(dressBaby, pawn, baby));
				}
			}

		}

		//for toddlers, mostly disabling/removing options for things they can't do
		else
		{
			foreach (FloatMenuOption test in opts)
			{
				Log.Message("opt Label: " + test.Label);
			}
			int n = opts.RemoveAll(x => x.Label.Contains(pawn.LabelShort));
			Log.Message("n: " + n);
			//opts.RemoveAll(x => x.revalidateClickTarget == pawn || x.Label.Contains(pawn.LabelShort) || x.Label.Contains(pawn.LabelShortCap));

			foreach (Thing t in c.GetThingList(pawn.Map))
			{
				if (t.def.IsApparel && !ToddlerUtility.CanDressSelf(pawn))
				{
					//copied directly from source
					//this will allow us to identify the menu options related to wearing this object
					string key = "ForceWear";
					if (t.def.apparel.LastLayer.IsUtilityLayer)
					{
						key = "ForceEquipApparel";
					}
					string text = key.Translate(t.Label, t);
					//Log.Message("text = " + text);

					//disable the float menu option and tell the player why
					foreach (FloatMenuOption wear in opts.FindAll(x => x.Label.Contains(text)))
					{
						//if it's already disabled, leave it alone
						if (wear.Disabled) continue;

						wear.Label = text += " : Not old enough to dress self";
						wear.Disabled = true;
					}
				}

				if (t.def.ingestible != null && !ToddlerUtility.CanFeedSelf(pawn))
				{
					//copied directly from source
					//this will allow us to identify the menu options related to consuming this object
					string text;
					if (t.def.ingestible.ingestCommandString.NullOrEmpty())
					{
						text = "ConsumeThing".Translate(t.LabelShort, t);
					}
					else
					{
						text = t.def.ingestible.ingestCommandString.Formatted(t.LabelShort);
					}

					//disable the float menu option and tell the player why
					foreach (FloatMenuOption consume in opts.FindAll(x => x.Label.Contains(text)))
					{
						//if it's already disabled, leave it alone
						if (consume.Disabled) continue;

						consume.Label = text += " : Not old enough to feed self";
						consume.Disabled = true;
					}
				}
			}
		}
	}


	public static LocalTargetInfo Old_SafePlaceForBaby(Pawn baby, Pawn hauler)
	{
		//Log.Message("Fired SafePlaceForBaby");
		if (!ChildcareUtility.CanSuckle(baby, out var _) || !ChildcareUtility.CanHaulBabyNow(hauler, baby, false, out var _))
			return LocalTargetInfo.Invalid;

		Building_Bed bed = baby.CurrentBed() ?? RestUtility.FindBedFor(baby, hauler, checkSocialProperness: true, ignoreOtherReservations: false, baby.GuestStatus);
		//Log.Message("bed: " + bed);

		IntVec3 currentCell = baby.PositionHeld;
		LocalTargetInfo target = LocalTargetInfo.Invalid;

		//if the baby has a serious temperature injury
		//look for somewhere they can recover
		//ignoring zoning
		if (baby.health != null && baby.health.hediffSet != null
			&& baby.health.hediffSet.HasTemperatureInjury(TemperatureInjuryStage.Serious))
		{
			//if the best available bed is at a safe temperature, pick that
			if (bed != null && baby.SafeTemperatureRange().Includes(TemperatureAtBed(bed, baby.MapHeld)))
				return bed;


		}

		//sick babies and toddlers should both be taken to medical beds if possible
		//requiring safe temperature but not necessarily comfortable
		if (HealthAIUtility.ShouldSeekMedicalRest(baby))
		{
			//Log.Message("Baby " + baby + " is sick");

			//if the best available bed is at a safe temperature, pick that
			if (bed != null && baby.SafeTemperatureRange().Includes(TemperatureAtBed(bed, baby.MapHeld)))
				return bed;

			//if we didn't find a good bed, look for just a spot that's a good temperature 
			target = ChildcareUtility.BabyNeedsMovingForTemperatureReasons(baby, hauler, out var preferredRegion)
				? ((LocalTargetInfo)RCellFinder.SpotToStandDuringJob(hauler, null, preferredRegion))
				: ((!baby.Spawned)
					? ((LocalTargetInfo)RCellFinder.SpotToStandDuringJob(hauler, null, baby.GetRegionHeld()))
					: ((LocalTargetInfo)baby.Position));

			return target;
		}

		//more relaxed logic for healthy toddlers
		if (!baby.Downed)
		{
			//Log.Message("Baby " + baby + " is a toddler");

			//if the toddler is in a non-optimal temperature, try to pick a spot that would be better
			if (ChildcareUtility.BabyNeedsMovingForTemperatureReasons(baby, hauler, out var preferredRegion))
			{
				//Log.Message("Baby " + baby + " needs moving for temperature reasons");

				//if their bed would be comfortable, take them there
				if (bed != null && baby.ComfortableTemperatureRange().Includes(TemperatureAtBed(bed, baby.MapHeld)))
					return bed;
				//otherwise pick a spot in the region indicated by BabyNeedsMovingForTemperatureReasons
				else target = (LocalTargetInfo)RCellFinder.SpotToStandDuringJob(hauler, null, preferredRegion);
				//if they have a bed check which of the bed and the target spot is the better temperature, preferring the bed if they're equivalent
				if (bed != null) return BestTemperatureForPawn(bed, target, baby);
				return target;
			}

			//if the toddler is tired and they have a bed to go to, consider moving them to it
			else if (bed != null && baby.needs.rest.CurLevelPercentage < 0.28)
			{
				//Log.Message("Baby " + baby + " is tired");

				//if the bed is a comfortable temperature, just do it
				if (baby.ComfortableTemperatureRange().Includes(TemperatureAtBed(bed, baby.MapHeld))) return bed;

				//otherwise if the bed is a better temperature than where the toddller is now, do it
				return BestTemperatureForPawn(bed, currentCell, baby);
			}

			//otherwise if the toddler isn't spawned (ie being carried), try to pick a spot near where they are currently (ie just put them down)
			else if (!baby.Spawned) return RCellFinder.SpotToStandDuringJob(hauler, null, baby.GetRegionHeld());

			//if none of the above, just leave them where they are
			else return currentCell;
		}

		//mostly-vanilla logic for babies that prefers taking them to bed even if they aren't tired
		else
		{
			//if the bed's a comfortable temperature, take them straight there
			if (baby.ComfortableTemperatureRange().Includes(TemperatureAtBed(bed, baby.MapHeld)))
			{
				//Log.Message("bed is fine");
				return bed;
			}

			//if they need moving for temperature reasons
			if (ChildcareUtility.BabyNeedsMovingForTemperatureReasons(baby, hauler, out var preferredRegion))
			{
				//Log.Message("needs moving for temperature reasons");
				//pick a spot in the region indicated by BabyNeedsMovingForTemperatureReasons
				target = (LocalTargetInfo)RCellFinder.SpotToStandDuringJob(hauler, null, preferredRegion);
				//if they have a bed check which of the bed and the target spot is the better temperature, preferring the bed if they're equivalent
				//Log.Message("bed temperature: " + GenTemperature.GetTemperatureForCell(bed.Position, hauler.MapHeld));
				//Log.Message("target: " + target.Cell.ToString() + ", temperature: " + GenTemperature.GetTemperatureForCell(target.Cell, hauler.MapHeld));
				if (bed != null) return BestTemperatureForPawn(bed, target, baby);
				return target;
			}
			//otherwise
			else
			{
				//take them to their bed so long as it's not a worse temperature than where they are now
				if (bed != null) return BestTemperatureForPawn(bed, currentCell, baby);

				//otherwise if the toddler isn't spawned (ie being carried), try to pick a spot near where they are currently (ie just put them down)
				else if (!baby.Spawned) return RCellFinder.SpotToStandDuringJob(hauler, null, baby.GetRegionHeld());

				//if none of the above, just leave them where they are
				else return currentCell;
			}
		}

		//fall-through, shouldn't happen
		return LocalTargetInfo.Invalid;
	}

	public static bool NeedsRescue(Pawn baby, Pawn hauler)
	{
		//Log.Message("Fired NeedsRescue");
		if (baby == null || hauler == null) return false;
		if (CaravanFormingUtility.IsFormingCaravanOrDownedPawnToBeTakenByCaravan(baby)) return false;
		LocalTargetInfo safePlace = SafePlaceForBaby(baby, hauler, out BabyMoveReason moveReason);
		//Log.Message("safePlace: " + safePlace.ToString());
		//Log.Message("baby.PositionHeld: " + safePlace.ToString());
		if (safePlace.IsValid && safePlace.Cell != baby.PositionHeld) return true;
		return false;
	}

}