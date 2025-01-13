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
            AddFailCondition(() => !CribUtility.InCrib(pawn) || pawn.Downed);
            Toil toil = ToilMaker.MakeToil("LayAngleInCrib");
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = ToddlerPlayUtility.PlayDuration;
            toil.AddPreInitAction(delegate ()
            {
                angle = Rand.Value * 360f;
                PawnPosture posture = PawnPosture.LayingInBedFaceUp;
                this.pawn.jobs.posture = posture;
                //this.pawn.Drawer.renderer.SetAllGraphicsDirty();
                pawn.Drawer.renderer.SetAnimation(Toddlers_AnimationDefOf.LayAngleInCrib);
            });
            toil.AddPreTickAction(delegate ()
            {
                ticksAtAngle++;
                if (ticksAtAngle > 620)
                {
                    angle = Rand.Value * 360f;
                    ticksAtAngle = 0;
                    //this.pawn.Drawer.renderer.SetAllGraphicsDirty();
                }
                if (pawn.Drawer.renderer.CurAnimation != Toddlers_AnimationDefOf.LayAngleInCrib)
                    pawn.Drawer.renderer.SetAnimation(Toddlers_AnimationDefOf.LayAngleInCrib);
            });
            toil.AddFinishAction(() => pawn.Drawer.renderer.SetAnimation(null));                 //this.pawn.Drawer.renderer.SetAllGraphicsDirty());
            yield return toil;
        }
    }
}
