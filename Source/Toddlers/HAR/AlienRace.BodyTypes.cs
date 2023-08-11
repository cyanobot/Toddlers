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
    public partial class AlienRace
    {
        public bool bodyTypeAdded_Baby = false;
        public bool bodyTypeAdded_Child = false;
        public BodyTypeDef bodyType_Baby = null;
        public BodyTypeDef bodyType_Child = null;

        public void InitBodyTypes()
        {
            //Log.Message("Starting InitBodyTypes");

            object bodyTypes_obj = field_AlienPartGenerator_bodyTypes.GetValue(alienPartGenerator);
            //Log.Message("bodyTypes_obj: " + (bodyTypes_obj as IEnumerable).ToStringSafeEnumerable());

            List<BodyTypeDef> bodyTypes = bodyTypes_obj as List<BodyTypeDef>;

            foreach (BodyTypeDef bodyTypeDef in bodyTypes)
            {
                if (bodyTypeDef == BodyTypeDefOf.Baby) bodyType_Baby = BodyTypeDefOf.Baby;
                else if (bodyTypeDef.defName.Contains("baby") || bodyTypeDef.defName.Contains("Baby")) bodyType_Baby = bodyTypeDef;

                if (bodyTypeDef == BodyTypeDefOf.Child) bodyType_Child = BodyTypeDefOf.Child;
                else if (bodyTypeDef.defName.Contains("child") || bodyTypeDef.defName.Contains("Child")) bodyType_Child = bodyTypeDef;

                if (bodyType_Baby != null && bodyType_Child != null) break;
            }
            //Log.Message("Body type search complete, bodyType_Baby: " + bodyType_Baby + ", bodyType_Child: " + bodyType_Child);

            //if the race has not been allowed the baby/child body types, add those in
            if (bodyType_Baby == null)
            {
                bodyTypes.Add(BodyTypeDefOf.Baby);
                bodyType_Baby = BodyTypeDefOf.Baby;
                bodyTypeAdded_Baby = true;
            }
            if (bodyType_Child == null)
            {
                bodyTypes.Add(BodyTypeDefOf.Child);
                bodyType_Child = BodyTypeDefOf.Child;
                bodyTypeAdded_Child = true;
            }
            //Log.Message("new bodyTypes_obj: " + (bodyTypes_obj as IEnumerable).ToStringSafeEnumerable());

        }
    }
}
