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
    abstract class Hediff_ToddlerLearning : HediffWithComps
    {
        private const int updateInterval = 2500; //1h

        public abstract string SettingName { get; }

        public override bool ShouldRemove => Severity >= 1f | !ToddlerUtility.IsToddler(this.pawn);

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

        public override void Tick()
        {
            base.Tick();
            if (pawn.IsHashIntervalTick(updateInterval))
            {
                float factor = updateInterval;
                InnerTick(factor);
            }
        }

        public void InnerTick(float factor)
        {
            int prevStage = CurStageIndex;

            this.OnUpdate(CurStageIndex);

            //Log.Message("InnerTick for " + pawn + ", GetLearningPerTickBase: " + ToddlerUtility.GetLearningPerTickBase(pawn));

            Severity += ToddlerUtility.GetLearningPerTickBase(pawn) * factor * (1/ (float)typeof(Toddlers_Settings).GetField(SettingName, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).GetValue(null));

            if (CurStageIndex != prevStage)
            {
                this.OnStageUp(CurStageIndex);
            }
          
        }

        public virtual void OnUpdate(int stageIndex) { }

        public virtual void OnStageUp(int newStageIndex) { }

    }
}
