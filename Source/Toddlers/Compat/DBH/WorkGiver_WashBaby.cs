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
using static Toddlers.WashBabyUtility;

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

            if (!(t is Pawn baby)) return false;

            if (!CanWashNow(pawn, baby, forced)) return false;
            if (GetWashJob(pawn, baby) == null) return false;

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            /*
            DebugLog("WorkGiver_WashBaby JobOnThing - "
                + "pawn: " + pawn
                + ", t: " + t
                );
            */

            if (!(t is Pawn baby) || baby == pawn)
            {
                return null;
            }
            return GetWashJob(pawn, baby);
        }

        /*
        public static Job TryRunJob(Pawn pawn, Pawn baby)
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
                return JobMaker.MakeJob(washDef, baby, targetB.Thing);
            }
            if (targetB.Cell.IsValid)
            {
                return JobMaker.MakeJob(washDef, baby, targetB.Cell);
            }
            return null;
        }
        */
    }
}
