using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using LudeonTK;
using static Toddlers.AnimationUtility;

namespace Toddlers
{
    class AnimationWorker_LayAngleInCrib : AnimationWorker
    {
        
        public AnimationWorker_LayAngleInCrib(AnimationDef def, Pawn pawn, AnimationPart part, PawnRenderNode node)
        : base(def, pawn, part, node)
        {

        }

        public override bool Enabled()
        {
            if (!base.Enabled()) return false;

            if (pawn.CurJobDef == Toddlers_DefOf.LayAngleInCrib && pawn.jobs.curDriver.CurToilString == "LayAngleInCrib") return true;

            return false;
        }

        public override float AngleAtTick(int tick, PawnDrawParms parms)
        {
            if (!(pawn.jobs.curDriver is JobDriver_LayAngleInCrib)) return 0f;
            return ((JobDriver_LayAngleInCrib)pawn.jobs.curDriver).angle;
        }
    }

}
