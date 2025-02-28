using RimWorld;
using RimWorld.BaseGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static HarmonyLib.Code;
using static Toddlers.Patch_DBH;

namespace Toddlers
{ 
    public class StatPart_BabyHygiene : StatPart
    {
        public override string ExplanationPart(StatRequest req)
        {
            if (req.HasThing && req.Thing is Pawn pawn)
            {
                if (!ShouldTransform(pawn)) return null;

                StringBuilder sb = new StringBuilder();

                float bladderMult = BabyBladderMultipliler(pawn);
                if (bladderMult != 1f)
                {
                    //StatsReport_BabyHygiene_BladderMultiplier
                    sb.AppendLine("Baby".Translate().CapitalizeFirst() + ": x" + bladderMult.ToStringPercent());
                }

                float bedMult = BedMultiplier(pawn);
                if (bedMult != 1f)
                {
                    sb.AppendLine("StatsReport_InBed".Translate() + ": x" + bedMult.ToStringPercent());
                }
                else
                {
                    float playOfs = DirtyPlayOffset(pawn);
                    if (playOfs != 0f)
                    {
                        sb.AppendLine("StatsReport_BabyHygiene_DirtyPlayOffset".Translate() + ": +" + playOfs.ToStringPercent());
                    }
                }
                                
                string str = sb.ToStringSafe();
                str = str.TrimEndNewlines();
                if (str == "") return null;

                return str;
            }
            return null;
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            //LogUtil.DebugLog("StatParT_BabyHygiene.TransformValue - req: " + req);
            if (req.HasThing && req.Thing is Pawn pawn)
            {
                if (!ShouldTransform(pawn)) return;
                val *= BabyBladderMultipliler(pawn);
                float bedMult = BedMultiplier(pawn);
                val *= bedMult;
                if (bedMult == 1f)
                {
                    val += DirtyPlayOffset(pawn);
                }
            }
            
        }

        public bool ShouldTransform(Pawn pawn)
        {
            if (!babyHygiene) return false;
            if (pawn.DevelopmentalStage != Verse.DevelopmentalStage.Baby) return false;
            return true;
        }

        public float BabyBladderMultipliler(Pawn pawn)
        {
            if (babyBladder) return 0.4f;
            else return 1f;
        }

        public float BedMultiplier(Pawn pawn)
        {
            Building_Bed bed = pawn.CurrentBed();
            if (bed != null
                && bed.def != ThingDefOf.SleepingSpot
                && bed.def.defName != "DoubleSleepingSpot"
                && bed.def.defName != "BabySleepingSpot")
            {
                return 0.8f;
            }
            else return 1f;
        }

        public float DirtyPlayOffset(Pawn pawn)
        {
            if (pawn.Spawned
                && ToddlerPlayUtility.IsToddlerPlaying(pawn)
                && IsFloorDirty(pawn.Position, pawn.Map))
            {
                return 0.2f;
            }
            else return 0f;
        }

        public static bool IsFloorDirty(IntVec3 c, Map map)
        {
            TerrainDef terrainDef = c.GetTerrain(map);
            if (terrainDef.generatedFilth != null) return true;

            //LogUtil.DebugLog("GetThingList at " + c + ": " + c.GetThingList(map).ToStringSafeEnumerable());

            if (c.GetThingList(map).Any(
                t => t is Filth
                ))
            {
                return true;
            }
            return false;
        }
    }
}
