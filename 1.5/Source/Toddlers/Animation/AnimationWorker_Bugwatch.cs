﻿using System;
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
    class AnimationWorker_Bugwatch : AnimationWorker
    {
        public AnimationWorker_Bugwatch(AnimationDef def, Pawn pawn, AnimationPart part, PawnRenderNode node)
        : base(def, pawn, part, node)
        {
            
        }

        public override bool Enabled()
        {
            if (!base.Enabled()) return false;

            if (pawn.CurJobDef == Toddlers_DefOf.ToddlerBugwatching && pawn.jobs.curDriver.CurToilString == "Bugwatching") return true;

            return false;
        }

        public override float AngleAtTick(int tick, PawnDrawParms parms)
        {
            if (parms.facing == Rot4.East) return CRAWL_ANGLE;
            if (parms.facing == Rot4.West) return -1f * CRAWL_ANGLE;
            return 0f;
        }
    }

}
