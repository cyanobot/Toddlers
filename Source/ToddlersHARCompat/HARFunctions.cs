using AlienRace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Toddlers
{
    public static class HARFunctions
    {
        public static float HARToddlerMinAge(Pawn p)
        {
            if (p.def is ThingDef_AlienRace alienDef)
            {
                AlienRaceToddlerInfo toddlerInfo = HARCompat.alienRaceInfo.TryGetValue(alienDef);
                if (toddlerInfo != null) return toddlerInfo.toddlerMinAge;
            }
            return ToddlerUtility.BASE_MIN_AGE;
        }

        public static float HARToddlerEndAge(Pawn p)
        {
            if (p.def is ThingDef_AlienRace alienDef)
            {
                AlienRaceToddlerInfo toddlerInfo = HARCompat.alienRaceInfo.TryGetValue(alienDef);
                if (toddlerInfo != null) return toddlerInfo.toddlerEndAge;
            }
            return ToddlerUtility.BASE_END_AGE;
        }

        public static bool HasHumanlikeGait(Pawn p)
        {
            if (p.def is ThingDef_AlienRace alienDef)
            {
                AlienRaceToddlerInfo toddlerInfo = HARCompat.alienRaceInfo.TryGetValue(alienDef);
                if (toddlerInfo != null) return toddlerInfo.humanlikeGait;
            }
            if (p.RaceProps.Humanlike) return true;
            else return false;
        }
    }
}
