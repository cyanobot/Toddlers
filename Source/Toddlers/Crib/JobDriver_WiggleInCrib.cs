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
    class JobDriver_WiggleInCrib : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFailCondition(() => !ToddlerUtility.InCrib(pawn) || pawn.Downed);
            Toil toil = ToilMaker.MakeToil("WiggleInCrib");
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = ToddlerPlayUtility.PlayDuration;
            toil.AddPreInitAction(delegate ()
            {
                this.pawn.jobs.posture = PawnPosture.InBedMask;
                pawn.Rotation = Rot4.South;
            });
            toil.handlingFacing = true; 
            yield return toil;
        }
    }
}
