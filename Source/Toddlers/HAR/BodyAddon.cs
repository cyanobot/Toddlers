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
    //wrapper for HAR body addons to make handling them easier
    public class BodyAddon
    {
        public static FieldInfo field_alignWithHead = HARClasses["BodyAddon"].GetField("alignWithHead");
        public static FieldInfo field_inFrontOfBody = HARClasses["BodyAddon"].GetField("inFrontOfBody");
        public static FieldInfo field_layerInvert = HARClasses["BodyAddon"].GetField("layerInvert");
        public static FieldInfo field_ageGraphics = HARClasses["BodyAddon"].GetField("ageGraphics");
        public static FieldInfo field_defaultOffsets = HARClasses["BodyAddon"].GetField("defaultOffsets");
        public static FieldInfo field_offsets = HARClasses["BodyAddon"].GetField("offsets");
        public static FieldInfo field_femaleOffsets = HARClasses["BodyAddon"].GetField("femaleOffsets");
        public static PropertyInfo property_Name = HARClasses["BodyAddon"].GetProperty("Name");
        public static MethodInfo method_GetRotationOffset = HARClasses["DirectionalOffset"].GetMethod("GetOffset");
        public static MethodInfo method_GetOffset_ByTypes = HARClasses["RotationOffset"].GetMethod("GetOffset");

        public static int unnamedID = 0;

        public object orig;
        public object ageGraphics;
        public object defaultOffsets_north;
        public object offsets_north;
        public object femaleOffsets_north;
        public bool inFrontOfBody;
        public bool layerInvert;
        public bool alignWithHead;
        public string name;

        public BodyAddon(object orig)
        {
            /*
            Log.Message("field_alignWithHead: " + field_alignWithHead);
            Log.Message("field_inFrontOfBody: " + field_inFrontOfBody);
            Log.Message("field_layerInvert: " + field_layerInvert);
            Log.Message("field_ageGraphics: " + field_ageGraphics);
            Log.Message("field_defaultOffsets: " + field_defaultOffsets);
            Log.Message("field_offsets: " + field_offsets);
            Log.Message("field_femaleOffsets: " + field_femaleOffsets);
            Log.Message("property_Name: " + property_Name);
            Log.Message("method_GetRotationOffset: " + method_GetRotationOffset);
            Log.Message("method_GetOffset_ByTypes: " + method_GetOffset_ByTypes);
            */

            name = (string)property_Name.GetValue(orig);
            if (name == null)
            {
                name = "UnnamedAddon" + unnamedID.ToString();
                ++unnamedID;
            }

            //Log.Message("Initialising BodyAddon, Name: " + name);

            if (!HARClasses["BodyAddon"].IsAssignableFrom(orig.GetType()))
                Log.Error("Toddlers.BodyAddon attempted to initialise wrapper for a non-BodyAddon object " + orig);
            this.orig = orig;
            //Log.Message("orig: " + orig);

            ageGraphics = field_ageGraphics.GetValue(orig);
            //Log.Message("ageGraphics: " + ageGraphics);

            object defaultOffsets = field_defaultOffsets.GetValue(orig);
            //Log.Message("defaultOffsets: " + defaultOffsets);
            if (defaultOffsets != null)
                defaultOffsets_north = method_GetRotationOffset.Invoke(defaultOffsets, new object[] { Rot4.North });
            else
                defaultOffsets_north = null;
            //Log.Message("defaultOffsets_north: " + defaultOffsets_north);

            object offsets = field_offsets.GetValue(orig);
            //Log.Message("offsets: " + offsets);
            if (offsets != null)
                offsets_north = method_GetRotationOffset.Invoke(offsets, new object[] { Rot4.North });
            else
                offsets_north = null;
            //Log.Message("offsets_north: " + offsets_north);

            object femaleOffsets = field_offsets.GetValue(orig);
            //Log.Message("femaleOffsets: " + femaleOffsets);
            if (femaleOffsets != null)
                femaleOffsets_north = method_GetRotationOffset.Invoke(femaleOffsets, new object[] { Rot4.North });
            else
                femaleOffsets_north = null;
            //Log.Message("femaleOffsets_north: " + femaleOffsets_north);

            alignWithHead = (bool)field_alignWithHead.GetValue(orig);
            inFrontOfBody = (bool)field_inFrontOfBody.GetValue(orig);
            layerInvert = (bool)field_layerInvert.GetValue(orig);
        }


        public Vector3 GetNorthOffset(Pawn pawn)
        {
            //Log.Message("defaultOffsets_north: " + defaultOffsets_north);
            //Log.Message("Patch_HAR.method_GetOffsetByTypes: " + Patch_HAR.method_GetOffsetByTypes);
            Vector3 defaultOffset = defaultOffsets_north == null ? Vector3.zero :
                (Vector3?)method_GetOffset_ByTypes.Invoke(defaultOffsets_north,
                    new object[] { false, pawn.story?.bodyType ?? BodyTypeDefOf.Male, pawn.story?.headType ?? HeadTypeDefOf.Stump })
                ?? Vector3.zero;
            //Log.Message("defaultOffset: " + defaultOffset);

            object specificOffsets = pawn.gender == Gender.Female ? femaleOffsets_north : offsets_north;
            Vector3 specificOffset = specificOffsets == null ? Vector3.zero :
                (Vector3?)method_GetOffset_ByTypes.Invoke(specificOffsets,
                    new object[] { false, pawn.story?.bodyType ?? BodyTypeDefOf.Male, pawn.story?.headType ?? HeadTypeDefOf.Stump })
                ?? Vector3.zero;

            Vector3 offset = defaultOffset + specificOffset;

            offset.y = inFrontOfBody ? 0.3f + offset.y : -0.3f - offset.y;
            if (layerInvert)
            {
                offset.y = -offset.y;
            }

            return offset;
        }
    }
}
