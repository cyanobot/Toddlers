using Verse;
using UnityEngine;
using HarmonyLib;
using System.Reflection;
using static Toddlers.Patch_HAR;


namespace Toddlers
{
    [HarmonyPatch]
    static class HAR_DrawAddonsFinalHook_Patch
    {
        static bool Prepare(MethodBase original)
        {
            if (!Toddlers_Mod.HARLoaded)
                return false;
            else return true;
        }

        static MethodBase TargetMethod() => Patch_HAR.method_HarmonyPatches_DrawAddonsFinalHook;

        //public static void DrawAddonsFinalHook(Pawn pawn, AlienPartGenerator.BodyAddon addon, Rot4 rot, ref Graphic graphic, ref Vector3 offsetVector, ref float angle, ref Material mat)
        static void Postfix(PawnRenderFlags renderFlags, Pawn pawn, object addon, ref Rot4 rot, ref Graphic graphic, ref Vector3 offsetVector, ref float angle, ref Material mat)
        {
            //Log.Message("HAR Patch Postfix, pawn: " + pawn + ", addon: " + addon.GetType().GetProperty("Name").GetValue(addon) 
            //    + ", rot: " + rot + ", offsetVector: " + offsetVector + ", angle: " + angle);

            if (!ToddlerUtility.IsLiveToddler(pawn))
                return;

            bool isInvisible = renderFlags.HasFlag(PawnRenderFlags.Invisible);
            if (renderFlags.HasFlag(PawnRenderFlags.Portrait) | renderFlags.HasFlag(PawnRenderFlags.StylingStation)) return;

            if (!HARClasses["BodyAddon"].IsAssignableFrom(addon.GetType())) return;

            ToddlerRenderer.ToddlerRenderMode renderMode = ToddlerRenderer.GetToddlerRenderMode(pawn);

            if (renderMode == ToddlerRenderer.ToddlerRenderMode.Crawling)
            {

                BodyAddon wrapper = Patch_HAR.GetBodyAddonWrapper(pawn, addon);
                if (wrapper == null) return;

                bool alignWithHead = wrapper.alignWithHead;
                Rot4 crawlRot = rot;

                if (alignWithHead)
                {
                    float angleChange = 0f;
                    //Log.Message("renderMode == Crawling, start angle: " + angle);
                    if (rot == Rot4.East)
                    {
                        angleChange = -1 * ToddlerRenderer.CrawlAngle / 2;
                    }
                    if (rot == Rot4.West)
                    {
                        angleChange = ToddlerRenderer.CrawlAngle / 2;
                    }
                    if (rot == Rot4.South)
                    {
                        angleChange = 180f;
                    }
                    Quaternion quat = Quaternion.AngleAxis(angleChange, Vector3.up);
                    offsetVector = quat * offsetVector;
                    angle += angleChange;
                }
                else 
                {
                    if (rot == Rot4.South)
                    {
                        rot = Rot4.North;

                        offsetVector = wrapper.GetNorthOffset(pawn);

                        mat = graphic.MatAt(rot);
                        if (isInvisible)
                            mat = InvisibilityMatPool.GetInvisibleMat(mat);
                    }
                }

                if (pawn.def.HasModExtension<CrawlingTweak>())
                {
                    Vector2 tweakVector = pawn.def.GetModExtension<CrawlingTweak>().BodyAddonOffset(wrapper, crawlRot);
                    //Log.Message("Found CrawlingTweak, addon: " + wrapper.Name + ", vector: " + tweakVector);
                    offsetVector.x += tweakVector.x;
                    offsetVector.z += tweakVector.y;
                }
            }

            //Log.Message("End rot: " + rot + ", offsetVector: " + offsetVector + ", angle: " + angle);
        }
    }
}
