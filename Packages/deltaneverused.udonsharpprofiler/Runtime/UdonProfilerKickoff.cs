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
        private void FixedUpdate() {
            _profilerDataReader.EmitStartEvent("Udon FixedUpdate()");
        }

        [DontUdonProfile]
        private void Update() {
            _profilerDataReader.EmitStartEvent("Udon Update()");
        }

        [DontUdonProfile]
        private void LateUpdate() {
            _profilerDataReader.EmitStartEvent("Udon LateUpdate()");
        }
    }
}
