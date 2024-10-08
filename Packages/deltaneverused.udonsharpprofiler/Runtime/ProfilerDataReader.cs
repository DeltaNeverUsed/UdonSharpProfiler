﻿using System;
using System.Diagnostics;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using Debug = UnityEngine.Debug;


namespace UdonSharpProfiler {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
    using UnityEditor;
    using System.Linq;
    using UdonSharpEditor;

    [CustomEditor(typeof(ProfilerDataReader))]
    public class ProfileDataReaderEditor : Editor {
        public override void OnInspectorGUI() {
            UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target);
            DrawDefaultInspector();

            if (GUILayout.Button("Get All U# Behaviours")) {
                var blacklisted = new Type[]
                    { typeof(ProfilerDataReader), typeof(UdonProfilerKickoff), typeof(ProfilerController) };
                var t = (ProfilerDataReader)target;

                var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                t.targets = roots.SelectMany(root => root.GetComponentsInChildren<UdonSharpBehaviour>(true))
                    .Where(x => !blacklisted.Contains(x.GetType())).ToArray();
                EditorUtility.SetDirty(target);
            }
        }
    }

#endif

    [DefaultExecutionOrder(1000000000)]
    public class ProfilerDataReader : UdonSharpBehaviour {
        public UdonSharpBehaviour[] targets;
        public bool recording;

        private DataList _packets = new DataList();

        private DataDictionary _process;
        private DataDictionary _thread;

        private long _zeroTimeStamp;

        [DontUdonProfile]
        public void Emit(DataDictionary packet) {
            _packets.Add(packet);
        }

        /// <summary>
        /// Creates DataDict(s) in the form of a Perfetto packet(s) and adds it to the _packets DataList recursively 
        /// </summary>
        /// <param name="node">Packets from the object's profiling dict</param>
        [DontUdonProfile]
        private void EmitPacket(DataDictionary node) {
            var start = (long)((double)node["start"].Long / Stopwatch.Frequency * 1000000d); // in microseconds
            var functionName = node["name"].String;
            var type = (ProfilerEventType)node["type"].Int;

            switch (type) {
                case ProfilerEventType.FunctionCall:
                    var end = (long)((double)node["end"].Long / Stopwatch.Frequency * 1000000d); // in microseconds
                    Emit(PerfettoHelper.CreatePacket()
                        .AddEventName(functionName)
                        .AddTimeStamp(start)
                        .AddDuration(end - start)
                        .AddEventType(PerfettoTrackEventType.TYPE_SLICE_COMPLETE)
                        .AddIds());
                    break;
                case ProfilerEventType.Instant:
                    Emit(PerfettoHelper.CreatePacket()
                        .AddEventName(functionName)
                        .AddTimeStamp(start)
                        .AddEventType(PerfettoTrackEventType.TYPE_INSTANT)
                        .AddIds());
                    break;
                default:
                    break;
            }
        }

        [DontUdonProfile]
        public override void PostLateUpdate() {
            if (recording)
                Emit(PerfettoHelper.CreatePacket()
                    .AddEventName("Profiler Emit")
                    .AddTimeStamp((long)((double)Stopwatch.GetTimestamp() / Stopwatch.Frequency * 1000000d))
                    .AddEventType(PerfettoTrackEventType.TYPE_SLICE_BEGIN)
                    .AddIds());

            foreach (var target in targets) {
                if (!Utilities.IsValid(target))
                    continue;

                if (recording) {
                    // Get every called function from the "root" or for example: Update, Start, or input events
                    var root = (DataList)target.GetProgramVariable(UdonProfilerConsts.StopwatchListKey);
                    var unformattedPackets = root.ToArray();

                    foreach (var unformattedPacket in unformattedPackets) {
                        EmitPacket(unformattedPacket.DataDictionary);
                    }
                }

                // Reset Dict after getting it
                target.SetProgramVariable(UdonProfilerConsts.StopwatchListKey, new DataList());
            }

            if (recording)
                Emit(PerfettoHelper.CreatePacket()
                    .AddEventName("Profiler Emit")
                    .AddTimeStamp((long)((double)Stopwatch.GetTimestamp() / Stopwatch.Frequency * 1000000d))
                    .AddEventType(PerfettoTrackEventType.TYPE_SLICE_END)
                    .AddIds());
        }

        /// <summary>
        /// Write the Prefetto packets to the log
        /// </summary>
        [DontUdonProfile]
        public void WriteEmitToLog() {
            _zeroTimeStamp = DateTime.MaxValue.Ticks;
            var allPackets = _packets.ToArray();

            // Get the lowest timestamp
            foreach (var packet in allPackets) {
                if (packet.DataDictionary["ts"].Long < _zeroTimeStamp)
                    _zeroTimeStamp = packet.DataDictionary["ts"].Long;
            }

            // Adjust the packets to start at the lowest timestamp
            foreach (var packet in allPackets) {
                packet.DataDictionary.AdjustTimeStamp(_zeroTimeStamp);
            }

            VRCJson.TrySerializeToJson(_packets, JsonExportType.Minify, out var result);
            Debug.Log($"{{  \"traceEvents\": {result}, \"displayTimeUnit\": \"us\" }}");
            _packets = new DataList();
        }

        [DontUdonProfile]
        public void EmitStartEvent(string name) {
            if (recording)
                Emit(PerfettoHelper.CreatePacket()
                    .AddEventName(name)
                    .AddTimeStamp((long)((double)Stopwatch.GetTimestamp() / Stopwatch.Frequency * 1000000d))
                    .AddEventType(PerfettoTrackEventType.TYPE_SLICE_BEGIN)
                    .AddIds());
        }

        [DontUdonProfile]
        public void EmitEndEvent(string name) {
            if (recording)
                Emit(PerfettoHelper.CreatePacket()
                    .AddEventName(name)
                    .AddTimeStamp((long)((double)Stopwatch.GetTimestamp() / Stopwatch.Frequency * 1000000d))
                    .AddEventType(PerfettoTrackEventType.TYPE_SLICE_END)
                    .AddIds());
        }

        [DontUdonProfile]
        public override void Interact() {
            WriteEmitToLog();
        }

        [DontUdonProfile]
        private void FixedUpdate() {
            EmitEndEvent("Udon FixedUpdate()");
        }

        [DontUdonProfile]
        private void Update() {
            EmitEndEvent("Udon Update()");
        }

        [DontUdonProfile]
        private void LateUpdate() {
            EmitEndEvent("Udon LateUpdate()");
        }
    }
}