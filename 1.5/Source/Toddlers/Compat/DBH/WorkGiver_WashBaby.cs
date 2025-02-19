using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;
using static Toddlers.Patch_DBH;
using static Toddlers.LogUtil;

namespace Toddlers
{
    public class WorkGiver_WashBaby : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            if (!babyHygiene) yield break;

            foreach (Pawn item in pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (!item.DestroyedOrNull()
                    && item.Spawned
                    && !item.Dead
                    && item.DevelopmentalStage == DevelopmentalStage.Baby
                    && item.RaceProps.Humanlike
                    && item.needs != null
                    )
                {
                    //DebugLog("WorkGiver_WashBaby PotentialWorkThingsGlobal: " + item);
                    yield return item;
                }
                
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {

            if (!(t is Pawn baby) || pawn == baby) return false;

            if (!pawn.CanReserve(baby, 1, -1, null, forced)) return false;

            Need need_Hygiene = baby.needs.AllNeeds.Find(n => n.def.defName == "Hygiene");
            if (need_Hygiene == null || need_Hygiene.CurLevel > 0.3f) return false;

            if (!ShouldWashNow(baby)) return false;

            if (TryRunJob(pawn, baby) == null) return false;

            return true;
        }

        public static bool ShouldWashNow(Pawn baby)
        {
            if (!ChildcareUtility.CanSuckleNow(baby, out var _)) return false;
            if (baby.Faction != Faction.OfPlayer && baby.HostFaction != Faction.OfPlayer) return false;

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            DebugLog("WorkGiver_WashBaby JobOnThing - "
                + "pawn: " + pawn
                + ", t: " + t
                );

            if (!(t is Pawn baby) || baby == pawn)
            {
                return null;
            }
            return TryRunJob(pawn, baby);
        }

        public Job TryRunJob(Pawn pawn, Pawn baby)
        {
                        LocalTargetInfo targetB = null;
            targetB = pawn.inventory.innerContainer.FirstOrDefault((Thing x) => x.def.defName == "DBH_WaterBottle");

            JobDef washDef = DefDatabase<JobDef>.GetNamed("CYB_WashBaby");

            if (targetB.IsValid && targetB.HasThing)
            {
                Job job = JobMaker.MakeJob(washDef, baby, targetB);
                job.count = 1;
                return job;
            }
            targetB = (LocalTargetInfo)m_FindBestCleanWaterSource.Invoke(null,
                new object[] { pawn, baby, false, 9999f, null, null });
            if (targetB == null || !targetB.IsValid)
            {
                return null;
            }
            if (targetB.HasThing)
            {
                /*
                if (targetB.Thing.def.HasModExtension<WaterExt>())
                {
                    Log.Warning("Returned drink for washPatient, this shouldn't happen");
                    return null;
                }
                */
                return JobMaker.MakeJob(washDef, baby, targetB.Thing);
            }
            if (targetB.Cell.IsValid)
            {
                return JobMaker.MakeJob(washDef, baby, targetB.Cell);
            }
            return null;
        }
    }
}
