using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Toddlers
{
    public static class Patch_FacialAnimation
    {
        public const string FANamespace = "FacialAnimation";
        public const string FAHarmonyID = "rimworld.Nals.FacialAnimation";

        public static Type class_HarmonyPatches;
        public static MethodBase methodBase_DrawFace;

        public static void Init() 
        {
            class_HarmonyPatches = (from asm in AppDomain.CurrentDomain.GetAssemblies()
                                    from type in asm.GetTypes()
                                    where type.Namespace == FANamespace && type.IsClass && type.Name == "HarmonyPatches"
                                    select type).Single();

            methodBase_DrawFace = class_HarmonyPatches.GetMethod("DrawFace", BindingFlags.Static | BindingFlags.Public);
        }

        public static void DrawFace(Mesh mesh, Vector3 pos, Quaternion quaternion, Material mat, bool portrait)
        {
            methodBase_DrawFace.Invoke(null, new object[] { mesh, pos, quaternion, mat, portrait });
        }

    }

}
