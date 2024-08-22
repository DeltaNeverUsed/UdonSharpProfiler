using UdonSharp;
using VRC.SDK3.Data;

namespace UdonSharpProfiler {
    public static class ManualProfiler {
        [DontUdonProfile]
        public static void EmitInstant(this UdonSharpBehaviour behaviour, string message) {
            var fakeSelf = (UdonSharpBehaviour)behaviour.GetProgramVariable(UdonProfilerConsts.StopwatchSelfKey);
            var list = (DataList)fakeSelf.GetProgramVariable(UdonProfilerConsts.StopwatchListKey);

            var info = new DataDictionary();

            info.Add("name", message);
            info.Add("start", System.Diagnostics.Stopwatch.GetTimestamp());
            info.Add("type", (int)ProfilerEventType.Instant);

            list.Add(info);

            fakeSelf.SetProgramVariable(UdonProfilerConsts.StopwatchListKey, list);
        }
    }
}