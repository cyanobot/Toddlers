using AlienRace;
using AlienRace.ExtendedGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Toddlers
{
    public static class HARCompat
    {
        public const string HARHarmonyID = "rimworld.erdelf.alien_race.main";
        public static Dictionary<ThingDef_AlienRace,AlienRaceToddlerInfo> alienRaceInfo = new Dictionary<ThingDef_AlienRace, AlienRaceToddlerInfo>();

        public static List<ThingDef_AlienRace> addedHumanlikeLifestage = new List<ThingDef_AlienRace>();
        public static List<ThingDef_AlienRace> createdNewLifestage = new List<ThingDef_AlienRace>();
        public static Dictionary<ThingDef_AlienRace, AlienRaceSkipReason> skipped = new Dictionary<ThingDef_AlienRace, AlienRaceSkipReason>();

        public static void Init()
        {
            List<ThingDef_AlienRace> races = DefDatabase<ThingDef_AlienRace>.AllDefsListForReading;

            addedHumanlikeLifestage.Clear();
            createdNewLifestage.Clear();
            skipped.Clear();

            foreach (ThingDef_AlienRace race in races)
            {
                try
                {
                    AlienRaceToddlerInfo toddlerInfo = new AlienRaceToddlerInfo(race);
                    alienRaceInfo.Add(race, toddlerInfo);
                }
                catch (Exception e)
                {
                    Log.Error("[Toddlers] Init for alien race " + race.defName + " threw an error: " + e.Message + ", StackTrace: " + e.StackTrace);
                }
            }

            StringBuilder sb_addedHumanlike = new StringBuilder($"[Toddlers] Added lifestage HumanlikeToddler to {addedHumanlikeLifestage.Count} races");
            if (addedHumanlikeLifestage.Count > 0)
            {
                sb_addedHumanlike.Append(": ");
                foreach (ThingDef_AlienRace race in addedHumanlikeLifestage)
                {
                    sb_addedHumanlike.AppendInNewLine($"{race.label} ({race.defName})");
                }
            }
            Log.Message(sb_addedHumanlike.ToString());

            StringBuilder sb_createdNew = new StringBuilder($"[Toddlers] Created new toddler lifestage for {createdNewLifestage.Count} races");
            if (createdNewLifestage.Count > 0)
            {
                sb_createdNew.Append(": ");
                foreach (ThingDef_AlienRace race in createdNewLifestage)
                {
                    sb_createdNew.AppendInNewLine($"{race.label} ({race.defName})");
                }
            }
            Log.Message(sb_createdNew.ToString());

            StringBuilder sb_skipped = new StringBuilder($"[Toddlers] Skipped {skipped.Count} races");
            if (sb_skipped.Length > 0)
            {
                sb_skipped.Append(": ");
                foreach (KeyValuePair<ThingDef_AlienRace, AlienRaceSkipReason> kvp in skipped)
                {
                    sb_skipped.AppendInNewLine($"{kvp.Key.label} ({kvp.Key.defName}) : {SkipReasonString(kvp.Value)}");
                }
            }
            Log.Message(sb_skipped.ToString());
        }

        public static string SkipReasonString(AlienRaceSkipReason reason)
        {
            switch (reason)
            {
                case AlienRaceSkipReason.AlreadyHasToddler:
                    return "found pre-generated life stage HumanlikeToddler";
                case AlienRaceSkipReason.NotHumanlikeLifestages:
                    return "could not identify baby and child life stages";
                case AlienRaceSkipReason.GrowsTooFast:
                    return "no room for at least 1yr toddlerhood";
                default:
                    return "unknown reason";
            }
        }
    }
}
