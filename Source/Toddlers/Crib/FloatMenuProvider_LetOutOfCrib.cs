using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using static Toddlers.ToddlerUtility;

namespace Toddlers
{
    public class FloatMenuOptionProvider_LetOutOfCrib : FloatMenuOptionProvider
    {
        protected override bool Drafted => true;
        protected override bool Undrafted => true;
        protected override bool Multiselect => false;
        protected override bool RequiresManipulation => true;
        protected override bool MechanoidCanDo => true;
        protected override bool IgnoreFogged => false;

        public override bool SelectedPawnValid(Pawn pawn, FloatMenuContext context)
        {
            if (!base.SelectedPawnValid(pawn, context)) return false;
            if (IsToddler(pawn)) return false;
            return true;
        }

        public override bool TargetPawnValid(Pawn pawn, FloatMenuContext context)
        {
            if (!base.TargetPawnValid(pawn, context)) return false;
            if (!IsLiveToddler(pawn)) return false;
            return true;
        }

        protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
        {
            if (CribUtility.InCrib(clickedPawn) && ToddlerLearningUtility.IsCrawler(clickedPawn))
            {
                FloatMenuOption letOutOfCrib = new FloatMenuOption("LetOutOfCrib".Translate(clickedPawn), delegate
                {
                    Building_Bed crib = CribUtility.GetCurrentCrib(clickedPawn);
                    if (crib == null) return;
                    Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("LetOutOfCrib"), clickedPawn, crib);
                    job.count = 1;
                    context.FirstSelectedPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }, MenuOptionPriority.Default);
                return letOutOfCrib;
            }
            else return null;
        }
    }
}
