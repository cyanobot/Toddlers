using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using UnityEngine;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Toddlers.ToddlerUtility;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Toddlers
{
    /*
    [HarmonyPatch]
    class TestPatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(PawnRenderer), "RenderPawnInternal");
        }
        static void Prepare(MethodBase original)
        {
            //Log.Message("Prepare, original: " + original);
        }
        static void Postfix(MethodInfo __originalMethod, object[] __args)
        {
            Log.Message(__originalMethod.DeclaringType + "." + __originalMethod.Name + " fired, args: " + __args.ToStringSafeEnumerable());
        }
    }
    */

    [HarmonyPatch]
    class TestPatch_GetHumanlikeMeshSets
    {
        static  IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(HumanlikeMeshPoolUtility), nameof(HumanlikeMeshPoolUtility.GetHumanlikeBodySetForPawn));
            yield return AccessTools.Method(typeof(HumanlikeMeshPoolUtility), nameof(HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn));
            yield return AccessTools.Method(typeof(HumanlikeMeshPoolUtility), nameof(HumanlikeMeshPoolUtility.GetHumanlikeHairSetForPawn));
        }

        static void Postfix(MethodInfo __originalMethod, Pawn pawn, GraphicMeshSet __result)
        {
            Log.Message(__originalMethod.Name + " fired, pawn: " + pawn + ", result: " + __result + ", mesh(south).vertices: " + __result.MeshAt(Rot4.South).vertices.ToStringSafeEnumerable(),false);
        }
    }

    [HarmonyPatch(typeof(PawnRenderer),"RenderPawnInternal")]
    class TestPatch_RenderPawnInternal
    {
        static void Postfix(PawnGraphicSet ___graphics)
        {
            List<ApparelGraphicRecord> apparelGraphics = ___graphics.apparelGraphics;
            Log.Message("RenderPawnInternal finished, apparelGraphics: " + apparelGraphics.ToStringSafeEnumerable());
            ApparelGraphicRecord apGrap0 = apparelGraphics[0];
            Log.Message("apparelGraphics[0].sourceApparel: " + apGrap0.sourceApparel + ", .graphic: " + apGrap0.graphic);
            Graphic graphic = apGrap0.graphic;
            Log.Message("graphic.drawSize: " + graphic.drawSize + ", mesh(south).vertices: " + graphic.MeshAt(Rot4.South).vertices.ToStringSafeEnumerable());
        }
    }

    [HarmonyPatch(typeof(PawnRenderer), "DrawHeadHair")]
    //[HarmonyDebug]
    class DrawHeadHair_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {

            //foreach (CodeInstruction instruction in instructions)
            //{
            //    yield return instruction;
            //}
            //yield break;


            //DrawHeadHair(Vector3 rootLoc, Vector3 headOffset, float angle, Rot4 bodyFacing, Rot4 headFacing, RotDrawMode bodyDrawType, PawnRenderFlags flags, bool bodyDrawn)

            string outerName = "DrawHeadHair";
            string innerName = "DrawApparel";

            string nameStart = "<" + outerName + ">g__" + innerName + "|";

            MethodInfo m_DrawApparel = typeof(PawnRenderer).GetNestedTypes(BindingFlags.NonPublic)
                .Where(t => t.GetCustomAttribute<CompilerGeneratedAttribute>() != null)
                .SelectMany(t => t.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
                .Where(x => x.Name.StartsWith(nameStart))
                .Single();
            MethodInfo m_DrawApparelFake = AccessTools.Method(typeof(DrawHeadHair_Patch), nameof(DrawHeadHair_Patch.DrawApparelFake));
            MethodInfo m_IsHumanBaby = AccessTools.Method(typeof(DrawHeadHair_Patch), nameof(DrawHeadHair_Patch.IsHumanBaby));

            bool found = false;
            int i = -1;
            List<Label> origLabels = new List<Label>();
            List<Label> skipLabels = new List<Label>();
            Label origLabel = il.DefineLabel();
            Label skipOrigLabel = il.DefineLabel();

            foreach (CodeInstruction instruction in instructions)
            {
                if (found)
                {
                    instruction.labels.Add(skipLabels[i]);
                    found = false;
                }

                if (instruction.Calls(m_DrawApparel))
                {
                    found = true;
                    i++;
                    origLabels.Add(il.DefineLabel());
                    skipLabels.Add(il.DefineLabel());

                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, m_IsHumanBaby);
                    yield return new CodeInstruction(OpCodes.Brfalse, origLabels[i]);

                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Ldarg_3);
                    yield return new CodeInstruction(OpCodes.Ldarg, 4);
                    yield return new CodeInstruction(OpCodes.Ldarg, 5);
                    yield return new CodeInstruction(OpCodes.Ldarg, 6);
                    yield return new CodeInstruction(OpCodes.Ldarg, 7);
                    yield return new CodeInstruction(OpCodes.Ldarg, 8);

                    yield return new CodeInstruction(OpCodes.Call, m_DrawApparelFake);
                    yield return new CodeInstruction(OpCodes.Pop);                      //got a stray this on the stack that would be needed by callvirt drawapparell but not by our static fake

                    yield return new CodeInstruction(OpCodes.Br, skipLabels[i]);
                    instruction.labels.Add(origLabels[i]);
                    yield return instruction;

                }
                else
                {
                    yield return instruction;
                }
            }
        }

        //duplicate of vanilla method
        static void DrawApparelFake(ApparelGraphicRecord apparelRecord, PawnRenderer instance, Vector3 rootLoc, Vector3 headOffset, float angle, Rot4 bodyFacing, Rot4 headFacing, RotDrawMode bodyDrawType, PawnRenderFlags flags, bool bodyDrawn)
        {
            Log.Message("DrawApparelFake");

            Vector3 onHeadLoc = rootLoc + headOffset;
            Quaternion quat = Quaternion.AngleAxis(angle, Vector3.up);
            Pawn pawn = (Pawn)instance.GetType().GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);

            Mesh mesh3 = instance.graphics.HairMeshSet.MeshAt(headFacing);
            Log.Message("mesh3 vertices: " + mesh3.vertices.ToStringSafeEnumerable());
            if (!apparelRecord.sourceApparel.def.apparel.hatRenderedFrontOfFace)
            {
                onHeadLoc.y += 0.0289575271f;
                Material material3 = apparelRecord.graphic.MatAt(bodyFacing);
                material3 = (flags.FlagSet(PawnRenderFlags.Cache) ? material3 : OverrideMaterialIfNeeded(instance, material3, pawn, flags.FlagSet(PawnRenderFlags.Portrait)));
                GenDraw.DrawMeshNowOrLater(mesh3, onHeadLoc, quat, material3, flags.FlagSet(PawnRenderFlags.DrawNow));
            }
            else
            {
                Material material4 = apparelRecord.graphic.MatAt(bodyFacing);
                material4 = (flags.FlagSet(PawnRenderFlags.Cache) ? material4 : OverrideMaterialIfNeeded(instance, material4, pawn, flags.FlagSet(PawnRenderFlags.Portrait)));
                if (apparelRecord.sourceApparel.def.apparel.hatRenderedBehindHead)
                {
                    onHeadLoc.y += 0.0221660212f;
                }
                else
                {
                    onHeadLoc.y += ((bodyFacing == Rot4.North && !apparelRecord.sourceApparel.def.apparel.hatRenderedAboveBody) ? 0.00289575267f : 0.03185328f);
                }
                GenDraw.DrawMeshNowOrLater(mesh3, onHeadLoc, quat, material4, flags.FlagSet(PawnRenderFlags.DrawNow));
            }
        }

        static Material OverrideMaterialIfNeeded(PawnRenderer instance, Material original, Pawn pawn, bool portrait = false)
        {
            MethodInfo origMethod = instance.GetType().GetMethod("OverrideMaterialIfNeeded", BindingFlags.Instance | BindingFlags.NonPublic);
            return (Material)origMethod.Invoke(instance, new object[] { original, pawn, portrait });
        }

        static bool IsHumanBaby(PawnRenderer instance)
        {
            return true;

            Pawn pawn = (Pawn)instance.GetType().GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);
            if (pawn.def == ThingDefOf.Human && pawn.DevelopmentalStage == DevelopmentalStage.Baby) return true;
            return false;
        }
    }
}
