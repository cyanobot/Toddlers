using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

#if RW_1_5
#else

namespace Toddlers
{
    public class FloatMenuOptionProvider_WashBaby : FloatMenuOptionProvider
    {
        protected override bool Drafted => true;

        protected override bool Undrafted => true;

        protected override bool Multiselect => false;

        protected override bool RequiresManipulation => false;

        protected override bool MechanoidCanDo => false;

        public override bool SelectedPawnValid(Pawn pawn, FloatMenuContext context)
        {
            LogUtil.DebugLog($"FloatMenuOptionProvider_WashBaby.SelectedPawnValid - pawn: {pawn}");
            if (!base.SelectedPawnValid(pawn, context)) return false;

            if (ToddlerUtility.IsToddler(pawn)) return false;

            LogUtil.DebugLog("FloatMenuOptionProvider_WashBaby.SelectedPawnValid returning true");
            return true;
        }

        public override bool TargetPawnValid(Pawn pawn, FloatMenuContext context)
        {
            LogUtil.DebugLog($"FloatMenuOptionProvider_WashBaby.TargetPawnValid - pawn: {pawn}");
            if (!base.TargetPawnValid(pawn, context)) return false;

            //LogUtil.DebugLog("FloatMenuOptionProvider_WashBaby.TargetPawnValid passed base checks");

            if (pawn.DevelopmentalStage != DevelopmentalStage.Baby) return false;
            //LogUtil.DebugLog("FloatMenuOptionProvider_WashBaby.TargetPawnValid target is baby");

            Need need_Hygiene = pawn.needs?.AllNeeds.Find(n => n.def.defName == "Hygiene");
            //LogUtil.DebugLog($"FloatMenuOptionProvider_WashBaby.TargetPawnValid need_Hygiene: {need_Hygiene}");
            if (need_Hygiene == null) return false;

            //LogUtil.DebugLog("FloatMenuOptionProvider_WashBaby.TargetPawnValid returning true");
            return true;
        }

        protected override bool AppliesInt(FloatMenuContext context)
        {
            //LogUtil.DebugLog($"FloatMenuOptionProvider_WashBaby.AppliesInt - DBHLoaded: {Toddlers_Mod.DBHLoaded}," +
            //    $", babyHygiene: {Patch_DBH.babyHygiene}");
            return Toddlers_Mod.DBHLoaded && Patch_DBH.babyHygiene;
        }

        protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
        {
            //LogUtil.DebugLog($"FloatMenuOptionProvider_WashBaby.GetSingleOptionFor - pawn: {clickedPawn}" +
            //    $", context: {context}");

            Need need_Hygiene = clickedPawn.needs?.AllNeeds.Find(n => n.def.defName == "Hygiene");
            //LogUtil.DebugLog($"FloatMenuOptionProvider_WashBaby.GetSingleOptionFor need_Hygiene: {need_Hygiene}" +
            //    $", + CurLevel: {need_Hygiene.CurLevel}");
            if (need_Hygiene != null && need_Hygiene.CurLevel <= 0.3f)
            {
                FloatMenuOption washOption = new FloatMenuOption("Wash".Translate() + " " + clickedPawn.LabelShort, null);

                if (context.FirstSelectedPawn.WorkTagIsDisabled(WorkTags.Caring))
                {
                    //LogUtil.DebugLog($"FloatMenuOptionProvider_WashBaby.GetSingleOptionFor - caring disabled for {context.FirstSelectedPawn}");
                    washOption.Label += ": " + "CannotPrioritizeWorkTypeDisabled".Translate(WorkTypeDefsUtility.LabelTranslated(WorkTags.Caring));
                    return washOption;
                }

                Job washJob = WashBabyUtility.GetWashJob(context.FirstSelectedPawn, clickedPawn);

                //LogUtil.DebugLog($"FloatMenuOptionProvider_WashBaby.GetSingleOptionFor - washJob: {washJob}");
                if (washJob != null)
                {
                    washJob.playerForced = true;
                    washJob.count = 1;
                    washOption.action = delegate
                        {
                            clickedPawn.SetForbidden(value: false, warnOnFail: false);
                            context.FirstSelectedPawn.Reserve(clickedPawn, washJob, ignoreOtherReservations: false);
                            context.FirstSelectedPawn.jobs.TryTakeOrderedJob(washJob, JobTag.Misc);
                        };
                    return FloatMenuUtility.DecoratePrioritizedTask(washOption, context.FirstSelectedPawn, clickedPawn);
                }
            }

            return null;
        }

    }
}
#endif