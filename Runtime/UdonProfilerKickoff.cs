using System.Diagnostics;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace UdonSharpProfiler {
    [DefaultExecutionOrder(-1000000000)]
    public class UdonProfilerKickoff : UdonSharpBehaviour {
        private ProfileDataReader _profileDataReader;

        [DontUdonProfile]
        private void Start() {
            if (!Utilities.IsValid(_profileDataReader))
                _profileDataReader = GetComponent<ProfileDataReader>();
        }

        [DontUdonProfile]
        private void EmitStartEvent(string name) {
            _profileDataReader.Emit(PerfettoHelper.CreatePacket()
                .AddEventName(name)
                .AddTimeStamp(Stopwatch.GetTimestamp() / Stopwatch.Frequency * 1000000)
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
