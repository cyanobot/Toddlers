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
    /*
    //allows crying/giggling toddlers to be given orders
    [HarmonyPatch(typeof(FloatMenuMakerMap), "CanTakeOrder")]
    class CanTakeOrder_Patch
    {
        static bool Postfix(bool result, Pawn pawn)
        {
            if (result == false && ToddlerUtility.IsLiveToddler(pawn) && pawn.Spawned
                && (pawn.IsColonist || pawn.IsSlaveOfColony))
                result = true;
            return result;
        }
    }
    */

    [HarmonyPatch(typeof(FloatMenuMakerMap), "ChoicesAtFor")]
    class FloatMenu_Patch
    {
        //copied from Injured Carry with modifications
        private static TargetingParameters ForToddler(Pawn pawn)
        {
            return new TargetingParameters
            {
                canTargetPawns = true,
                canTargetBuildings = false,
                neverTargetIncapacitated = true,
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

        //copied from Dress Patient with modifications
        private static TargetingParameters ForBaby(Pawn pawn)
        {
            return new TargetingParameters
            {
                canTargetPawns = true,
                canTargetBuildings = false,
                neverTargetIncapacitated = false,
                neverTargetHostileFaction = true,
                mapObjectTargetsMustBeAutoAttackable = false,
                validator = delegate (TargetInfo targ)
                {
                    if (!targ.HasThing) return false;
                    Pawn baby = targ.Thing as Pawn;
                    if (baby == null || baby == pawn) return false;
                    return baby.DevelopmentalStage == DevelopmentalStage.Baby && ChildcareUtility.CanSuckle(baby, out var _);
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
            if (!ToddlerUtility.IsLiveToddler(pawn))
            {
                //have to be able to manipulate to do anything to a baby
                if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                {
                    foreach (LocalTargetInfo localTargetInfo1 in GenUI.TargetsAt(clickPos, ForToddler(pawn), thingsOnly: true))
                    {
                        Pawn toddler = (Pawn)localTargetInfo1.Thing;

                        //option to let crawlers out of their cribs
                        if (ToddlerUtility.InCrib(toddler) && ToddlerUtility.IsCrawler(toddler))
                        {
                            FloatMenuOption letOutOfCrib = new FloatMenuOption("Let " + toddler.Label + " out of crib", delegate 
                            {
                                Building_Bed crib = ToddlerUtility.GetCurrentCrib(toddler);
                                if (crib == null) return;
                                Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("LetOutOfCrib"), toddler, crib);
                                job.count = 1;
                                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                            },MenuOptionPriority.Default);
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(letOutOfCrib, pawn, toddler));
                        }

                        //option to pick up toddlers and take them to their bed

                        //patch for Injured Carry to avoid duplicate menu options
                        //checks the same logic as Injured Carry
                        if (Toddlers_Mod.injuredCarryLoaded)
                        {
                            if (HealthAIUtility.ShouldSeekMedicalRest(toddler)
                                && !toddler.IsPrisonerOfColony && !toddler.IsSlaveOfColony
                                && (!toddler.InMentalState || toddler.health.hediffSet.HasHediff(HediffDefOf.Scaria))
                                && !toddler.IsColonyMech
                                && (toddler.Faction == Faction.OfPlayer || toddler.Faction == null || !toddler.Faction.HostileTo(Faction.OfPlayer)))
                                continue;
                        }

                        if (!toddler.InBed()
                            && pawn.CanReserveAndReach(toddler, PathEndMode.OnCell, Danger.None, 1, -1, null, ignoreOtherReservations: true)
                            && !toddler.mindState.WillJoinColonyIfRescued
                        )
                        {
                            FloatMenuOption putInCrib = new FloatMenuOption("Put " + toddler.Label + " in crib", delegate
                            {
                                Building_Bed building_Bed = RestUtility.FindBedFor(toddler, pawn, checkSocialProperness: false);
                                if (building_Bed == null)
                                {
                                    building_Bed = RestUtility.FindBedFor(toddler, pawn, checkSocialProperness: false, ignoreOtherReservations: true);
                                }
                                if (building_Bed == null)
                                {
                                    string t = (!toddler.RaceProps.Animal) ? ((string)"NoNonPrisonerBed".Translate()) : ((string)"NoAnimalBed".Translate());
                                    Messages.Message("CannotRescue".Translate() + ": " + "No bed", toddler, MessageTypeDefOf.RejectInput, historical: false);
                                }
                                else
                                {
                                    Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("PutInCrib"), toddler, building_Bed);
                                    job.count = 1;
                                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                }
                            }, MenuOptionPriority.RescueOrCapture, null, toddler);
                            if (RestUtility.FindBedFor(toddler, pawn, checkSocialProperness: false, ignoreOtherReservations: true) == null)
                            {
                                putInCrib.Label += " : No crib available";
                                putInCrib.Disabled = true;
                            }
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(putInCrib, pawn, toddler));
                        }

                        //option to return toddlers to safety
                        //should have correct label rather than "rescue"
                        foreach (FloatMenuOption rescue in opts.FindAll(x =>
                            x.Label.Contains("Rescue".Translate(toddler.LabelCap, toddler))))
                        {
                            //if they do need rescuing, still say rescue
                            if (!HealthAIUtility.ShouldSeekMedicalRest(toddler))
                            {
                                rescue.Label = "PutSomewhereSafe".Translate(toddler.LabelCap, toddler);
                            }
                        }

                    }
                    //options for dressing and undressing babies and toddlers
                    foreach (LocalTargetInfo localTargetInfo1 in GenUI.TargetsAt(clickPos, ForBaby(pawn), thingsOnly: true))
                    {
                        Pawn baby = (Pawn)localTargetInfo1.Thing;

                        //patch for Dress Patients to avoid duplicate menu options
                        //check the same logic as Dress Patients to figure out if that mod will be generating a menu option 
                        if (Toddlers_Mod.dressPatientsLoaded)
                        {
                            if (baby.InBed()
                                 && (baby.Faction == Faction.OfPlayer || baby.HostFaction == Faction.OfPlayer)
                                 && (baby.guest != null ? pawn.guest.interactionMode != PrisonerInteractionModeDefOf.Execution : true)
                                 && HealthAIUtility.ShouldSeekMedicalRest(baby))
                                continue;
                        }

                        if (!pawn.CanReserveAndReach(baby, PathEndMode.ClosestTouch, Danger.None, 1, -1, null, ignoreOtherReservations: true))
                            continue;

                        //option to dress baby
                        FloatMenuOption dressBaby = new FloatMenuOption("Dress " + baby.Label, delegate ()
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
                }
            }

            //for toddlers, mostly disabling/removing options for things they can't do
            else
            {
                //int n = opts.RemoveAll(x => x.Label.Contains(pawn.LabelShort));
                opts.RemoveAll(x => x.revalidateClickTarget == pawn || x.Label.Contains(pawn.LabelShort) || x.Label.Contains(pawn.LabelShortCap));

                foreach (Thing t in c.GetThingList(pawn.Map))
                {                                  
                    if (t.def.IsApparel && !ToddlerUtility.CanDressSelf(pawn))
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

                            wear.Label = text += " : Not old enough to dress self";
                            wear.Disabled = true;
                        }
                    }

                    if (t.def.ingestible != null && !ToddlerUtility.CanFeedSelf(pawn))
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

                            consume.Label = text += " : Not old enough to feed self";
                            consume.Disabled = true;
                        }
                    }
                }
            }
            return opts;
        }
    }
}
