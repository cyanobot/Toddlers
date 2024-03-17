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
            //Log.Message("MentalState_RemoveClothes.MentalStateTick");
            if (pawn.apparel.WornApparelCount == 0 || HealthAIUtility.ShouldSeekMedicalRest(pawn)) 
            {
                //Log.Message("No apparel or ShouldSeekMedicalRest");
                RecoverFromState();
                return;
            }
            if (target == null || target.Wearer != pawn)
            {
                //Log.Message("No target");
                if (Rand.Chance(0.5f))
                {
                    //Log.Message("Exiting state");
                    RecoverFromState();
                    return;
                }
                else
                    target = pawn.apparel.WornApparel.RandomElement<Apparel>();
                //Log.Message("target: " + target);
            }
            
            //don't remove clothes if it would make us unsafe (uncomfy is allowed)
            float curTemp = GenTemperature.GetTemperatureForCell(pawn.Position, pawn.MapHeld);
            float minSafeTemp = pawn.GetStatValue(StatDefOf.ComfyTemperatureMin, applyPostProcess: true, 1) - 10f;
            if (curTemp < minSafeTemp + target.GetStatValue(StatDefOf.Insulation_Cold))
            {
                //Log.Message("Too cold");
                RecoverFromState();
                return;
            }
            float maxSafeTemp = pawn.GetStatValue(StatDefOf.ComfyTemperatureMax, applyPostProcess: true, 1) + 10f;
            if (curTemp > maxSafeTemp - target.GetStatValue(StatDefOf.Insulation_Heat))
            {
                //Log.Message("Too hot");
                RecoverFromState();
                return;
            }

            base.MentalStateTick();
        }

    }
}
