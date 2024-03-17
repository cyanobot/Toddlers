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

            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnForbidden(TargetIndex.B);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);
            //yield return Toils_Haul.CarryHauledThingToCell(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
            yield return Toils_Haul.PlaceCarriedThingInCellFacing(TargetIndex.A);

            // Wait duration, strip conflicting clothes periodically.
            Toil stripAndDress = new Toil()
            {
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = duration,
                tickAction = delegate ()
                {
                    unequipBuffer++;
                    TryUnequipSomething();
                }
            };
            stripAndDress.AddPreInitAction(delegate
            {
                Pawn baby = (Pawn)stripAndDress.actor.jobs.curJob.GetTarget(TargetIndex.A).Thing;
                if (baby.Awake() && ToddlerUtility.IsLiveToddler(baby) && !ToddlerUtility.InCrib(baby))
                {
                    Job beDressedJob = JobMaker.MakeJob(Toddlers_DefOf.BeDressed, stripAndDress.actor);
                    job.count = 1;
                    baby.jobs.StartJob(beDressedJob, JobCondition.InterruptForced);
                }
                
            });
            stripAndDress.AddFinishAction(delegate
            {
                Pawn pawn = (Pawn)stripAndDress.actor.jobs.curJob.GetTarget(TargetIndex.A).Thing;
                if (pawn.CurJobDef == Toddlers_DefOf.BeDressed)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Succeeded);
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
