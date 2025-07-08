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
    class MentalStateWorker_RemoveClothes : MentalStateWorker
    {
        public override bool StateCanOccur(Pawn pawn)
        {
            //Log.Message("MentalStateWorker_RemoveClothes.StateCanOccur, base: " + base.StateCanOccur(pawn) 
            //    + ", WornApparelCount: " + pawn.apparel.WornApparelCount);
            if (!base.StateCanOccur(pawn)) return false;
            if (pawn.apparel.WornApparelCount == 0) return false;
            return true;
        }
    }
}
