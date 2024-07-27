using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using File = System.IO.File;

namespace UdonSharpProfiler {
    public static class GetPerfettoTrace {
        public static void SaveTrace(string trace) {
            var saveFolder = EditorUtility.SaveFilePanel("Save Trace File", "", "UdonTrace.json", "json");
            if (string.IsNullOrEmpty(saveFolder)) {
                Injections.PrintError("No folder selected!");
                return;
            }

            File.WriteAllText(saveFolder, trace);
        }

        [MenuItem("Tools/UdonSharpProfiler/Save Unity Log")]
        public static void GetUnityLog() {
            string logFilePath = Application.platform switch {
                RuntimePlatform.WindowsEditor => Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity",
                    "Editor", "Editor.log"),
                RuntimePlatform.OSXEditor => Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Logs",
                    "Unity", "Editor.log"),
                RuntimePlatform.LinuxEditor => Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".config", "unity3d",
                    "Editor.log"),
                _ => ""
            };

            if (!File.Exists(logFilePath)) {
                Injections.PrintError("Log file not found.");
                return;
            }

            string logContent;
            using (var file = File.Open(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(file)) {
                logContent = reader.ReadToEnd();
            }

            var traceIndex = logContent.LastIndexOf("{  \"traceEvents\": ", StringComparison.Ordinal);

            if (traceIndex == -1) {
                Injections.PrintError("Perfetto log not found.");
                return;
            }

            SaveTrace(logContent.Substring(traceIndex,
                logContent.IndexOf("\n", traceIndex, StringComparison.Ordinal) - traceIndex));
        }
    }
}