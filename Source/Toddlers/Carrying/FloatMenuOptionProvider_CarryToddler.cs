using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;
using static Toddlers.ToddlerUtility;

namespace Toddlers
{
#if RW_1_5
#else
    public class FloatMenuOptionProvider_CarryToddler : FloatMenuOptionProvider
    {
        protected override bool Drafted => true;

        protected override bool Undrafted => true;

        protected override bool Multiselect => false;

        protected override bool RequiresManipulation => true;

        protected override bool AppliesInt(FloatMenuContext context)
        {
            if (context.FirstSelectedPawn.IsMutant)
            {
                return !context.FirstSelectedPawn.mutant.Def.canCarryPawns;
            }
            return true;
        }

        protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
        {
            if (!IsToddler(clickedPawn))
            {
                return null;
            }
            if (IsToddler(context.FirstSelectedPawn))
            {
                return null;
            }
            if (!context.FirstSelectedPawn.CanReach(clickedPawn, PathEndMode.ClosestTouch, Danger.Deadly))
            {
                return new FloatMenuOption("CannotCarry".Translate(clickedPawn) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
            }
            if (!context.FirstSelectedPawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                return new FloatMenuOption("CannotCarry".Translate(context.FirstSelectedPawn) + ": " + "Incapable".Translate().CapitalizeFirst(), null);
            }
            return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Carry".Translate(clickedPawn), delegate
            {
                clickedPawn.SetForbidden(value: false, warnOnFail: false);
                Job job = JobMaker.MakeJob(Toddlers_DefOf.CarryToddler, clickedPawn);
                job.count = 1;
                context.FirstSelectedPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }), context.FirstSelectedPawn, clickedPawn);
        }
    }
#endif
}
