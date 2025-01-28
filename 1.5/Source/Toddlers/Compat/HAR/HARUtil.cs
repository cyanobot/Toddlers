using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static Toddlers.HARCompat;

namespace Toddlers
{
    public static class HARUtil
    {
        public static AlienRace GetAlienRaceWrapper(Pawn pawn)
        {
            string defName = pawn.def.defName;
            if (alienRaces.ContainsKey(defName)) return alienRaces[defName];
            else return null;
        }

        //consider a pawn to most likely have humanlike gait
        //if it has exactly two legs and at least two arms
        public static bool HasHumanlikeGait(ThingDef def)
        {
            if (Toddlers_DefOf.HumanlikeGaitOverride.whitelist.Contains(def)) return true;
            if (Toddlers_DefOf.HumanlikeGaitOverride.blacklist.Contains(def)) return false;

            List<BodyPartRecord> parts = def?.race?.body?.AllParts;
            if (parts.NullOrEmpty()) return false;

            int legCount = parts.Where(x => IsLeg(x)).Count();
            if (legCount != 2) return false;
            int armCount = parts.Where(x => IsArm(x)).Count();
            if (armCount < 2) return false;
            return true;
        }

        public static bool IsLeg(BodyPartRecord partRecord)
        {
            if (partRecord.def == BodyPartDefOf.Leg) return true;
            if (partRecord.def.defName.Contains("leg") || partRecord.def.defName.Contains("Leg") || partRecord.def.defName.Contains("LEG")) return true;
            if (partRecord.Label.Contains("leg") || partRecord.def.defName.Contains("Leg") || partRecord.def.defName.Contains("LEG")) return true;
            else return false;
        }
        public static bool IsArm(BodyPartRecord partRecord)
        {
            if (partRecord.def == BodyPartDefOf.Arm) return true;
            if (partRecord.def.defName.Contains("arm") || partRecord.def.defName.Contains("Arm") || partRecord.def.defName.Contains("ARM")) return true;
            if (partRecord.Label.Contains("arm") || partRecord.def.defName.Contains("Arm") || partRecord.def.defName.Contains("ARM")) return true;
            else return false;
        }
    }
}
