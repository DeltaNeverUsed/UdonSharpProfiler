using System.Diagnostics;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace UdonSharpProfiler {
    [DefaultExecutionOrder(-1000000000)]
    public class UdonProfilerKickoff : UdonSharpBehaviour {
        private ProfilerDataReader _profilerDataReader;

        [DontUdonProfile]
        private void Start() {
            if (!Utilities.IsValid(_profilerDataReader))
                _profilerDataReader = GetComponent<ProfilerDataReader>();
        }

        [DontUdonProfile]
        private void EmitStartEvent(string name) {
            _profilerDataReader.Emit(PerfettoHelper.CreatePacket()
                .AddEventName(name)
                .AddTimeStamp((long)((double)Stopwatch.GetTimestamp() / Stopwatch.Frequency * 1000000d))
                .AddEventType(PerfettoTrackEventType.TYPE_SLICE_BEGIN)
                .AddIds());
        }

        [DontUdonProfile]
        private void FixedUpdate() {
            EmitStartEvent("Udon FixedUpdate()");
        }

        [DontUdonProfile]
        private void Update() {
            EmitStartEvent("Udon Update()");
        }

        [DontUdonProfile]
        private void LateUpdate() {
            EmitStartEvent("Udon LateUpdate()");
        }
    }
}
