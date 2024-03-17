using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toddlers
{
    class JobDriver_BePlayedWith : JobDriver
    {
        protected const TargetIndex AdultInd = TargetIndex.A;

        protected Pawn Adult => (Pawn)base.TargetThingA;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (pawn.CarriedBy == null) pawn.Reserve(pawn.Position,job);
            return true;
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Log.Message("Firing JobDriver_BePlayedWith.MakeNewToils for baby " + pawn + ", AdultInd: " + AdultInd
            //    + ", Adult: " + Adult);
            this.FailOnDestroyedNullOrForbidden(AdultInd);
            //this.FailOnSomeonePhysicallyInteracting(AdultInd);
            this.AddFailCondition(delegate
            {
                return !(Adult.jobs.curDriver is JobDriver_BabyPlay);
            });

            Toil wait = ToilMaker.MakeToil("Wait");
            
            wait.defaultCompleteMode = ToilCompleteMode.Never;
            wait.initAction = delegate
            {
                if (pawn.CarriedBy == null)
                {
                    base.Map.pawnDestinationReservationManager.Reserve(pawn, job, pawn.Position);
                    pawn.pather?.StopDead();

                    if (pawn.CurrentBed() != null ||
                        (pawn.Position != null && pawn.Position.GetThingList(pawn.Map)
                        .Find((x) => x is Building_Bed) != null))
                    {
                        pawn.jobs.posture = PawnPosture.LayingInBed;
                        PortraitsCache.SetDirty(pawn);
                    }
                }

            };
            wait.tickAction = delegate
            {
                if (pawn.CarriedBy == null && pawn.CurrentBed() != null)
                {
                    pawn.GainComfortFromCellIfPossible();
                    Thing spawnedParentOrMe;
                    if (pawn.IsHashIntervalTick(100) && (spawnedParentOrMe = pawn.SpawnedParentOrMe) != null
                        && !spawnedParentOrMe.Position.Fogged(spawnedParentOrMe.Map)
                        && pawn.health.hediffSet.GetNaturallyHealingInjuredParts().Any())
                    {
                        FleckMaker.ThrowMetaIcon(spawnedParentOrMe.Position, spawnedParentOrMe.Map, FleckDefOf.HealingCross);
                    }
                }
            };

            yield return wait;
        }
    }
}
