using System;
using System.Diagnostics;
using System.Text;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using Debug = UnityEngine.Debug;


namespace UdonSharpProfiler {
    [DefaultExecutionOrder(1000000000)]
    public class ProfilerDataReader : UdonSharpBehaviour {
        public bool recording;

        private DataList _packets = new DataList();
        private readonly DataList _queuedPackets = new DataList();

        private DataDictionary _process;
        private DataDictionary _thread;
        
        private double _conversionRate;
        private bool _busy;
        
        private readonly DataDictionary _startTemplate = PerfettoHelper.CreatePacket()
            .AddEventName("")
            .AddTimeStamp(-1)
            .AddEventType(PerfettoTrackEventType.TYPE_SLICE_BEGIN)
            .AddIds();
        
        private readonly DataDictionary _endTemplate = PerfettoHelper.CreatePacket()
            .AddTimeStamp(-1)
            .AddEventType(PerfettoTrackEventType.TYPE_SLICE_END)
            .AddIds();

        [DontUdonProfile]
        private void OnEnable() {
            _conversionRate = 1d / Stopwatch.Frequency * 1000000d;
        }

        [DontUdonProfile]
        private void Update() {
            EmitEndEvent("Udon Update()");
        }

        [DontUdonProfile]
        private void FixedUpdate() {
            EmitEndEvent("Udon FixedUpdate()");
        }

        [DontUdonProfile]
        private void LateUpdate() {
            EmitEndEvent("Udon LateUpdate()");
        }

        [DontUdonProfile]
        public void Emit(DataDictionary packet) {
            _packets.Add(packet);
        }

        /// <summary>
        ///     Creates DataDict(s) in the form of a Perfetto packet(s) and adds it to the _packets DataList recursively
        /// </summary>
        /// <param name="node">Packets from the object's profiling dict</param>
        [DontUdonProfile]
        private void EmitPacket(DataDictionary node) {
            bool isStart = node.ContainsKey("start");
            if (isStart) {
                DataDictionary cloned = _startTemplate.ShallowClone();
                cloned["name"] =  node["name"].String;
                cloned["ts"] = (long)(node["start"].Long * _conversionRate);
                Emit(cloned);
            }
            else {
                DataDictionary cloned = _endTemplate.ShallowClone();
                cloned["ts"] = (long)(node["end"].Long * _conversionRate);
                Emit(cloned);
            }
        }

        [DontUdonProfile]
        public void QueuePacket(DataDictionary packet) {
            if (recording)
                _queuedPackets.Add(packet);
        }

        /// <summary>
        ///     Write the Prefetto packets to the log
        /// </summary>
        [DontUdonProfile]
        public void WriteEmitToLog() {
            if (_busy) {
                Debug.Log("Profiler busy emitting");
                return;
            }

            _busy = true;
            _tokens = _queuedPackets.ToArray();
            _queuedPackets.Clear();
            _tokenIndex = 0;
            EmitAllLoop();
        }

        private DataToken[] _tokens;
        private int _tokenIndex;

        [DontUdonProfile]
        public void EmitAllLoop() {
            long start = (long)(Stopwatch.GetTimestamp() * _conversionRate);
            
            Stopwatch stopwatch = Stopwatch.StartNew();
            TimeSpan maxExecutionTime = TimeSpan.FromSeconds(5);
            int tokenLen = _tokens.Length;
            for (; _tokenIndex < tokenLen; _tokenIndex++) {
                EmitPacket(_tokens[_tokenIndex].DataDictionary);
                if (stopwatch.Elapsed > maxExecutionTime)
                    break;
            }
            
            stopwatch.Stop();

            SendCustomEventDelayedFrames(_tokenIndex != tokenLen ? nameof(EmitAllLoop) : nameof(FinishEmitting), 1);

            Emit(PerfettoHelper.CreatePacket()
                .AddEventName("Profiler Emit queued loop")
                .AddTimeStamp(start)
                .AddEventType(PerfettoTrackEventType.TYPE_SLICE_COMPLETE)
                .AddDuration((long)(Stopwatch.GetTimestamp() * _conversionRate) - start)
                .AddIds());
        }

        [DontUdonProfile]
        public void FinishEmitting() {
            VRCJson.TrySerializeToJson(_packets, JsonExportType.Minify, out DataToken result);
            StringBuilder sb = new StringBuilder();
            sb.Append("{  \"traceEvents\": ");
            sb.Append(result);
            sb.Append("\"displayTimeUnit\": \"us\" }");
            Debug.Log(sb);
            _packets = new DataList();
            _busy = false;
        }

        [DontUdonProfile]
        public void EmitStartEvent(string functionName) {
            if (recording)
                Emit(PerfettoHelper.CreatePacket()
                    .AddEventName(functionName)
                    .AddTimeStamp((long)((double)Stopwatch.GetTimestamp() / Stopwatch.Frequency * 1000000d))
                    .AddEventType(PerfettoTrackEventType.TYPE_SLICE_BEGIN)
                    .AddIds());
        }

        [DontUdonProfile]
        public void EmitEndEvent(string functionName) {
            if (recording)
                Emit(PerfettoHelper.CreatePacket()
                    .AddEventName(functionName)
                    .AddTimeStamp((long)((double)Stopwatch.GetTimestamp() / Stopwatch.Frequency * 1000000d))
                    .AddEventType(PerfettoTrackEventType.TYPE_SLICE_END)
                    .AddIds());
        }

        [DontUdonProfile]
        public override void Interact() {
            WriteEmitToLog();
        }
    }
}