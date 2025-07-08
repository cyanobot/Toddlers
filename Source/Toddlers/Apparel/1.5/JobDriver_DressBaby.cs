#if RW_1_5
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
    class JobDriver_DressBaby : JobDriver
    {
        private int duration;
        private int unequipBuffer = 0;
        private Pawn Baby => TargetA.Pawn;
        private Apparel Apparel => TargetB.Thing as Apparel;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            //Log.Message("Fired DressBaby.PreToilReservations");
            return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed) && pawn.Reserve(TargetB, job, 1, -1, null, errorOnFailed);
        }
        public override void Notify_Starting()
        {
            base.Notify_Starting();

            // Job duration based on equip time of target apparel.
            duration = (int)(Apparel.GetStatValue(StatDefOf.EquipDelay) * 60f);
            List<Apparel> wornApparel = Baby.apparel.WornApparel;
            foreach (Apparel apparel in wornApparel)
            {
                if (!ApparelUtility.CanWearTogether(Apparel.def, apparel.def, Baby.RaceProps.body))
                {
                    // Add equip time of all apparel that must be removed.
                    duration += (int)(apparel.GetStatValue(StatDefOf.EquipDelay) * 60f);
                }
            }
            job.count = 1;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFailCondition(() => !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation));
            AddFailCondition(() => Baby.DestroyedOrNull() || !Baby.Spawned || Baby.DevelopmentalStage != DevelopmentalStage.Baby || !ChildcareUtility.CanSuckle(Baby, out var _));
            AddFailCondition(() => Apparel.DestroyedOrNull() || Apparel.IsBurning());

            //go to clothes, pick up
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnForbidden(TargetIndex.B);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);
            //go to baby
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);

            //make baby wait
            //Toil makeBabyWait = ToilMaker.MakeToil("MakeBabyWait");
            //makeBabyWait.defaultCompleteMode = ToilCompleteMode.Instant;
            //makeBabyWait.

            //put clothes down beside baby
            yield return Toils_Haul.PlaceCarriedThingInCellFacing(TargetIndex.A);

            // Wait duration, strip conflicting clothes periodically.
            Toil stripAndDress = ToilMaker.MakeToil("StripAndDress");
            stripAndDress.defaultCompleteMode = ToilCompleteMode.Delay;
            stripAndDress.defaultDuration = duration;
            stripAndDress.tickAction = delegate ()
            {
                unequipBuffer++;
                TryUnequipSomething();
            };
            stripAndDress.AddPreInitAction(delegate
            {
                Pawn baby = (Pawn)stripAndDress.actor.jobs.curJob.GetTarget(TargetIndex.A).Thing;
                if (!baby.Downed && baby.Awake() && ToddlerUtility.IsLiveToddler(baby) && !CribUtility.InCrib(baby))
                {
                    LogUtil.DebugLog("attempting to assign BeDressed job to baby: " + baby);
                    Job beDressedJob = JobMaker.MakeJob(Toddlers_DefOf.BeDressed, stripAndDress.actor);
                    beDressedJob.count = 1;
                    baby.jobs.StartJob(beDressedJob, JobCondition.InterruptForced);
                }
                
            });
            stripAndDress.AddFinishAction(delegate
            {
                Pawn baby = (Pawn)stripAndDress.actor.jobs.curJob.GetTarget(TargetIndex.A).Thing;
                if (baby?.CurJobDef == Toddlers_DefOf.BeDressed)
                {
                    baby.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
            });

            stripAndDress.WithProgressBarToilDelay(TargetIndex.A);
            stripAndDress.FailOnDespawnedOrNull(TargetIndex.A);
            yield return stripAndDress;

            // Equip apparel.
            yield return Toils_General.Do(() => Baby.apparel.Wear(Apparel));
            if (Baby.outfits != null && job.playerForced)
            {
                Baby.outfits.forcedHandler.SetForced(Apparel, forced: true);
            }
            yield break;
        }

        private void TryUnequipSomething()
        {
            foreach (Apparel wornApparel in Baby.apparel.WornApparel)
            {
                if (!ApparelUtility.CanWearTogether(Apparel.def, wornApparel.def, Baby.RaceProps.body))
                {
                    int equipDelay = (int)(wornApparel.GetStatValue(StatDefOf.EquipDelay, true) * 60f);
                    if (unequipBuffer >= equipDelay)
                    {
                        if (!Baby.apparel.TryDrop(wornApparel, out Apparel droppedApparel, Baby.PositionHeld, false))
                        {
                            Log.Error(Baby + " could not drop " + wornApparel.ToStringSafe());
                            EndJobWith(JobCondition.Errored);
                            return;
                        }
                        unequipBuffer -= equipDelay;
                    }
                    break;
                }
            }
        }
    }
}
#endif