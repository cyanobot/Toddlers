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
    class AnimationWorker_ToddlerCrawl_Root : AnimationWorker
    {
        public AnimationWorker_ToddlerCrawl_Root(AnimationDef def, Pawn pawn, AnimationPart part, PawnRenderNode node)
        : base(def, pawn, part, node)
        {
            
        }

        public override bool Enabled()
        {
            if (!pawn.Spawned) return false;
            if (!base.Enabled()) return false;
            return true;
        }

        public override float AngleAtTick(int tick, PawnDrawParms parms)
        {
            if (parms.facing == Rot4.East) return CRAWL_ANGLE;
            if (parms.facing == Rot4.West) return -1f * CRAWL_ANGLE;
            if (parms.facing == Rot4.North && parms.flipHead) return 180f;
            return 0f;
        }
    }

    class AnimationWorker_ToddlerCrawl_Head : AnimationWorker
    {
        public AnimationWorker_ToddlerCrawl_Head(AnimationDef def, Pawn pawn, AnimationPart part, PawnRenderNode node)
        : base(def, pawn, part, node)
        {

        }

        public override bool Enabled()
        {
            if (!base.Enabled()) return false;

            if (!pawn.Spawned) return false;
            if (!pawn.pather.Moving) return false;

            return true;
        }

        public override float AngleAtTick(int tick, PawnDrawParms parms)
        {
            if (parms.facing == Rot4.East) return -0.5f * CRAWL_ANGLE;
            if (parms.facing == Rot4.West) return 0.5f * CRAWL_ANGLE;
            if (parms.facing == Rot4.North && parms.flipHead) return 180f;
            return 0f;
        }
    }
}
