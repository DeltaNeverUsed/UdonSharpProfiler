using UdonSharp;
using UdonSharp.Lib.Internal;
using UnityEngine;
using VRC.Udon;

namespace UdonSharpProfiler {
    public static class UdonProfilerConsts {
        public const string StopwatchSelfKey = "__refl_stopwatch_self";
        public const string StopwatchNameKey = "__refl_stopwatch_name";

        public const string DoInjectTrackerKey = "__refl_inject_tracker";

        [DontUdonProfile]
        public static T GetComponentNonProfiled<T>(GameObject instance) where T : UdonSharpBehaviour {
            UdonBehaviour[] udonBehaviours = (UdonBehaviour[])instance.GetComponents(typeof(UdonBehaviour));
            long targetID = UdonSharpBehaviour.GetUdonTypeID<T>();

            foreach (UdonBehaviour behaviour in udonBehaviours) {
#if UNITY_EDITOR
                if (behaviour.GetProgramVariableType(CompilerConstants.UsbTypeIDHeapKey) == null)
                    continue;
#endif
                object idValue = behaviour.GetProgramVariable(CompilerConstants.UsbTypeIDHeapKey);
                if (idValue != null && (long)idValue == targetID)
                    return (T)(Component)behaviour;
            }

            return null;
        }
    }
}