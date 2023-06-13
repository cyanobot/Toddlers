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


namespace Toddlers
{
	[HarmonyPatch()]
	class Seek_Patch
	{
		static Type[] targetTypes = new Type[]
		{
			//typeof(RimWorld.BabyPlayGiver_PlayWalking),
			//typeof(Verse.AI.JobDriver_BabyPlay)
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

		static bool Prefix(object[] __args, MethodBase __originalMethod, ref object __result, object __instance)
		{
			//Log.Message("Firing Prefix for " + __originalMethod.Name);
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
	}


	//put copies here
	//make classes static
	//give non-static methods a new first argument: object instance
	//make those methods static
	//wherever "this" would be used, replace with instance

	public static class BabyPlayGiver_PlayWalking 
	{
		public static bool CanDo(object instance, Pawn pawn, Pawn other)
		{
			Log.Message("CanDo -- IsCarrying: " + pawn.IsCarryingPawn(other) 
				+ ", CanReserveAndReach: " + pawn.CanReserveAndReach(other, PathEndMode.Touch, Danger.Some));
			if (!pawn.IsCarryingPawn(other) && !pawn.CanReserveAndReach(other, PathEndMode.Touch, Danger.Some))
			{
				return false;
			}
			return true;
		}

		public static Job TryGiveJob(object instance, Pawn pawn, Pawn other)
		{
			IntVec3 intVec = JobDriver_PlayWalking.TryFindWanderCell(pawn, other.PositionHeld);
			Log.Message("TryGiveJob -- intVec: " + intVec + ", CanReserveAndReach: " + pawn.CanReserveAndReach(intVec, PathEndMode.Touch, Danger.Some));
			if (!intVec.IsValid)
			{
				return null;
			}
			if (!pawn.CanReserveAndReach(intVec, PathEndMode.Touch, Danger.Some))
			{
				return null;
			}
			RimWorld.BabyPlayGiver_PlayWalking typedInstance = (RimWorld.BabyPlayGiver_PlayWalking)instance;
			Job job = JobMaker.MakeJob(typedInstance.def.jobDef, other, intVec);
			job.count = 1;
			Log.Message("Returning job " + job);
			return job;
		}
	}

	public static class JobDriver_BabyPlay
	{

		public static bool TryMakePreToilReservations(object instance, bool errorOnFailed)
		{
			Verse.AI.JobDriver_BabyPlay typedInstance = (Verse.AI.JobDriver_BabyPlay)instance;
			Log.Message("Firing BabyPlay.TryMakePreToilReservations, pawn: " + typedInstance.pawn
				+ ", baby: " + typedInstance.job.GetTarget(TargetIndex.A));
			return typedInstance.pawn.Reserve(typedInstance.job.GetTarget(TargetIndex.A), typedInstance.job, 1, -1, null, errorOnFailed);
		}

		public static IEnumerable<Toil> MakeNewToils(object instance)
		{
			Verse.AI.JobDriver_BabyPlay typedInstance = (Verse.AI.JobDriver_BabyPlay)instance;
			Pawn baby = (Pawn)typeof(Verse.AI.JobDriver_BabyPlay).GetProperty("Baby", BindingFlags.NonPublic | BindingFlags.Instance)
				.GetValue(typedInstance);

			Log.Message("Firing BabyPlay.TryMakePreToilReservations, pawn: " + typedInstance.pawn
				+ ", baby: " + baby);

			typedInstance.FailOnDestroyedNullOrForbidden(TargetIndex.A);
			typedInstance.FailOnSomeonePhysicallyInteracting(TargetIndex.A);
			typedInstance.AddFailCondition(() => !ChildcareUtility.CanSuckle(baby, out var _));
			typedInstance.AddFinishAction(delegate
			{
				typedInstance.AddPlayThoughtIfAppropriate();
			});
			bool finishedSetup = (bool)typeof(Verse.AI.JobDriver_BabyPlay).GetField("finishedSetup", BindingFlags.NonPublic | BindingFlags.Instance)
				.GetValue(typedInstance);
			typeof(Verse.AI.JobDriver_BabyPlay).GetMethod("SetFinalizerJob", BindingFlags.Instance | BindingFlags.NonPublic)
				.Invoke(typedInstance, new object[] { new Func<JobCondition, Job>(
					(JobCondition condition) => (!finishedSetup) ? null : ChildcareUtility.MakeBringBabyToSafetyJob(typedInstance.pawn, baby)
					) });
			//SetFinalizerJob((JobCondition condition) => (!finishedSetup) ? null : ChildcareUtility.MakeBringBabyToSafetyJob(pawn, Baby));
			foreach (Toil item in CreateStartingCondition(instance))
			{
				yield return item;
			}
			yield return typedInstance.SetPlayPercentage();
			yield return Toils_General.DoAtomic(delegate
			{
				finishedSetup = true;
			});
			foreach (Toil item2 in (IEnumerable<Toil>)typeof(Verse.AI.JobDriver_BabyPlay)
				.GetMethod("Play", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(typedInstance,new object[] { }))
			{
				yield return item2;
			}
			foreach (Toil item3 in JobDriver_PickupToHold.Toils(typedInstance))
			{
				yield return item3;
			}
		}

		public static IEnumerable<Toil> CreateStartingCondition(object instance)
		{
			Verse.AI.JobDriver_BabyPlay typedInstance = (Verse.AI.JobDriver_BabyPlay)instance;
			Verse.AI.JobDriver_BabyPlay.StartingConditions startingCondition = 
				(Verse.AI.JobDriver_BabyPlay.StartingConditions)typeof(Verse.AI.JobDriver_BabyPlay)
				.GetProperty("StartingCondition", BindingFlags.NonPublic | BindingFlags.Instance)
				.GetValue(typedInstance);

			if (startingCondition == Verse.AI.JobDriver_BabyPlay.StartingConditions.PickupBaby)
			{
				foreach (Toil item in JobDriver_PickupToHold.Toils(typedInstance, TargetIndex.A, subtractNumTakenFromJobCount: false))
				{
					yield return item;
				}
				yield break;
			}
			if (startingCondition == Verse.AI.JobDriver_BabyPlay.StartingConditions.GotoBaby)
			{
				yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
				yield break;
			}
			throw new NotImplementedException(startingCondition.ToString());
		}

	}

}