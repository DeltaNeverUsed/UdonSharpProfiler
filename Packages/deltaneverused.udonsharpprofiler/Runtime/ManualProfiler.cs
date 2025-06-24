using System.Diagnostics;
using UdonSharp;

namespace UdonSharpProfiler {
    public static class ManualProfiler {
        [DontUdonProfile]
        public static void EmitInstant(this UdonSharpBehaviour behaviour, string message) {
            ProfilerDataReader profilerDataReader = (ProfilerDataReader)behaviour.GetProgramVariable("__getProfilerDataReader");
            profilerDataReader.Emit(PerfettoHelper.CreatePacket()
                .AddEventName(message)
                .AddTimeStamp((long)((double)Stopwatch.GetTimestamp() / Stopwatch.Frequency * 1000000d))
                .AddEventType(PerfettoTrackEventType.TYPE_INSTANT)
                .AddIds());
        }
    }
}