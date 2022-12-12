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
    class JobDriver_ToddlerBePlayedWith : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil toil = ToilMaker.MakeToil("MakeNewToils");
            toil.tickAction = delegate ()
            {
                ToddlerPlayUtility.ToddlerPlayedWithTickCheckEnd(this.pawn);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            yield return toil;
            yield break;
        }
    }
}
