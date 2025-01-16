using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toddlers
{

    [HarmonyPatch(typeof(FloatMenuMakerMap), "ChoicesAtFor")]
    class FloatMenu_Patch
    {
        public const bool LOG_FLOAT_MENU = false;

        public static void FloatMenuLog(string message)
        {
            if (LOG_FLOAT_MENU) LogUtil.DebugLog(message);
        }

        private static TargetingParameters ForToddler(Pawn pawn)
        {
            return new TargetingParameters
            {
                canTargetPawns = true,
                canTargetBuildings = false,
                canTargetAnimals = false,
                canTargetMechs = false,
                neverTargetHostileFaction = true,
                mapObjectTargetsMustBeAutoAttackable = false,
                validator = delegate (TargetInfo targ)
                {
                    if (!targ.HasThing) return false;
                    Pawn toddler = targ.Thing as Pawn;
                    if (toddler == null || toddler == pawn) return false;
                    return ToddlerUtility.IsLiveToddler(toddler);
                }
            };
        }

        private static TargetingParameters ForInfant(Pawn pawn)
        {
            return new TargetingParameters
            {
                canTargetPawns = true,
                canTargetBuildings = false,
                canTargetAnimals = false,
                canTargetMechs = false,
                neverTargetHostileFaction = true,
                mapObjectTargetsMustBeAutoAttackable = false,
                validator = delegate (TargetInfo targ)
                {
                    if (!targ.HasThing) return false;
                    Pawn infant = targ.Thing as Pawn;
                    if (infant == null || infant == pawn) return false;
                    if (!ChildcareUtility.CanSuckle(infant, out var _)) return false;
                    if (ToddlerUtility.IsToddler(infant)) return false;
                    return true;
                }
            };
        }

        private static TargetingParameters ForBaby(Pawn pawn)
        {
            return new TargetingParameters
            {
                canTargetPawns = true,
                canTargetBuildings = false,
                canTargetAnimals = false,
                canTargetMechs = false,
                neverTargetHostileFaction = true,
                mapObjectTargetsMustBeAutoAttackable = false,
                validator = delegate (TargetInfo targ)
                {
                    if (!targ.HasThing) return false;
                    Pawn baby = targ.Thing as Pawn;
                    if (baby == null || baby == pawn) return false;
                    return ChildcareUtility.CanSuckle(baby, out var _);
                }
            };
        }

        //copied from Dress Patient with modifications
        private static TargetingParameters ForApparel(LocalTargetInfo targetBaby)
        {
            return new TargetingParameters
            {
                canTargetItems = true,
                mapObjectTargetsMustBeAutoAttackable = false,
                validator = delegate (TargetInfo targ)
                {
                    if (!targ.HasThing) return false;
                    Apparel apparel = targ.Thing as Apparel;
                    //Log.Message("apparel : " + apparel.Label);
                    if (apparel == null) return false;
                    if (!targetBaby.HasThing) return false;
                    Pawn baby = targetBaby.Thing as Pawn;
                    //Log.Message("baby : " + baby.Name);
                    if (baby == null) return false;
                    //Log.Message("HasPartsToWear : " + ApparelUtility.HasPartsToWear(baby, apparel.def));
                    if (!apparel.PawnCanWear(baby) || !ApparelUtility.HasPartsToWear(baby, apparel.def)) return false;
                    return true;
                }
            };
        }

        static List<FloatMenuOption> Postfix(List<FloatMenuOption> opts, Pawn pawn, Vector3 clickPos)
        {
            //Log.Message("opts.Count = " + opts.Count);
            IntVec3 c = IntVec3.FromVector3(clickPos);
           
            //for non-toddlers
            if (pawn.DevelopmentalStage != DevelopmentalStage.Baby)
            {
                //have to be able to manipulate to do anything to a baby
                if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                {
                    //check all babies (infant and toddler) at targeted square
                    foreach (LocalTargetInfo localTargetInfo1 in GenUI.TargetsAt(clickPos, ForBaby(pawn), thingsOnly: true))
                    {
                        Pawn baby = (Pawn)localTargetInfo1.Thing;

                        //logic for all babies
                        FloatMenuLog("baby.IsForbidden: " + baby.IsForbidden(pawn)
                            + ", CanReserveAndReach: " + pawn.CanReserveAndReach(baby, PathEndMode.ClosestTouch, Danger.Deadly, ignoreOtherReservations: true)
                            );

                        if (baby.IsForbidden(pawn) ||
                            !pawn.CanReserveAndReach(baby, PathEndMode.ClosestTouch, Danger.Deadly, ignoreOtherReservations: true)) continue;

                        //pick up baby option
                        //should always be available not just when drafted
                        if (!pawn.Drafted)
                        {
                            FloatMenuOption carryOption = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Carry".Translate(baby), delegate
                            {
                                baby.SetForbidden(value: false, warnOnFail: false);
                                Job carryJob = JobMaker.MakeJob(JobDefOf.CarryDownedPawnDrafted, baby);
                                carryJob.playerForced = true;
                                carryJob.count = 1;
                                pawn.Reserve(baby, carryJob, ignoreOtherReservations: false);
                                pawn.jobs.TryTakeOrderedJob(carryJob, JobTag.Misc);
                            }), pawn, baby);
                            opts.Add(carryOption);
                        }

                        //return to crib/safe place options

                        //first: figure out what option are already in the list
                            //"rescue" and "putsomewheresafe" are alternate labels for the same option
                            //"putsomewheresafe" for infants who don't need medical rest, "rescue" if medical rest needed or not infant
                            //vanilla only targets downed pawns
                            //with Injured Carry may also target injured, or all
                        List<FloatMenuOption> rescueOpts = opts.FindAll(
                            x => x.Label.Contains("Rescue".Translate(baby.LabelCap, baby))
                            || x.Label.Contains("PutSomewhereSafe".Translate(baby.LabelCap, baby)));
                        //"carrytosafeplace" generated on any baby by calling SafePlaceForBaby
                            //only shows if valid and not current bed or spawned cell                 
                            //and not if baby to be taken to caravan
                            //but doesn't show if pawn drafted
                        List<FloatMenuOption> carrySafeOpts = opts.FindAll(x => x.Label.Contains("CarryToSafePlace".Translate(baby.Named("BABY"))));

                        bool cribOptionAccountedFor = false;
                        bool nonCribOptionAccountedFor = false;
                        bool removeAllCarrySafe = false;

                        FloatMenuLog("rescueOpts: " + rescueOpts.ToStringSafeEnumerable());
                        FloatMenuLog("carrySafeOpts: " + carrySafeOpts.ToStringSafeEnumerable());

                        //SafePlaceForBaby, if already called, will have done the logic
                        //unfortunately we can't tell what it returned without calling it again
                        LocalTargetInfo safePlace = ChildcareUtility.SafePlaceForBaby(baby, pawn, true);
                        FloatMenuLog("safePlace: " + safePlace);

                        if (!rescueOpts.NullOrEmpty())
                        {
                            //rescue option generated by vanilla
                            if (baby.Downed)
                            {
                                //vanilla option tries to assign MakeBringBabyToSafetyJob
                                //which calls SafePlaceForBaby
                                if (safePlace.HasThing && safePlace.Thing is Building_Bed) cribOptionAccountedFor = true;
                                else if (safePlace.IsValid) nonCribOptionAccountedFor = true;
                                FloatMenuLog("found vanilla rescue option");
                            }
                            //rescue option from Injured Carry or other
                            else
                            {
                                //Injured Carry only checks beds
                                    //if it successfully generated an option, it found a bed option
                                    //if it generated a disabled option, it ruled out beds
                                    //either way don't need to check again
                                cribOptionAccountedFor = true;
                                FloatMenuLog("found non-vanilla rescue option, crib accoutned for");
                            }

                            //don't need more than one rescue option if this has somehow happened
                            if (rescueOpts.Count > 1)
                            {
                                FloatMenuLog("found more than one rescue option, attempting to prune");
                                for (int i = 0; i < rescueOpts.Count; i++)
                                {
                                    if (i > 0) opts.Remove(rescueOpts[i]);
                                }
                            }
                        }

                        if (!carrySafeOpts.NullOrEmpty())
                        {
                            //if safeplace is not valid
                            //either there is no good option
                            //or baby is happy at current position
                            //only need to consider crib option
                            if (!safePlace.IsValid)
                            {
                                FloatMenuLog("safe place not valid, nonCribOption accounted for");
                                nonCribOptionAccountedFor = true;
                            }
                            //if safeplace returns a bed
                            else if (safePlace.HasThing && safePlace.Thing is Building_Bed)
                            {
                                FloatMenuLog("safe place is a bed");
                                //if rescue already accounted for bed, don't need a duplicate
                                if (cribOptionAccountedFor)
                                {
                                    FloatMenuLog("carrySafeOpts redundant");
                                    removeAllCarrySafe = true;
                                }
                                //otherwise we have now accounted for the bed
                                else
                                {
                                    FloatMenuLog("crib option accounted for");
                                    cribOptionAccountedFor = true;
                                }
                            }
                            //if safeplace returns a valid spot that is not a bed
                            else
                            {
                                FloatMenuLog("safe place is not a bed");
                                //if rescue already accounted for non-bed, don't need a duplicate
                                if (nonCribOptionAccountedFor)
                                {
                                    FloatMenuLog("carrySafeOpts redundant");
                                    removeAllCarrySafe = true;
                                }
                                //otherwise we have now accounted for a non-bed option
                                else
                                {
                                    FloatMenuLog("noncrib accounted for");
                                    nonCribOptionAccountedFor = true;
                                }
                            }

                            //remove redundant
                            if (removeAllCarrySafe)
                            {
                                FloatMenuLog("attempting to remove carrySafeOpts");
                                foreach (FloatMenuOption carrySafeOpt in carrySafeOpts)
                                {
                                    opts.Remove(carrySafeOpt);
                                }
                            }
                        }

                        FloatMenuLog("cribOptionAccountedFor: " + cribOptionAccountedFor
                            + ", nonCribOptionAccountedFor: " + nonCribOptionAccountedFor);
                        //add options that have not already been generated by vanilla/other mods
                        if (!cribOptionAccountedFor || !nonCribOptionAccountedFor)
                        {
                            
                            //LocalTargetInfo safePlace = ChildcareUtility.SafePlaceForBaby(baby, pawn, true);
                            //FloatMenuLog("safePlace: " + safePlace);
                            bool makeSafePlaceOption = false;
                            bool makeReturnToCribOption = false;
                            //Building_Bed cribToReturnTo = null;
                            if (!safePlace.IsValid)
                            {
                                FloatMenuLog("safePlace not valid");
                                //if safe place can't find anywhere either there are no options
                                //or the baby doesn't need to move

                                //definitely don't need to generate safeplace option
                                //but ought to consider possibility of returning to crib
                                if (!cribOptionAccountedFor && baby.CurrentBed() == null) makeReturnToCribOption = true;
                            }
                            else if (safePlace.HasThing && safePlace.Thing is Building_Bed safeBed)
                            {
                                FloatMenuLog("safePlace is bed");
                                //SafePlaceForBaby has considered whether crib is best, and thinks it is
                                //so only need return to one option
                                //and only then if it's not already accounted for
                                if (!cribOptionAccountedFor)
                                {
                                    makeSafePlaceOption = true;
                                    //cribToReturnTo = safeBed;
                                }
                            }
                            else
                            {
                                FloatMenuLog("safePlace is not bed");
                                //SafePlaceForBaby has generated non-crib option, should also consider option of returning to crib
                                if (!nonCribOptionAccountedFor) makeSafePlaceOption = true;
                                if (!cribOptionAccountedFor && baby.CurrentBed() == null) makeReturnToCribOption = true;
                            }

                            FloatMenuLog("makeSafePlaceOption: " + makeSafePlaceOption
                                + ", makeReturnToCribOption: " + makeReturnToCribOption);

                            if (makeSafePlaceOption)
                            {
                                FloatMenuLog("adding returnToSafety option");
                                FloatMenuOption returnToSafety = new FloatMenuOption("PutSomewhereSafe".Translate(baby.LabelCap, baby), delegate
                                {
                                    pawn.jobs.TryTakeOrderedJob(ChildcareUtility.MakeBringBabyToSafetyJob(pawn, baby), JobTag.Misc);
                                }, MenuOptionPriority.RescueOrCapture, null, baby);
                                opts.Add(FloatMenuUtility.DecoratePrioritizedTask(returnToSafety, pawn, baby));
                            }

                            if (makeReturnToCribOption)
                            {
                                //don't need return to crib if we're already in (appropriate) crib
                                //Building_Bed foundBed = cribToReturnTo ?? RestUtility.FindBedFor(baby, pawn, checkSocialProperness: true);
                                Building_Bed foundBed = RestUtility.FindBedFor(baby, pawn, checkSocialProperness: true);
                                FloatMenuLog("considering returnToCrib option - foundBed: " + foundBed
                                    + ", CurrentBed: " + baby.CurrentBed());
                                if (baby.CurrentBed() == null
                                    || (foundBed != null && foundBed != baby.CurrentBed()))
                                {
                                    FloatMenuLog("making returnToCrib option");
                                    FloatMenuOption putInCrib = new FloatMenuOption("PutInCrib".Translate(baby), delegate
                                    {
                                        Building_Bed building_Bed = RestUtility.FindBedFor(baby, pawn, checkSocialProperness: true);
                                        if (building_Bed == null)
                                        {
                                            building_Bed = RestUtility.FindBedFor(baby, pawn, checkSocialProperness: true, ignoreOtherReservations: true);
                                        }
                                        if (building_Bed == null)
                                        {
                                            Messages.Message("CannotRescue".Translate() + ": " + "NoCrib".Translate(), baby, MessageTypeDefOf.RejectInput, historical: false);
                                        }
                                        else
                                        {
                                            //DefDatabase<JobDef>.GetNamed("PutInCrib")
                                            Job job = JobMaker.MakeJob(Toddlers_DefOf.PutInCrib, baby, building_Bed);
                                            job.count = 1;
                                            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                        }
                                    }, MenuOptionPriority.RescueOrCapture, null, baby);

                                    if (foundBed == null)
                                    {
                                        putInCrib.Label += " : " + "NoCrib".Translate();
                                        putInCrib.Disabled = true;
                                    }
                                    else if (!GenTemperature.SafeTemperatureAtCell(baby, foundBed.Position, baby.MapHeld))
                                    {
                                        putInCrib.Label += " : " + "BadTemperature".Translate();
                                    }

                                    opts.Add(FloatMenuUtility.DecoratePrioritizedTask(putInCrib, pawn, baby));
                                }
                            }

                            if (!makeReturnToCribOption && !makeSafePlaceOption && !cribOptionAccountedFor && !nonCribOptionAccountedFor)
                            {
                                FloatMenuLog("making disabled option");
                                //no good options, but should give feedback to the player
                                FloatMenuOption disabledOption = new FloatMenuOption(
                                    "CannotCarryToSafePlace".Translate()
                                    , null, MenuOptionPriority.DisabledOption);
                                disabledOption.Disabled = true;

                                opts.Add(FloatMenuUtility.DecoratePrioritizedTask(disabledOption, pawn, baby));
                            }
                        }


                        //options for dressing and undressing babies and toddlers

                        //patch for Dress Patients to avoid duplicate menu options
                        //check the same logic as Dress Patients to figure out if that mod will be generating a menu option 
                        if (Toddlers_Mod.dressPatientsLoaded
                            && baby.InBed()
                            && (baby.Faction == Faction.OfPlayer || baby.HostFaction == Faction.OfPlayer)
                            && (baby.guest != null ? pawn.guest.ExclusiveInteractionMode != PrisonerInteractionModeDefOf.Execution : true)
                            && HealthAIUtility.ShouldSeekMedicalRest(baby)
                            )
                        {
                            //do nothing, Dress Patients has this covered
                            ;
                        }
                        else if (pawn.CanReserveAndReach(baby, PathEndMode.ClosestTouch, Danger.None, 1, -1, null, ignoreOtherReservations: true))
                        {
                            FloatMenuOption dressBaby = new FloatMenuOption("DressBaby".Translate(baby), delegate ()
                            {
                                Find.Targeter.BeginTargeting(ForApparel(baby), (LocalTargetInfo targetApparel) =>
                                {
                                    //Log.Message("pawn : " + pawn.Name);
                                    //Log.Message("baby : " + baby.Name);
                                    //Log.Message("apparel : " + targetApparel.Label);
                                    Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("DressBaby"), baby, targetApparel);
                                    targetApparel.Thing.SetForbidden(false);
                                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                });
                            }, MenuOptionPriority.High);
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(dressBaby, pawn, baby));
                        }


                        //logic for toddlers
                        if (ToddlerUtility.IsLiveToddler(baby))
                        {
                            //option to let crawlers out of their cribs
                            if (CribUtility.InCrib(baby) && ToddlerLearningUtility.IsCrawler(baby))
                            {
                                FloatMenuOption letOutOfCrib = new FloatMenuOption("LetOutOfCrib".Translate(baby), delegate
                                {
                                    Building_Bed crib = CribUtility.GetCurrentCrib(baby);
                                    if (crib == null) return;
                                    Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("LetOutOfCrib"), baby, crib);
                                    job.count = 1;
                                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                }, MenuOptionPriority.Default);
                                opts.Add(FloatMenuUtility.DecoratePrioritizedTask(letOutOfCrib, pawn, baby));
                            }
                        }
                        //logic for infants
                        else
                        {
                            //none at this time
                            ;
                        }
                    }
                
                    //option to put baby down if carrying
                    if (pawn.IsCarryingPawn())
                    {
                        Pawn baby = (Pawn)pawn.carryTracker.CarriedThing;
                        if (baby.DevelopmentalStage == DevelopmentalStage.Baby)
                        {
                            IntVec3 clickCell = IntVec3.FromVector3(clickPos);
                            if (clickCell.IsValid && clickCell.Standable(pawn.Map))
                            {
                                FloatMenuOption dropOption = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Drop".Translate(pawn, baby), delegate
                                {
                                    Job dropJob = JobMaker.MakeJob(JobDefOf.HaulToCell, baby);
                                    dropJob.targetA = baby;
                                    dropJob.targetB = clickCell;
                                    dropJob.playerForced = true;
                                    dropJob.count = 1;
                                    pawn.jobs.TryTakeOrderedJob(dropJob, JobTag.Misc);
                                }), pawn, clickCell);
                                opts.Add(dropOption);
                            }
                            
                        }
                       
                    }
                }
            }
            

            //for toddlers, mostly disabling/removing options for things they can't do
            else if (ToddlerUtility.IsLiveToddler(pawn))
            {
                //int n = opts.RemoveAll(x => x.Label.Contains(pawn.LabelShort));
                opts.RemoveAll(x => x.revalidateClickTarget == pawn || x.Label.Contains(pawn.LabelShort) || x.Label.Contains(pawn.LabelShortCap));

                foreach (Thing t in c.GetThingList(pawn.Map))
                {                                  
                    if (t.def.IsApparel && !ToddlerLearningUtility.CanDressSelf(pawn))
                    {
                        //copied directly from source
                        //this will allow us to identify the menu options related to wearing this object
                        string key = "ForceWear";
                        if (t.def.apparel.LastLayer.IsUtilityLayer)
                        {
                            key = "ForceEquipApparel";
                        }
                        string text = key.Translate(t.Label, t);
                        //Log.Message("text = " + text);

                        //disable the float menu option and tell the player why
                        foreach (FloatMenuOption wear in opts.FindAll(x => x.Label.Contains(text)))
                        {
                            //if it's already disabled, leave it alone
                            if (wear.Disabled) continue;

                            wear.Label = text += " : " + "NotOldEnoughToDressSelf".Translate();
                            wear.Disabled = true;
                        }
                    }

                    if (t.def.ingestible != null && !ToddlerLearningUtility.CanFeedSelf(pawn))
                    {
                        //copied directly from source
                        //this will allow us to identify the menu options related to consuming this object
                        string text;
                        if (t.def.ingestible.ingestCommandString.NullOrEmpty())
                        {
                            text = "ConsumeThing".Translate(t.LabelShort, t);
                        }
                        else
                        {
                            text = t.def.ingestible.ingestCommandString.Formatted(t.LabelShort);
                        }

                        //disable the float menu option and tell the player why
                        foreach (FloatMenuOption consume in opts.FindAll(x => x.Label.Contains(text)))
                        {
                            //if it's already disabled, leave it alone
                            if (consume.Disabled) continue;

                            consume.Label = text += " : " + "NotOldEnoughToFeedSelf".Translate();
                            consume.Disabled = true;
                        }
                    }
                }
            }
            return opts;
        }
    }
}
