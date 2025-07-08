using HarmonyLib;
using RimWorld;
using System.Reflection;
using Verse;

namespace Toddlers
{
    [HarmonyPatch(typeof(Pawn_StoryTracker))]
    class WorkTags_Patch
    {
        public static MethodBase TargetMethod()
        {
            return typeof(Pawn_StoryTracker).GetProperty(nameof(Pawn_StoryTracker.DisabledWorkTagsBackstoryTraitsAndGenes), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(false);
        }

        static WorkTags Postfix(WorkTags worktags, Pawn ___pawn)
        {
            //AllWork catches most things, and should catch most modded worktypes as well
            //violent stops eg equipping weapons, manning mortars
            if (ToddlerUtility.IsToddler(___pawn))
            {
                worktags |= WorkTags.AllWork | WorkTags.Violent;
                //Log.Message("Fired DisabledWorkTagsBackstoryTraitsAndGenes for pawn: " + ___pawn + ", worktags: " + worktags.ToString());
            }               
            return worktags;
        }
    }

    
}