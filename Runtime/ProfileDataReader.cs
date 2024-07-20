using System;
using System.Linq;
using TMPro;
using UdonSharp;
using UdonSharpEditor;
using UdonSharpProfiler;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

/*
#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(ProfileDataReader))]
    public class ProfileDataReaderEditor : Editor {
        private bool _alreadyDone;
        
        public override void OnInspectorGUI() {
            if (!_alreadyDone) {
                _alreadyDone = true;
                var t = (ProfileDataReader)target;

                var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                t.targets = roots.SelectMany(root => root.GetComponentsInChildren<UdonSharpBehaviour>(true)).ToArray();
            }
            
            UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target);
            DrawDefaultInspector();
        }
    }

#endif
*/

[DefaultExecutionOrder(1000000000)]
public class ProfileDataReader : UdonSharpBehaviour {
    public UdonSharpBehaviour target;

    public TMP_InputField text;

    public bool recording;

    private DataList _packets = new DataList();

    private DataDictionary _process;
    private DataDictionary _thread;

    private long _zeroTimeStamp;

    [DontUdonProfile]
    private void Emit(DataDictionary packet) {
        _packets.Add(packet);
    }

    [RecursiveMethod, DontUdonProfile]
    private void EmitTree(DataDictionary node) {
        var start = node["start"].Long / 10;
        var end = node["end"].Long / 10;
        var functionName = node["name"].String;
        
        //Debug.Log($"{name}: {timer}");
        
        Emit(PerfettoHelper.CreatePacket()
            .AddEventName(functionName)
            .AddTimeStamp(start)
            .AddDuration(end - start)
            .AddEventType(PerfettoTrackEventType.TYPE_SLICE_COMPLETE)
            .AddIds());
        
        var children = node["children"].DataList.ToArray();
        foreach (var child in children) {
            EmitTree(child.DataDictionary);
        }
    }
    
    [DontUdonProfile]
    public override void PostLateUpdate() {
        //foreach (var target in targets) {
        //    if (!Utilities.IsValid(target))
        //        continue;
            
            if (recording) {
                var root = (DataDictionary)target.GetProgramVariable(UdonProfilerConsts.StopwatchHeapKey);
                var keys = root.GetKeys().ToArray();

                foreach (var key in keys) {
                    EmitTree(root[key].DataDictionary);
                }
            }
            
            // Reset Dict after getting it
            target.SetProgramVariable(UdonProfilerConsts.StopwatchHeapKey, new DataDictionary());
        //}
    }

    [DontUdonProfile]
    public void WriteEmitToLog() {
        /*var tempString = "{";

       
        var allPackets = _packets.ToArray();
        foreach (var packet in allPackets) {
            var j = VRCJson.TrySerializeToJson(packet, JsonExportType.Beautify, out var result);
            if (!j) {
                Debug.LogError("Failed to serialize, skipping: " + result);
                continue;
            }

            tempString += "\"packet\": " + result.String + "\n";
        }

        tempString += "}*/
        
        _zeroTimeStamp = DateTime.MaxValue.Ticks;
        var allPackets = _packets.ToArray();
        foreach (var packet in allPackets) {
            if (packet.DataDictionary["ts"].Long < _zeroTimeStamp)
                _zeroTimeStamp = packet.DataDictionary["ts"].Long;
        }
        
        foreach (var packet in allPackets) {
            packet.DataDictionary.AdjustTimeStamp(_zeroTimeStamp);
        }
        
        VRCJson.TrySerializeToJson(_packets, JsonExportType.Minify, out var result);
        Debug.Log($"{{  \"traceEvents\": {result}, \"displayTimeUnit\": \"us\" }}");
    }
}
