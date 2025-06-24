using System;

namespace UdonSharpProfiler {
    public static class EmitContextEmitReturnPatch {
        public static void Prefix(ref object __instance) {
            Type instanceType = __instance.GetType();
            UdonSharpAssemblyModuleWrapper module = new(instanceType.GetProperty("Module").GetValue(__instance));

            object doInject = module.GetValueDefault(UdonProfilerConsts.DoInjectTrackerKey);
            if (doInject is true) {
                module.AddCommentTag("Injected End Call Begin");

                // Variable for fake self
                object udonTarget = module.GetValue(UdonProfilerConsts.StopwatchSelfKey);

                // Call stop profiling
                module.AddPush(udonTarget);
                module.AddPush(module.GetConstantValue(__instance, typeof(string), "Profiler_EndTiming"));
                module.AddExtern(__instance,
                    "VRCUdonCommonInterfacesIUdonEventReceiver.__SendCustomEvent__SystemString__SystemVoid");

                module.AddCommentTag("Injected End Call End");
            }
        }
    }
}