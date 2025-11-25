using HarmonyLib;
using LudeonTK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Toddlers
{
    /*
    [HarmonyPatch]
    public static class HARTestPatches_ConditionSatisfied
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            List<Type> types = AccessTools.TypeByName("AlienRace.ExtendedGraphics.Condition").AllSubclassesNonAbstract();
            foreach (Type type in types)
            {
                MethodBase method = AccessTools.Method(type, "Satisfied");
                if (method != null) yield return method;
            }
        }

        public static void Prefix(object __instance)
        {
            LogUtil.DebugLog("HARTestPatches_ConditionSatisfied - Condition: "
                + __instance
                );
        }
    }
    */

    [HarmonyPatch]
    public static class HARTestPatches_GetPath
    {
        public static MethodBase TargetMethod()
        {
            Traverse trav = Traverse.Create(HARCompat.t_AlienPartGenerator);
            Type t_extendedGraphicTop = (Type)trav.Type("ExtendedGraphicTop").GetValue();

            return AccessTools.Method(t_extendedGraphicTop, "GetPath", new Type[] { typeof(Pawn), typeof(int).MakeByRefType(), typeof(int?), typeof(string) }); ;
        }

        public static void Postfix(Pawn pawn, ref int sharedIndex, int? savedIndex, string pathAppendix, string __result, object __instance)
        {
            //if (true)
            if (ToddlerUtility.IsToddler(pawn))
            {
                LogUtil.DebugLog($"HARTestPatches_GetPath - pawn: {pawn}, " +
                $"sharedIndex: {sharedIndex}, savedIndex: {savedIndex}, " +
                $"pathAppendix: {pathAppendix}, " +
                $"result: {__result}"
                );
            }            
        }
    }

    [HarmonyPatch]
    public static class HARTestPatches_GetBestGraphic
    {
        public static MethodBase TargetMethod()
        {
            Traverse trav = Traverse.Create(HARCompat.t_AlienPartGenerator);
            Type t_extendedGraphicTop = (Type)trav.Type("ExtendedGraphicTop").GetValue();
            MethodBase methodBase = AccessTools.Method(t_extendedGraphicTop, "GetBestGraphic");

            LogUtil.DebugLog($"t_extendedGraphicTop: {t_extendedGraphicTop}, " +
                $"methodBase: {methodBase}");

            return methodBase;
        }

        public static Type t_ExtendedGraphicsPawnWrapper;
        public static PropertyInfo p_WrappedPawn;
        public static Type t_ResolveData;
        public static FieldInfo f_bodyPart;


        public static Type T_ExtendedGraphicsPawnWrapper
        {
            get
            {
                if (t_ExtendedGraphicsPawnWrapper == null)
                {
                    t_ExtendedGraphicsPawnWrapper = AccessTools.TypeByName("AlienRace.ExtendedGraphics.ExtendedGraphicsPawnWrapper");
                    LogUtil.DebugLog($"t_ExtendedGraphicsPawnWrapper: {t_ExtendedGraphicsPawnWrapper}");
                }
                return t_ExtendedGraphicsPawnWrapper;
            }
        }

        public static PropertyInfo P_WrappedPawn
        {
            get
            {
                if ( p_WrappedPawn == null)
                {
                    p_WrappedPawn = AccessTools.Property(T_ExtendedGraphicsPawnWrapper, "WrappedPawn");
                    LogUtil.DebugLog($"p_WrappedPawn: {p_WrappedPawn}");
                }
                return p_WrappedPawn;
            }
        }

        public static Type T_ResolveData
        {
            get
            {
                if (t_ResolveData == null)
                {
                    t_ResolveData = AccessTools.TypeByName("AlienRace.ExtendedGraphics.ResolveData");
                }
                return t_ResolveData;
            }
        }

        public static FieldInfo F_bodyPart
        {
            get
            {
                if ( f_bodyPart == null)
                {
                    f_bodyPart = AccessTools.Field(T_ResolveData, "bodyPart");
                    LogUtil.DebugLog($"f_bodyPart: {f_bodyPart}");
                }
                return f_bodyPart;
            }
        }

        public static IEnumerable GetSubGraphics(object ExtendedGraphic, object extendedGraphicsPawnWrapper, object resolveData)
        {
            MethodInfo m_GetSubGraphics = AccessTools.Method(
                            ExtendedGraphic.GetType(), "GetSubGraphics",
                            new Type[] { T_ExtendedGraphicsPawnWrapper, T_ResolveData });

            object objectresult = m_GetSubGraphics.Invoke(ExtendedGraphic,
                        new object[] { extendedGraphicsPawnWrapper, resolveData });

            return (IEnumerable)objectresult;
        }

        public static void Postfix(object __instance, object pawn, object data, object __result)
        {
            Pawn innerPawn = (Pawn)P_WrappedPawn.GetValue(pawn);
            if (ToddlerUtility.IsToddler(innerPawn))
            {
                LogUtil.DebugLog($"HARTestPatches_GetBestGraphic - " +
                $"instance: {__instance}, " +
                $"pawnwrapper: {pawn}, " +
                $"pawn: {innerPawn}, " +
                $"data: {data}, " +
                $"result: {__result}"
                );

                /*
                //Pair<int, IExtendedGraphic> bestGraphic = new Pair<int, IExtendedGraphic>(0, this);
                Pair<int, object> bestGraphic = new Pair<int, object>(0, __instance);

                //Stack<Pair<int, IEnumerator<IExtendedGraphic>>> stack = new Stack<Pair<int, IEnumerator<IExtendedGraphic>>>();
                Stack<Pair<int, IEnumerator>> stack = new Stack<Pair<int, IEnumerator>>();

                //stack.Push(new Pair<int, IEnumerator<IExtendedGraphic>>(1, GetSubGraphics(pawn, data).GetEnumerator()));
                stack.Push(new Pair<int, IEnumerator>(1, GetSubGraphics(__instance, pawn, data).GetEnumerator()));

                //while (stack.Count > 0 && (bestGraphic.Second == this || bestGraphic.First < stack.Peek().First))
                while (stack.Count > 0 && (bestGraphic.Second == __instance || bestGraphic.First < stack.Peek().First))
                {
                    //Pair<int, IEnumerator<IExtendedGraphic>> currentGraphicSet = stack.Pop();
                    Pair<int, IEnumerator> currentGraphicSet = stack.Pop();

                    //while (currentGraphicSet.Second.MoveNext())
                    while (currentGraphicSet.Second.MoveNext())
                    {
                        //IExtendedGraphic current = currentGraphicSet.Second.Current;
                        object current = currentGraphicSet.Second.Current;

                        //if (current == null || !current.IsApplicable(pawn, ref data))
                        if (current == null || !current.IsApplicable(pawn, ref data))       
                        {
                            continue;
                        }


                    }
                }
                */
            }
        }
    }
        

    [HarmonyPatch("AlienRace.AlienRenderTreePatches", "BodyGraphicForPrefix")]
    public static class HARTestPatches_BodyGraphicForPrefix
    {
        public static MethodInfo m_RegenerateResolveData;
        public static FieldInfo f_sharedIndex;
        public static FieldInfo f_alienComp;
        public static FieldInfo f_bodyVariant;

        public static MethodInfo M_RegenerateResolveData
        {
            get
            {
                if (m_RegenerateResolveData == null)
                {
                    m_RegenerateResolveData = AccessTools.Method(
                        AccessTools.TypeByName("AlienRace.AlienRenderTreePatches"), "RegenerateResolveData");
                }
                return m_RegenerateResolveData;
            }
        }
        public static FieldInfo F_sharedIndex
        {
            get
            {
                if (f_sharedIndex == null)
                {
                    f_sharedIndex = AccessTools.Field(HARCompat.t_PawnRenderResolveData, "sharedIndex");
                    LogUtil.DebugLog($"f_sharedIndex: {f_sharedIndex}," +
                        $"t_PawnRenderResolveData: {HARCompat.t_PawnRenderResolveData}");
                }
                return f_sharedIndex;
            }
        }
        public static FieldInfo F_alienComp
        {
            get
            {
                if (f_alienComp == null)
                {
                    f_alienComp = AccessTools.Field(HARCompat.t_PawnRenderResolveData, "alienComp");
                    LogUtil.DebugLog($"f_alienComp: {f_alienComp}");
                }
                return f_alienComp;
            }
        }
        public static FieldInfo F_bodyVariant
        {
            get
            {
                if (f_bodyVariant == null)
                {
                    f_bodyVariant = AccessTools.Field(HARCompat.t_AlienComp, "bodyVariant");
                    LogUtil.DebugLog($"f_bodyVariant: {f_bodyVariant}");
                }
                return f_bodyVariant;
            }
        }


        public static void Postfix(PawnRenderNode_Body __0, Pawn __1, ref Graphic __2)
        {
            PawnRenderNode_Body __instance = __0;
            Pawn pawn = __1;
            Graphic __result = __2;

            //if (true)
            if (ToddlerUtility.IsToddler(pawn))
            {
                LogUtil.DebugLog($"HARTestPatches_BodyGraphicForPrefix Postfix - " +
                    $"__instance: {__instance}, " +
                    $"pawn: {pawn}, " +
                    $"__result: {__result}, " +
                    $"pawn.def type: {pawn.def.GetType()}, "
                    );

                AlienRace wrapper = HARUtil.GetAlienRaceWrapper(pawn);
                //LogUtil.DebugLog($"wrapper: {wrapper}, graphicPaths: {wrapper.graphicPaths}," +
                //    $"hash code: {wrapper.graphicPaths.GetHashCode()}"
                //    );

                object pawnRenderData = M_RegenerateResolveData.Invoke(null, new object[] { pawn });
               // LogUtil.DebugLog($"pawnRenderData: {pawnRenderData}");

                int sharedIndex = (int)F_sharedIndex.GetValue(pawnRenderData);
                //LogUtil.DebugLog($"sharedIndex: {sharedIndex}");

                object alienComp = F_alienComp.GetValue(pawnRenderData);
                //LogUtil.DebugLog($"alienComp: {alienComp}");

                int bodyVariant = (int)F_bodyVariant.GetValue(alienComp);
                //LogUtil.DebugLog($"bodyVariant: {bodyVariant}");

                /*
                Traverse traverse = Traverse.Create(wrapper.graphicPaths);
                string bodyPath = traverse.Field("body").Method("GetPath", new object[]
                {
                    pawn, sharedIndex, (bodyVariant < 0) ? null : new int?(bodyVariant)
                }).GetValue() as string;
                */

                LogUtil.DebugLog($"pawnRenderData: {pawnRenderData}, " +
                    $"sharedIndex: {sharedIndex}, " +
                    $"alienComp: {alienComp}, " +
                    $"bodyVariant: {bodyVariant}, "
                    //$"bodyPath: {bodyPath}"
                    );

            }
        }
    }
}
