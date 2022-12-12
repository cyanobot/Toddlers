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
    class MentalState_RemoveClothes : MentalState
    {
        public Apparel target;

        public override void MentalStateTick()
        {
            if (pawn.apparel.WornApparelCount == 0) RecoverFromState();
            if (target == null || target.Wearer != pawn) target = pawn.apparel.WornApparel.RandomElement<Apparel>();
            base.MentalStateTick();
        }

    }
}
