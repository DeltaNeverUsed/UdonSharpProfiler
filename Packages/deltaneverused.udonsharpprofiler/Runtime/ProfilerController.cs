using UdonSharp;
using UnityEngine;

namespace UdonSharpProfiler {
    public class ProfilerController : UdonSharpBehaviour {
        public ProfilerDataReader profiler;

        [Header("Controls")] 
        public KeyCode startRecording = KeyCode.O;
        public KeyCode stopRecording = KeyCode.P;

        [DontUdonProfile]
        public void Update() {
            if (Input.GetKeyDown(startRecording))
                profiler.recording = true;
            if (Input.GetKeyDown(stopRecording)) {
                profiler.recording = false;
                profiler.WriteEmitToLog();
            }
        }
    }
}