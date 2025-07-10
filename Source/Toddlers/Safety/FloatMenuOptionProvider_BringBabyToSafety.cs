using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

#if RW_1_5
#else

namespace Toddlers
{
    public class FloatMenuOptionProvider_BringBabyToSafety : FloatMenuOptionProvider
    {
        protected override bool Drafted => true;
        protected override bool Undrafted => true;
        protected override bool Multiselect => false;
        protected override bool RequiresManipulation => true;
        protected override bool MechanoidCanDo => true;
        public override bool CanTargetDespawned => true;

        public override bool SelectedPawnValid(Pawn pawn, FloatMenuContext context)
        {
            //LogUtil.DebugLog($"FloatMenuOptionProvider_BringBabyToSafety.SelectedPawnValid - pawn: {pawn}");

            if (!base.SelectedPawnValid(pawn, context)) return false;

            if (ChildcareUtility.CanSuckle(pawn, out _)) return false;

            //LogUtil.DebugLog($"FloatMenuOptionProvider_BringBabyToSafety.SelectedPawnValid - returning true");
            return true;
        }

        public override bool TargetPawnValid(Pawn pawn, FloatMenuContext context)
        {
            //LogUtil.DebugLog($"FloatMenuOptionProvider_BringBabyToSafety.TargetPawnValid  - pawn: {pawn}");
            if (!base.TargetPawnValid(pawn, context)) return false;

            if (!ChildcareUtility.CanSuckle(pawn, out _)) return false;

            //LogUtil.DebugLog($"FloatMenuOptionProvider_BringBabyToSafety.TargetPawnValid - returning true");
            return true;
        }

        public override IEnumerable<FloatMenuOption> GetOptionsFor(Pawn clickedPawn, FloatMenuContext context)
        {
            //LogUtil.DebugLog($"FloatMenuOptionProvider_BringBabyToSafety.GetOptionsFor  - clickedPawn: {clickedPawn}" +
            //    $", firstPawn: {context.FirstSelectedPawn}, CanReserveAndReach: {context.FirstSelectedPawn.CanReserveAndReach(clickedPawn, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, true)}");
            FloatMenuOption safetyOption = new FloatMenuOption("PutSomewhereSafe".Translate(clickedPawn.LabelCap, clickedPawn),null
                , MenuOptionPriority.RescueOrCapture, null, clickedPawn);

            //if the hauler can't reach the baby
            if (!context.FirstSelectedPawn.CanReserveAndReach(clickedPawn, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, true))
            {
                //return a disabled option so the player knows what the problem is
                safetyOption.Label += ": " + "NoPath".Translate().CapitalizeFirst();
                yield return safetyOption;
                yield break;
            }

            BabyMoveReason moveReason = BabyMoveReason.Undetermined;
            LocalTargetInfo safePlace = BabyMoveUtility.BestPlaceForBaby(clickedPawn, context.FirstSelectedPawn, ref moveReason, true);


            //LogUtil.DebugLog($"FloatMenuOptionProvider_BringBabyToSafety.GetOptionsFor  - safePlace: {safePlace}" +
            //    $", moveReason: {moveReason}"
            //    );

            //if there is a safe place to put the baby, go ahead and make the safety job action
            if (safePlace.IsValid)
            {
                safetyOption.action = delegate ()
                {
                    context.FirstSelectedPawn.jobs.TryTakeOrderedJob(ChildcareUtility.MakeBringBabyToSafetyJob(context.FirstSelectedPawn, clickedPawn), JobTag.Misc);
                };
                yield return FloatMenuUtility.DecoratePrioritizedTask(safetyOption,
                    context.FirstSelectedPawn, clickedPawn);
            }

            //if the best place to take the baby is back to bed, don't need an additional crib option
            if (safePlace.HasThing && safePlace.Thing is Building_Bed)
            {                
                yield break;
            }

            //if the baby's already in bed, don't need a crib option
            if (clickedPawn.CurrentBed() != null)
            {
                yield break;
            }

            Building_Bed foundBed = RestUtility.FindBedFor(clickedPawn, context.FirstSelectedPawn, checkSocialProperness: true);
            

            //if no bed found
            if (foundBed == null)
            {
                //if we also didn't find a safe place, need a disabled option so the player knows what the problem is
                if (safetyOption.Disabled)
                {
                    safetyOption.Label += ": " + "NoCrib".Translate().CapitalizeFirst();
                    yield return safetyOption;
                }
                yield break;
            }

            //otherwise a bed was found
            FloatMenuOption putInCrib = new FloatMenuOption("PutInCrib".Translate(clickedPawn), delegate
            {
                Building_Bed building_Bed = RestUtility.FindBedFor(clickedPawn, context.FirstSelectedPawn, checkSocialProperness: true);
                if (building_Bed == null)
                {
                    building_Bed = RestUtility.FindBedFor(clickedPawn, context.FirstSelectedPawn, checkSocialProperness: true, ignoreOtherReservations: true);
                }
                if (building_Bed == null)
                {
                    Messages.Message("CannotRescue".Translate() + ": " + "NoCrib".Translate(), clickedPawn, MessageTypeDefOf.RejectInput, historical: false);
                }
                else
                {
                    //DefDatabase<JobDef>.GetNamed("PutInCrib")
                    Job job = JobMaker.MakeJob(Toddlers_DefOf.PutInCrib, clickedPawn, building_Bed);
                    job.count = 1;
                    context.FirstSelectedPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }
            }, MenuOptionPriority.RescueOrCapture, null, clickedPawn);

            //if the crib is an unsafe temperature, warn the player
            if (!GenTemperature.SafeTemperatureAtCell(clickedPawn, foundBed.Position, clickedPawn.MapHeld))
            {
                putInCrib.Label += " : " + "BadTemperature".Translate();
            }
            yield return FloatMenuUtility.DecoratePrioritizedTask(putInCrib,
                    context.FirstSelectedPawn, clickedPawn);
        }
    }
}
#endif