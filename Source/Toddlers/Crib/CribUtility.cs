using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Toddlers
{
    public static class CribUtility
    {
        public static bool InCrib(Pawn p)
        {
            if (!(p.ParentHolder is Map) || p.pather.Moving) return false;

            if (GetCurrentCrib(p) == null) return false;
            else return true;
        }

        public static Building_Bed GetCurrentCrib(Pawn p)
        {
            if (!p.Spawned) return null;
            Building_Bed bed = null;
            List<Thing> thingList = p.Position.GetThingList(p.Map);
            for (int i = 0; i < thingList.Count; i++)
            {
                bed = thingList[i] as Building_Bed;
                if (bed != null && IsCrib(bed)) return bed;
            }
            return null;
        }

        public static bool IsCrib(Building_Bed bed)
        {
            if (bed == null) return false;
            ThingDef bedDef = bed.def;
            if (bedDef.defName.Contains("Crib") || bedDef.defName.Contains("crib")
                || bedDef.label.Contains("Crib") || bedDef.label.Contains("crib")
                || bedDef.defName.Contains("Cradle") || bedDef.defName.Contains("cradle")
                || bedDef.label.Contains("Cradle") || bedDef.label.Contains("cradle")
                )
            {
                return true;
            }
            return false;
        }

    }
}
