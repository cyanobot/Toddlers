using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;
using static Toddlers.LogUtil;
using static Toddlers.Patch_DBH;

namespace Toddlers
{
    public static class WashBabyUtility
    {
        public static Need HygieneNeedFor(Pawn pawn)
        {
            return pawn?.needs?.TryGetNeed(DBHDefOf.Hygiene);
        }

        public static bool CanWashNow(Pawn carer, Pawn baby, bool forceReserve = false)
        {
            if (carer == null || baby == null || carer == baby) return false;
            if (!carer.CanReserve(baby, 1, -1, null, forceReserve)) return false;

            if (!NeedsWashNow(baby)) return false;
            if (ToddlerUtility.IsBabyBusy(baby)) return false;

            return true;
        }

        public static bool NeedsWashNow(Pawn baby)
        {
            if (!ColonistShouldWash(baby)) return false;

            Need need_Hygiene = HygieneNeedFor(baby);
            if (need_Hygiene == null || need_Hygiene.CurLevel > 0.3f) return false;

            return true;
        }

        public static bool ColonistShouldWash(Pawn baby)
        {
            if (!ChildcareUtility.CanSuckle(baby, out var _)) return false;
            if (baby.Faction != Faction.OfPlayer && baby.HostFaction != Faction.OfPlayer) return false;

            return true;
        }

        public static Pawn FindDirtyBaby(Pawn pawn)
        {
            foreach (Pawn item in pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (item.needs != null
                    && item.mindState.AutofeedSetting(pawn) == AutofeedMode.Urgent
                    )
                {
                    DebugLog("item " + item + " passed autofeed check");

                    if (!CanWashNow(pawn, item)) continue;

                    return item;
                }
            }
            return null;
        }
    
        public static Job GetWashJob(Pawn carer, Pawn baby, bool allowBath=true)
        {
            //check for bathtub if toddler also wants to play or in 20% of cases
            
            if (allowBath && ToddlerUtility.IsToddler(baby) && !HealthAIUtility.ShouldSeekMedicalRest(baby))
            {
                Need_Play needPlay = baby.needs?.play;
                if ((needPlay != null && needPlay.CurLevelPercentage < 0.7f) || Rand.Value < 0.2f)
                {
                    Thing bath;
                    if (FindBathOrTub(carer, baby, out bath))
                    {
                        Job bathJob = JobMaker.MakeJob(DBHDefOf.CYB_BatheToddler, baby, bath);
                        bathJob.count = 1;
                        return bathJob;
                    }
                    /*
                    object obj_bestHygieneSource = (LocalTargetInfo)m_FindBestHygieneSource.Invoke(null, new object[] { carer, false, 100f });
                    if (obj_bestHygieneSource != null)
                    {
                        LocalTargetInfo lti_bestHygieneSource = (LocalTargetInfo)obj_bestHygieneSource;
                        if (lti_bestHygieneSource.IsValid
                            && lti_bestHygieneSource.HasThing
                            && (lti_bestHygieneSource.Thing.GetType() == t_Building_bath
                                || lti_bestHygieneSource.Thing.GetType() == t_Building_washbucket))
                        {
                            //bath job here
                            
                        }
                    }
                    */
                }
            }

            //if infant or doesn't need play or no bath available
            //first priority is inventory water
            LocalTargetInfo targetB = null;
            targetB = carer.inventory.innerContainer.FirstOrDefault((Thing x) => x.def.defName == "DBH_WaterBottle");

            if (targetB.IsValid && targetB.HasThing)
            {
                return JobMaker.MakeJob(DBHDefOf.CYB_WashBaby, baby, targetB);
            }

            //next priority available clean water source
            targetB = (LocalTargetInfo)m_FindBestCleanWaterSource.Invoke(null,
                new object[] { carer, baby, false, 9999f, null, null });
            if (targetB == null || !targetB.IsValid)
            {
                return null;
            }
            if (targetB.HasThing)
            {
                return JobMaker.MakeJob(DBHDefOf.CYB_WashBaby, baby, targetB.Thing);
            }
            if (targetB.Cell.IsValid)
            {
                return JobMaker.MakeJob(DBHDefOf.CYB_WashBaby, baby, targetB.Cell);
            }
            return null;
        }
    
        public static bool FindBathOrTub(Pawn carer, Pawn baby, out Thing bath)
        {
            bath = null;

            if (carer?.Map == null) return false;

            List<Thing> allFixtures = ((IEnumerable<Thing>)m_AllFixtures.Invoke(null, new object[] { carer.Map })).ToList();
            if (allFixtures.NullOrEmpty()) return false;

            List<Thing> possibleBaths = new List<Thing>();
            foreach (Thing fixture in allFixtures)
            {
                if ((fixture.GetType() == t_Building_bath || fixture.GetType() == t_Building_washbucket)
                    && (bool)m_IsEverUsable.Invoke(null, new object[] { fixture, carer, baby, false, null, true })
                    && !fixture.IsForbidden(baby)
                    && (bool)m_UsableNow.Invoke(null, new object[] { fixture, carer, false, 9999f })
                    )
                {
                    possibleBaths.Add(fixture);
                }
            }

            if (possibleBaths.NullOrEmpty()) return false;

            float bestScore = 9999f;
            foreach (Thing possibleBath in possibleBaths)
            {
                float score = (baby.PositionHeld - possibleBath.Position).LengthManhattan;
                if (possibleBath.GetType() == t_Building_washbucket) score += 30f;

                if (score < bestScore)
                {
                    bestScore = score;
                    bath = possibleBath;
                }
            }

            DebugLog("FindBathOrTub - carer: " + carer + ", baby: " + baby + ", found bath: " + bath);

            return (bath != null);
        }
    
    }
}
