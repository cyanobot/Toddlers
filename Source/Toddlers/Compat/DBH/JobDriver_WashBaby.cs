﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using static Toddlers.Patch_DBH;
using static Toddlers.WashBabyUtility;

namespace Toddlers
{
    public class JobDriver_WashBaby : JobDriver
    {
        public Thing Water => job.targetB.Thing;
        public IntVec3 WaterCell => job.targetB.Cell;
        public Pawn Baby => (Pawn)job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!ReservationUtility.Reserve(pawn, Baby, job, 1, -1, (ReservationLayerDef)null, errorOnFailed))
            {
                return false;
            }
            if (job.targetB.HasThing)
            {
                if ((pawn.inventory == null 
                            || !pawn.inventory.Contains(Water)) 
                        && !ReservationUtility.Reserve(pawn, Water, job, 1, -1, (ReservationLayerDef)null, errorOnFailed))
                {
                    return false;
                }
            }
            else if (WaterCell.IsValid && !ReservationUtility.Reserve(pawn, (LocalTargetInfo)WaterCell, job, 1, -1, (ReservationLayerDef)null, errorOnFailed))
            {
                return false;
            }
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFailCondition(() =>
                pawn.Downed
                || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)
                || (pawn.Drafted && !job.playerForced)
                );
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            AddFailCondition(() =>
                !WashBabyUtility.ColonistShouldWash(Baby)
                || (Baby.Drafted && !job.playerForced)
                );
            if (pawn.inventory != null && pawn.inventory.Contains(base.TargetThingB))
            {
                yield return Toils_Misc.TakeItemFromInventoryToCarrier(pawn, TargetIndex.B);
            }
            else if (base.TargetThingB is Building)
            {
                yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell).FailOnForbidden(TargetIndex.B);
                yield return Toils_Reserve.Release(TargetIndex.B);
                yield return m_FillBottleFromThing.Invoke(null,
                    new object[] { TargetIndex.B, true, 1, true }) as Toil;
            }
            else if (!job.targetB.HasThing)
            {
                yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.ClosestTouch).FailOnForbidden(TargetIndex.B);
                yield return Toils_Reserve.Release(TargetIndex.B);
                yield return m_FillBottleFromCell.Invoke(null,
                    new object[] { TargetIndex.B, true, true }) as Toil;
            }
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil toil = ToilMaker.MakeToil("WashBaby");
            toil.defaultDuration = 800;
            toil.PlaySustainerOrSound(() => SoundDefOf.Interact_CleanFilth);
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.FailOnDestroyedNullOrForbidden(TargetIndex.A);
            toil.WithEffect(DefDatabase<EffecterDef>.GetNamed("WashingEffect"), TargetIndex.A);
            toil.WithProgressBar(TargetIndex.A, () => Baby.needs.AllNeeds.Find(n => n.def.defName== "Hygiene").CurLevel);
            toil.AddEndCondition(() => (Baby.needs.AllNeeds.Find(n => n.def.defName == "Hygiene").CurLevel < 1f) ? JobCondition.Ongoing : JobCondition.Succeeded);
            toil.initAction = delegate
            {
                //Baby.jobs.EndCurrentJob(JobCondition.InterruptForced, false);
                Job waitJob = JobMaker.MakeJob(ToddlerBeWashed, toil.actor, 5000);
                Baby.jobs.StartJob(waitJob, JobCondition.InterruptForced);
            };
            toil.tickAction = delegate
            {
                Need need_Hygiene = Baby.needs?.AllNeeds.Find(n => n.def.defName == "Hygiene");
                if (need_Hygiene != null)
                {
                    need_Hygiene.CurLevel = Mathf.Min(need_Hygiene.CurLevel + 0.002f, 1f);
                    f_lastGainTick.SetValue(need_Hygiene, Find.TickManager.TicksGame);
                }
            };
            toil.AddFinishAction(delegate
            {
                Need need_Hygiene = Baby.needs?.AllNeeds.Find(n => n.def.defName == "Hygiene");
                if (need_Hygiene != null)
                {
                    f_contaminated.SetValue(need_Hygiene, false);
                }
                if (Water != null && (Water.def.modExtensions?.Any(dme => dme.GetType().Name == "WaterExt") ?? false))
                {
                    Water.SplitOff(1);
                }
                if (Baby.CurJobDef == ToddlerBeWashed)
                {
                    Baby.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
            });
            yield return toil;
        }
    }
}
