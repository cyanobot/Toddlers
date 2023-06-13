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
    class JobDriver_LayAngleInCrib : JobDriver
    {
        public float angle;
        public int ticksAtAngle = 0;

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
                angle = Rand.Value * 360f;
                PawnPosture posture = PawnPosture.LayingInBedFaceUp;
                this.pawn.jobs.posture = posture;
                this.pawn.Drawer.renderer.graphics.SetAllGraphicsDirty();
            });
            toil.AddPreTickAction(delegate ()
            {
                ticksAtAngle++;
                if (ticksAtAngle > 620)
                {
                    angle = Rand.Value * 360f;
                    ticksAtAngle = 0;
                    this.pawn.Drawer.renderer.graphics.SetAllGraphicsDirty();
                }
            });
            toil.AddFinishAction(() => this.pawn.Drawer.renderer.graphics.SetAllGraphicsDirty());
            yield return toil;
        }
    }
}
