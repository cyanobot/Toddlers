using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Toddlers
{
    class ThinkNode_ConditionalNoTemperatureInjury : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            return !pawn.health.hediffSet.HasTemperatureInjury(TemperatureInjuryStage.Initial);
        }
    }
}
