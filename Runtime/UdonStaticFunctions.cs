using System;
using System.Diagnostics;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using Debug = UnityEngine.Debug;

namespace UdonSharpProfiler {
    public static class UdonStaticFunctions
    {
        [DontUdonProfile]
        public static void Profiler_StartTiming(UdonSharpBehaviour behaviour, string funcName) {
            //Debug.Log($"Profiler_StartTiming: caller: {funcName}");
            
            var root = (DataDictionary)behaviour.GetProgramVariable(UdonProfilerConsts.StopwatchHeapKey);
            var parent = (DataDictionary)behaviour.GetProgramVariable(UdonProfilerConsts.StopwatchHeapParentKey);
            
            var info = new DataDictionary();
            
            info.Add("parent", parent);
            info.Add("name", funcName);
            info.Add("start", Stopwatch.GetTimestamp());
            info.Add("end", -1);
            //info.Add("timer", new DataToken(stopwatch));
            info.Add("children", new DataList());

            if (parent == null) {
                root[funcName] = info;
            }
            else {
                parent["children"].DataList.Add(info);
            }

            behaviour.SetProgramVariable(UdonProfilerConsts.StopwatchHeapKey, root);
            behaviour.SetProgramVariable(UdonProfilerConsts.StopwatchHeapParentKey, info);
        }
        
        [DontUdonProfile]
        public static void Profiler_EndTiming(UdonSharpBehaviour behaviour) {
            var parent = (DataDictionary)behaviour.GetProgramVariable(UdonProfilerConsts.StopwatchHeapParentKey);
            if (parent != null) {
                parent["end"] = Stopwatch.GetTimestamp();
                //((Stopwatch)parent["timer"].Reference).Stop();
                if (parent.TryGetValue("parent", TokenType.DataDictionary, out var value))
                    behaviour.SetProgramVariable(UdonProfilerConsts.StopwatchHeapParentKey, value.DataDictionary);
                else
                    behaviour.SetProgramVariable(UdonProfilerConsts.StopwatchHeapParentKey, null);
            }
        }
    }

    public class DontUdonProfileAttribute : Attribute { }
}
