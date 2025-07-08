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
#if RW_1_5
    class AnimationWorker_WiggleInCrib : AnimationWorker
    {
        
        public AnimationWorker_WiggleInCrib(AnimationDef def, Pawn pawn, AnimationPart part, PawnRenderNode node)
        : base(def, pawn, part, node)
        {

        }

        public override bool Enabled()
        {
            if (!base.Enabled()) return false;

            if (pawn.CurJobDef == Toddlers_DefOf.WiggleInCrib && pawn.jobs.curDriver.CurToilString == "WiggleInCrib") return true;

            return false;
        }

        public override float AngleAtTick(int tick, PawnDrawParms parms)
        {
            float x = (float)(Find.TickManager.TicksGame % 120) / 120f;
            return 15f * Waveform(f => f, x);
        }
    }
#else
    class AnimationWorker_WiggleInCrib : AnimationWorker_Toddler
    {
        public override float AngleAtTick(int tick, AnimationDef def, PawnRenderNode node, AnimationPart part, PawnDrawParms parms)
        {
            float x = (float)(Find.TickManager.TicksGame % 120) / 120f;
            return 15f * Waveform(f => f, x);
        }

        public override bool Enabled(AnimationDef def, PawnRenderNode node, AnimationPart part, PawnDrawParms parms)
        {
            if (!base.Enabled(def, node, part, parms)) return false;
            if (parms.pawn.CurJobDef == Toddlers_DefOf.WiggleInCrib && parms.pawn.jobs.curDriver.CurToilString == "WiggleInCrib") return true;


            return false;
        }

    }
#endif
}
