using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Toddlers.Patch_HAR;

namespace Toddlers
{
    /*
    [HarmonyPatch]
    class TryIssueJobPackage_Testpatch
    {
      
        static IEnumerable<MethodBase> TargetMethods()
        {
            IEnumerable<Type> nodes = from asm in AppDomain.CurrentDomain.GetAssemblies()
                                          from type in asm.GetTypes()
                                          where typeof(ThinkNode_Priority).IsAssignableFrom(type)
                                          select type;
            Log.Message("nodes: " + nodes.ToStringSafeEnumerable());

            IEnumerable<MethodBase> methods0 = from node in nodes
                                               where node != null
                                               select node.GetMethod("TryIssueJobPackage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Log.Message("methods0: " + methods0.ToStringSafeEnumerable());

            IEnumerable<MethodBase> methods1 = methods0.Where(x => x != null && !x.IsAbstract);
            Log.Message("methods1: " + methods1.ToStringSafeEnumerable());

            return methods1;
        }
        

        //public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        static void Prefix(ThinkNode_Priority __instance, Pawn pawn, JobIssueParams jobParams, List<ThinkNode> ___subNodes)
        {
            //if (!ToddlerUtility.IsLiveToddler(pawn)) return;
            if (__instance.GetType() != typeof(ThinkNode_ConditionalMustKeepLyingDown) 
                && __instance.GetType() != typeof(ThinkNode_Priority)
                && __instance.GetType() != typeof(JobGiver_KeepLyingDown)
                && __instance.GetType() != typeof(JobGiver_LayDownAwake)
                && __instance.GetType() != typeof(JobGiver_LayDownResting)) return;
            Log.Message("TryIssueJobPackage - pawn: " + pawn + ", __instance: " + __instance + ", subNodes: " + ___subNodes.ToStringSafeEnumerable());

            int count = ___subNodes.Count;
            for (int i = 0; i < count; i++)
            {
                ThinkNode node = ___subNodes[i];
                ThinkResult result;
                try
                {
                    result = node.TryIssueJobPackage(pawn, jobParams);
                }
                catch
                {
                    result = ThinkResult.NoJob;
                }
                bool? satisfied = null;
                if (node.GetType().GetMethod("Satisfied", BindingFlags.Instance | BindingFlags.NonPublic) != null)
                {
                    satisfied = (bool)node.GetType().GetMethod("Satisfied", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(node, new object[] { pawn });
                }
                Log.Message("i: " + i + ", subNode: " + node
                    + ", satisfied: " + satisfied
                    + ", result: " + result + ", valid: " + result.IsValid);
            }
        }
    }
    */

    [HarmonyPatch(typeof(ThinkNode_ConditionalMustKeepLyingDown), "Satisfied")]
    class MustKeepLyingDown_Testpatch
    {
        //protected override bool Satisfied(Pawn pawn)
        static void Prefix(Pawn pawn)
        {
            Log.Message("MustKeepLyingDown, pawn: " + pawn + ", CurJob: " + pawn.CurJob + ", posture: " + pawn.GetPosture());
        }
    }

    [HarmonyPatch(typeof(Pawn_JobTracker), "TryFindAndStartJob")]
    class TryFindAndStartJob_Testpatch
    {
        //private void TryFindAndStartJob()
        static void Prefix(Pawn_JobTracker __instance, Pawn ___pawn)
        {
            Log.Message("TryFindAndStartJob - pawn: " + ___pawn + ", job: " + __instance.curJob + ", posture: " + __instance.posture);
        }
    }

    [HarmonyPatch]
    class TryGiveJob_Testpatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            IEnumerable<Type> jobGivers = from asm in AppDomain.CurrentDomain.GetAssemblies()
                                 from type in asm.GetTypes()
                                 where typeof(ThinkNode_JobGiver).IsAssignableFrom(type)
                                 select type;
            Log.Message("jobGivers: " + jobGivers.ToStringSafeEnumerable());
            
            IEnumerable<MethodBase> methods0 = from jobGiver in jobGivers
                   where jobGiver != null
                   select jobGiver.GetMethod("TryGiveJob", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Log.Message("methods0: " + methods0.ToStringSafeEnumerable());

            IEnumerable<MethodBase> methods1 = methods0.Where(x => x != null && !x.IsAbstract);
            Log.Message("methods1: " + methods1.ToStringSafeEnumerable());

            return methods1;
        }

        static void Prefix(ThinkNode_JobGiver __instance, Pawn __0)
        {
            Log.Message("TryGiveJob, ThinkNode: " + __instance.GetType() + ", pawn: " + __0);
        }
    }


    [HarmonyPatch(typeof(Pawn_JobTracker),nameof(Pawn_JobTracker.EndCurrentJob))]
    class EndCurrentJob_Testpatch
    {
        //public void EndCurrentJob(JobCondition condition, bool startNewJob = true, bool canReturnToPool = true)
        static void Prefix(Pawn_JobTracker __instance, Pawn ___pawn)
        {
            Log.Message("EndCurrentJob - pawn: " + ___pawn + ", job: " + __instance.curJob + ", posture: " + __instance.posture);
        }
    }

    [HarmonyPatch(typeof(Pawn_JobTracker), "CleanupCurrentJob")]
    class CleanupCurrentJob_Testpatch
    {
        //private void CleanupCurrentJob(JobCondition condition, bool releaseReservations, bool cancelBusyStancesSoft = true, bool canReturnToPool = false, bool? carryThingAfterJobOverride = null)
        static void Prefix(Pawn_JobTracker __instance, Pawn ___pawn)
        {
            Log.Message("CleanupCurrentJob - pawn: " + ___pawn + ", job: " + __instance.curJob + ", posture: " + __instance.posture);
        }
    }

    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
    class StartJob_Testpatch
    {
        //public void StartJob(Job newJob, JobCondition lastJobEndCondition = JobCondition.None, ThinkNode jobGiver = null, bool resumeCurJobAfterwards = false, bool cancelBusyStances = true, ThinkTreeDef thinkTree = null, JobTag? tag = null, bool fromQueue = false, bool canReturnCurJobToPool = false, bool? keepCarryingThingOverride = null, bool continueSleeping = false, bool addToJobsThisTick = true)
        static void Prefix(Pawn_JobTracker __instance, Pawn ___pawn, Job newJob)
        {
            Log.Message("StartJob - pawn: " + ___pawn + ", newJob: " + newJob + ", posture: " + __instance.posture);
        }
    }

    /*
    [HarmonyPatch]
    class ExtendedGraphicTop_GetBestGraphic_TestPatch
    {
        static MethodBase TargetMethod()
        {
            return HARClasses["ExtendedGraphicTop"].GetMethod("GetBestGraphic");
        }


        //public IExtendedGraphic GetBestGraphic(ExtendedGraphicsPawnWrapper pawn, BodyPartDef part, string partLabel)
        static void Postfix(object __instance, object[] __args)
        {
            object wrapper = __args[0];
            Pawn pawn = (Pawn)wrapper.GetType().GetProperty("WrappedPawn", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(wrapper);
            if (pawn.DevelopmentalStage != DevelopmentalStage.Baby) return;
            BodyPartDef part = (BodyPartDef)__args[1];
            string partLabel = (string)__args[2];

            Log.Message("Calling GetBestGraphic for pawn: " + pawn + ", part: " + part);

            Pair<int, object> bestGraphic = new Pair<int, object>(0, __instance);
            Stack<Pair<int, IEnumerator>> stack = new Stack<Pair<int, IEnumerator>>();
            stack.Push(new Pair<int, IEnumerator>(1,
                __instance.GetType().GetMethod("GetSubGraphics", new Type[] { __args[0].GetType(), typeof(BodyPartDef), typeof(string) }).Invoke(__instance, __args) as IEnumerator));

            while (stack.Count > 0 && (bestGraphic.Second == __instance || bestGraphic.First < stack.Peek().First))
            {
                Pair<int, IEnumerator> currentGraphicSet = stack.Pop();
                Log.Message("outerloop, level: " + currentGraphicSet.First + ", IEnumerator: " + currentGraphicSet.Second);
                
                //currentGraphicSet.Second.Reset();
                while (currentGraphicSet.Second.MoveNext())
                {
                    object current = currentGraphicSet.Second.Current;
                    
                    bool? isApplicable = (bool?)current?.GetType().GetMethod("IsApplicable").Invoke(current, __args);
                    Log.Message("innerloop, current: " + current + ", IsApplicable: " + isApplicable);
                    if (!(isApplicable ?? false)) continue;

                    string path = (string)current.GetType().GetMethod("GetPath", new Type[] { }).Invoke(current, new object[] { });
                    int variantCount = (int)current.GetType().GetMethod("GetVariantCount", new Type[] { }).Invoke(current, new object[] { });
                    Log.Message("path: " + path + ", variantCount: " + variantCount);
                    if (path == "void") 
                    {
                        stack.Push(currentGraphicSet);
                    }
                    else if (!path.NullOrEmpty() && variantCount > 0)
                    {
                        Log.Message("Updating bestGraphic to current");
                        bestGraphic = new Pair<int, object>(currentGraphicSet.First, current);
                    }
                    currentGraphicSet = new Pair<int, IEnumerator>(currentGraphicSet.First + 1, current.GetType().GetMethod("GetSubGraphics", new Type[] { __args[0].GetType(), typeof(BodyPartDef), typeof(string) }).Invoke(current, __args) as IEnumerator);
                }

            }
            Log.Message("Final bestGraphic: " + bestGraphic);
        }
    }
    */

    /*
    [HarmonyPatch]
    class ExtendedGraphicTop_GetPath_TestPatch
    {
        static MethodBase TargetMethod()
        {
            return HARClasses["ExtendedGraphicTop"].GetMethod("GetPath", new Type[] { typeof(Pawn), typeof(int).MakeByRefType(), typeof(int?), typeof(string) });
        }


        static void Postfix(object __instance, Pawn pawn, BodyPartDef ___bodyPart, string ___bodyPartLabel, string __result)
        {
            object wrapper = Activator.CreateInstance(HARClasses["ExtendedGraphicsPawnWrapper"], new object[] { pawn });
            object bestGraphic = __instance.GetType().GetMethod("GetBestGraphic")
                .Invoke(__instance, new object[] { wrapper, ___bodyPart, ___bodyPartLabel });
            Log.Message("pawn: " + pawn + ", bodyType: " + pawn.story.bodyType + ", bestGraphic: " + bestGraphic + ", result path: " + __result);
            IEnumerable subGraphics = (__instance.GetType().GetMethod("GetSubGraphics", new Type[] { })
                .Invoke(__instance, new object[] { })) as IEnumerable;
            Log.Message("subGraphics:");
            foreach(object subGraphic in subGraphics)
            {
                Log.Message("object: " + subGraphic + ", bodytype: " + (HARClasses["ExtendedBodytypeGraphic"].IsAssignableFrom(subGraphic.GetType()) ? field_ExtendedBodytypeGraphic_bodytype.GetValue(subGraphic) : null));
            }
        }
    }
    */

    /*
    [HarmonyPatch(typeof(Graphic_Multi), nameof(Graphic_Multi.Init))]
    class Graphic_Multi_TestPatch
    {
        [HarmonyPriority(Priority.High)]
        static void Prefix(GraphicRequest req)
        {
            Log.Message("req.path: " + req.path);
        }
    }
    */

    /*
    [HarmonyPatch(typeof(PawnRenderer), "RenderPawnInternal")]
    class RenderPawnInternal_TestPatch
    {
        [HarmonyPriority(Priority.High)]
        static void Prefix(PawnRenderer __instance, Pawn ___pawn, Vector3 rootLoc, ref float angle, bool renderBody, ref Rot4 bodyFacing, RotDrawMode bodyDrawType, PawnRenderFlags flags)
        {
            //if (!ToddlerUtility.IsLiveToddler(___pawn)) return;
            if (!(  ___pawn.DevelopmentalStage == DevelopmentalStage.Baby)) return;
            Log.Message("CurLifeStage: " + ___pawn.ageTracker.CurLifeStage + ", def: " + ___pawn.ageTracker.CurLifeStage.defName);

            Log.Message("RenderPawnInternal_TestPatch, pawn: " + ___pawn);
            Log.Message("rootLoc: " + rootLoc);

            Vector3 bodyDrawOffset = (Vector3)___pawn.ageTracker.CurLifeStage.bodyDrawOffset;
            Log.Message("bodyDrawOffset: " + bodyDrawOffset);
            
            Vector3 headOffset = __instance.BaseHeadOffsetAt(Rot4.North);
            Log.Message("bodyType.headOffset: " + ___pawn.story.bodyType.headOffset);
            
            
            float bodySizeFactor = ___pawn.ageTracker.CurLifeStage.bodySizeFactor;
            Log.Message("lifeStage.bodySizeFactor: " + bodySizeFactor + ", Sqrt: " + Mathf.Sqrt(bodySizeFactor));

            object ageAlien = ___pawn.ageTracker.CurLifeStageRace;
            Vector2 alienHeadOffset_base =
                (Vector2?)ageAlien.GetType().GetField("headOffset").GetValue(ageAlien)
                ?? Vector2.zero;
            Log.Message("alienHeadOffset_base: " + alienHeadOffset_base);

            object headOffsetDirectional =
                ageAlien.GetType().GetField("headOffsetDirectional")
                .GetValue(ageAlien);
            Vector2 alienHeadOffset_direc =
                (Vector2?)headOffsetDirectional.GetType()
                    .GetMethod("GetOffset", new Type[] { typeof(Rot4) })
                    .Invoke(headOffsetDirectional, new object[] { Rot4.North })
                ?? Vector2.zero;
            Log.Message("alienHeadOffset_direc: " + alienHeadOffset_direc);

            object headOffsetSpecific =
                ageAlien.GetType().GetField("headOffsetSpecific")
                .GetValue(ageAlien);
            object alienHeadOffset_spec =
                headOffsetSpecific.GetType()
                    .GetMethod("GetOffset", new Type[] { typeof(Rot4) })
                    .Invoke(headOffsetSpecific, new object[] { Rot4.North })
                ?? Vector2.zero;
            Vector3 alienHeadOffset_specInner =
                (Vector3?)alienHeadOffset_spec.GetType()
                    .GetMethod("GetOffset", new Type[] { typeof(bool), typeof(BodyTypeDef), typeof(HeadTypeDef) })
                    .Invoke(alienHeadOffset_spec, new object[] { false, ___pawn.story.bodyType, ___pawn.story.headType })
                ?? Vector3.zero;
            Log.Message("alienHeadOffset_specInner: " + alienHeadOffset_direc);
            

            
            //Vanilla:
            //head is drawn at vector3 + vector4, quaternion
            //vector3 = vector + y-offsets 
            //vector = rootLoc + bodyDrawOffset
            //vector4 = quaternion * baseheadoffsetat
            //quaternion = Quaternion.AngleAxis(angle, Vector3.up);

            
            Log.Message("angle: " + angle);
            Quaternion quaternion = Quaternion.AngleAxis(angle, Vector3.up);
            Log.Message("vector3: " + (rootLoc + bodyDrawOffset));
            Log.Message("vector4: " + quaternion * baseHeadOffset);
            Log.Message("vanilla head drawn at: " + (rootLoc + bodyDrawOffset + (quaternion * baseHeadOffset)));

            Mesh headMesh = HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn(___pawn).MeshAt(bodyFacing);
            Log.Message("headMesh: " + headMesh + ", name: " + headMesh.name);
            
            Material headMat = __instance.graphics.HeadMatAt(bodyFacing, bodyDrawType, flags.FlagSet(PawnRenderFlags.HeadStump), flags.FlagSet(PawnRenderFlags.Portrait), !flags.FlagSet(PawnRenderFlags.Cache));
            Log.Message("headMat: " + headMat + ", name: " + headMat.name);
            
        }

    }

    [HarmonyPatch(typeof(PawnRenderer),"DrawPawnBody")]
    class DrawPawnBody_TestPatch
    {
        [HarmonyPriority(Priority.High)]
        static void Prefix(PawnRenderer __instance, Pawn ___pawn, Vector3 rootLoc, float angle, Rot4 facing, RotDrawMode bodyDrawType, PawnRenderFlags flags, Mesh bodyMesh)
        {
            if (!ToddlerUtility.IsLiveToddler(___pawn)) return;

            //Quaternion quat = Quaternion.AngleAxis(angle, Vector3.up);

            Log.Message("DrawPawnBody, rootLoc: " + rootLoc + ", angle: " + angle + ", facing: " + facing);
            List<Material> list = __instance.graphics.MatsBodyBaseAt(facing, ___pawn.Dead, bodyDrawType, flags.FlagSet(PawnRenderFlags.Clothes));

            MethodInfo overrideMaterialIfNeeded = __instance.GetType().GetMethod("OverrideMaterialIfNeeded", BindingFlags.NonPublic | BindingFlags.Instance);

            for (int i = 0; i < list.Count; i++)
            {
                Material material = ((___pawn.RaceProps.IsMechanoid && ___pawn.Faction != null && ___pawn.Faction != Faction.OfMechanoids) ? __instance.graphics.GetOverlayMat(list[i], ___pawn.Faction.MechColor) : list[i]);
                Material mat = (flags.FlagSet(PawnRenderFlags.Cache) ? material : (Material)overrideMaterialIfNeeded.Invoke(__instance, new object[] { material, ___pawn, flags.FlagSet(PawnRenderFlags.Portrait) }));
                Log.Message("i: " + i + ", material: " + material + ", mat: " + mat);
                //GenDraw.DrawMeshNowOrLater(bodyMesh, rootLoc, quat, mat, flags.FlagSet(PawnRenderFlags.DrawNow));
            }
        }
    }

    [HarmonyPatch(typeof(GenDraw),nameof(GenDraw.DrawMeshNowOrLater),new Type[] { typeof(Mesh), typeof(Vector3), typeof(Quaternion), typeof(Material), typeof(bool) })]
    class DrawMeshNowOrLater_TestPatch
    {
        [HarmonyPriority(Priority.High)]
        static void Prefix(Mesh mesh, Vector3 loc, Quaternion quat, Material mat, bool drawNow)
        {
            if (mat.name != "Custom/CutoutComplex_KiiroHeadN1_south" 
                && mat.name != "Custom/CutoutComplex_KiiroHeadN1_north"
                && mat.name != "Custom/CutoutRecolor_Naked_Baby_north") 
                return;
            Log.Message("mesh: " + mesh + ", loc: " + loc + ", quat: " + quat + ", mat: " + mat);
        }
    }
    */


    /*
    [HarmonyPatch(typeof(PawnGraphicSet),nameof(PawnGraphicSet.MatsBodyBaseAt))]
    static class MatsBodyBaseAt_Patch
    {
        //public List<Material> MatsBodyBaseAt(Rot4 facing, bool dead, RotDrawMode bodyCondition = RotDrawMode.Fresh, bool drawClothes = true)
        static void Prefix(PawnGraphicSet __instance, Rot4 facing, bool dead, RotDrawMode bodyCondition, bool drawClothes)
        {

            Log.Message("pawn: " + __instance.pawn);

            /*
            Log.Message("facing: " + facing);
            Log.Message("dead: " + dead);
            Log.Message("bodyCondition: " + bodyCondition);
            Log.Message("drawClothes: " + drawClothes);

            int cachedMatsBodyBaseHash = (int)__instance.GetType().GetField("cachedMatsBodyBaseHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            Log.Message("cachedMatsBodyBaseHash: " + cachedMatsBodyBaseHash);

            List<Material> cachedMatsBodyBase = (List<Material>)__instance.GetType().GetField("cachedMatsBodyBase", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            Log.Message("cachedMatsBodyBase: " + cachedMatsBodyBase);

            Log.Message("corpseGraphic: " + __instance.corpseGraphic);
            Log.Message("nakedGraphic: " + __instance.nakedGraphic);
            Log.Message("rottingGraphic: " + __instance.rottingGraphic);
            Log.Message("dessicatedGraphic: " + __instance.dessicatedGraphic);
            Log.Message("apparelGraphics: " + __instance.apparelGraphics);
            Log.Message("apparelGraphics.Count: " + __instance.apparelGraphics.Count);
            /

    Pawn alien = __instance.pawn;
        if (Patch_HAR.class_ThingDef_AlienRace.IsAssignableFrom(alien.def.GetType()))
        {
            LifeStageAge lsa = alien.ageTracker.CurLifeStageRace;
            //if (lsa.def != Toddlers_DefOf.HumanlikeToddler) return;
            Log.Message("lsa: " + lsa + ", def: " + lsa.def);
            //Log.Message("Patch_HAR.class_ThingDef_AlienRace.IsAssignableFrom(alien.def.GetType())");
            object alienRace = alien.def.GetType().GetField("alienRace").GetValue(alien.def);
            //Log.Message("alienRace: " + alienRace);
            object graphicPaths = alienRace.GetType().GetField("graphicPaths").GetValue(alienRace);
            //Log.Message("graphicPaths: " + graphicPaths);

            object body = graphicPaths.GetType().GetField("body").GetValue(graphicPaths);
            Log.Message("body: " + body);

            Type class_AlienComp = (from asm in AppDomain.CurrentDomain.GetAssemblies()
                                    from type in asm.GetTypes()
                                    where type.Namespace == "AlienRace" && type.IsClass && type.Name == "AlienComp"
                                    select type).Single();
            //Log.Message("class_AlienComp: " + class_AlienComp);

            object alienComp = alien.AllComps.Find(x => x.GetType() == class_AlienComp);
            //Log.Message("alienComp: " + alienComp);

            object bodyVariant = alienComp.GetType().GetField("bodyVariant").GetValue(alienComp);
            //Log.Message("bodyVariant: " + bodyVariant);

            MethodInfo getPath = body.GetType().GetMethod("GetPath", new Type[] { typeof(Pawn), typeof(int).MakeByRefType(), typeof(int?), typeof(string) });
            Log.Message("getPath: " + getPath);

            int sharedIndex = 0;
            string bodyPath = (string)getPath.Invoke(body, new object[] { alien, sharedIndex, (int)bodyVariant, null });
            Log.Message("bodyPath: " + bodyPath);

            Type class_ExtendedGraphicsPawnWrapper = (from asm in AppDomain.CurrentDomain.GetAssemblies()
                                    from type in asm.GetTypes()
                                    where type.Namespace == "AlienRace.ExtendedGraphics" && type.IsClass && type.Name == "ExtendedGraphicsPawnWrapper"
                                                      select type).Single();
            Log.Message("class_ExtendedGraphicsPawnWrapper: " + class_ExtendedGraphicsPawnWrapper);

            MethodInfo getBestGraphic = body.GetType().GetMethod("GetBestGraphic", new Type[] { class_ExtendedGraphicsPawnWrapper, typeof(BodyPartDef),  typeof(string) });
            Log.Message("getBestGraphic: " + getBestGraphic);

            object pawnWrapper = Activator.CreateInstance(class_ExtendedGraphicsPawnWrapper, new object[] { alien });
            Log.Message("pawnWrapper: " + pawnWrapper);

            BodyPartDef bodyPart = (BodyPartDef)body.GetType().GetField("bodyPart").GetValue(body);
            string bodyPartLabel = (string)body.GetType().GetField("bodyPartLabel").GetValue(body);
            Log.Message("bodyPart: " + bodyPart + ", label: " + bodyPartLabel);

            object bestGraphic = getBestGraphic.Invoke(body, new object[] { pawnWrapper, bodyPart, bodyPartLabel });
            Log.Message("bestGraphic: " + bestGraphic);

            MethodInfo getSubGraphics = body.GetType().GetMethod("GetSubGraphics", new Type[] { class_ExtendedGraphicsPawnWrapper, typeof(BodyPartDef), typeof(string) });
            Log.Message("GetSubGraphics: " + getSubGraphics);

            object subGraphics = getSubGraphics.Invoke(body, new object[] { pawnWrapper, bodyPart, bodyPartLabel });
            Log.Message("subGraphics: " + subGraphics);

            IEnumerator subGraphics_ienum = subGraphics as IEnumerator;

            string REWIND_PATH = "void";

            Pair<int, object> bestGraphicInner = new Pair<int, object>(0, body);
            Stack<Pair<int, IEnumerator>> stack = new Stack<Pair<int, IEnumerator>>();
            stack.Push(new Pair<int, IEnumerator>(1, subGraphics_ienum)); // generate list of subgraphics

            // Loop through sub trees until we find a deeper match or we run out of alternatives
            while (stack.Count > 0 && (bestGraphicInner.Second == body || bestGraphicInner.First < stack.Peek().First))
            {
                Log.Message("Looping, stack.Count: " + stack.Count + ", bestGraphicInner:" + bestGraphicInner + ", stack.Peek(): " + stack.Peek());

                Pair<int, IEnumerator> currentGraphicSet = stack.Pop(); // get the top of the stack
                Log.Message("currentGraphicSet: " + currentGraphicSet);

                while (currentGraphicSet.Second.MoveNext()) // exits if iterates through list of subgraphics without advancing
                {
                    object current = currentGraphicSet.Second.Current; //current branch of tree
                    //Log.Message("current: " + current);
                    Log.ResetMessageCount();
                    string currentPath = (string)current.GetType().GetMethod("GetPath", new Type[] { }).Invoke(current, new object[] { });
                    Log.Message("currentPath: " + currentPath);
                    MethodInfo isApplicable = current.GetType().GetMethod("IsApplicable", new Type[] { class_ExtendedGraphicsPawnWrapper, typeof(BodyPartDef), typeof(string) });
                    //Log.Message("isApplicable: " + isApplicable);
                    bool? applicable = (bool?)isApplicable.Invoke(current, new object[] { pawnWrapper, bodyPart, bodyPartLabel });
                    Log.Message("applicable: " + applicable);

                    if (!(applicable ?? false))
                        continue;

                    MethodInfo getVariantCount = current.GetType().GetMethod("GetVariantCount", new Type[] { });
                    Log.Message("getVariantCount: " + getVariantCount);
                    int variantCount = (int)getVariantCount.Invoke(current, new object[] { });
                    Log.Message("variantCount: " + variantCount);

                    if (currentPath == REWIND_PATH)
                        // add the current layer back to the stack so we can rewind
                        stack.Push(currentGraphicSet);
                    else if (!currentPath.NullOrEmpty() && variantCount > 0)
                        // Only update best graphic if the current one has a valid path
                        bestGraphic = new Pair<int, object>(currentGraphicSet.First, current);
                    //Log.Message((string)bestGraphicInner.Second.GetType().GetMethod("GetPath", new Type[] { }).Invoke(bestGraphicInner.Second, new object[] { }));
                    //Log.Message(bestGraphicInner.Second.GetPath());
                    // enters next layer/branch
                    MethodInfo newGetSubGraphics = current.GetType().GetMethod("GetSubGraphics", new Type[] { class_ExtendedGraphicsPawnWrapper, typeof(BodyPartDef), typeof(string) });
                    Log.Message("newGetSubGraphics: " + newGetSubGraphics);

                    IEnumerator newSubGraphics = newGetSubGraphics.Invoke(current, new object[] { pawnWrapper, bodyPart, bodyPartLabel }) as IEnumerator;
                    currentGraphicSet = new Pair<int, IEnumerator>(currentGraphicSet.First + 1, newSubGraphics);
                }

                Log.Message("bestGraphicInner.Second: " + bestGraphicInner.Second);
            }
        }
    /
    }
}
*/
}
