using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toddlers
{
    class JobDriver_RestIdleInCrib : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFailCondition(() => !ToddlerUtility.InCrib(pawn) || pawn.Downed);
            Toil toil = ToilMaker.MakeToil("MakeNewToil");
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = ToddlerPlayUtility.PlayDuration;
            toil.AddPreInitAction(delegate ()
            {
                pawn.jobs.posture = PawnPosture.LayingInBed;
            });
            toil.AddPreTickAction(delegate ()
            {

            });

            yield return toil;
        }
    }
}
