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
    class Hediff_ToddlerLonely : HediffWithComps
    {
        public override bool Visible => false;

        public override string SeverityLabel
        {
            get
            {
                if (Severity == 0f)
                {
                    return null;
                }
                return Severity.ToStringPercent();
            }
        }

        public override bool ShouldRemove
        {
            get
            {
                if (!ToddlerUtility.IsToddler(pawn)) return true;
                return base.ShouldRemove;
            }
        }

#if RW_1_5
        public override void Tick()
        {
            base.Tick();
            if (pawn.IsHashIntervalTick(200))
            {
                //at 1yo, rateFromAge = 1, at 3yo = 0.4
                float rateFromAge = 1f - (0.6f * ToddlerUtility.PercentGrowth(pawn));
                
                Severity += ToddlerPlayUtility.BaseLonelinessRate * rateFromAge * Toddlers_Settings.lonelinessGainFactor;
            }
        }
#else
        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (pawn.IsHashIntervalTick(200, delta))
            {
                //at 1yo, rateFromAge = 1, at 3yo = 0.4
                float rateFromAge = 1f - (0.6f * ToddlerUtility.PercentGrowth(pawn));

                Severity += delta * ToddlerPlayUtility.BaseLonelinessRate * rateFromAge * Toddlers_Settings.lonelinessGainFactor;
            }
        }
#endif
    }
}
