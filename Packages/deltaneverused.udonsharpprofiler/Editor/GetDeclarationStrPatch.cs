﻿namespace UdonSharpProfiler {
    public class GetDeclarationStrPatch {
        public static void Postfix(ref string __result) {
            if (__result.Contains(UdonProfilerConsts.StopwatchSelfKey))
                __result = __result.Replace("null", "this"); // make sure that self starts out as self and not null
        }
    }
}