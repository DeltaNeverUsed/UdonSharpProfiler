# UdonSharp Profiler
Profiling tool for UdonSharp

# How to use
1. Download and install the project from my [VCC Listing](https://deltaneverused.github.io/VRChatPackages/)
2. Add the prefab from the package's sample folder into you scene
3. Enable the profiler by going to ``Tools/UdonSharpProfiler/Enable`` (click again to disable, having it enabled does make your udon programs slower.)
4. Drag the UdonSharp Behaviours into the ``Targets`` array on the Profiler prefab
5. Enter play mode or play through the VRChat client.
6. Press ``O`` to start recording.
7. Press ``P`` to stop recording.
8. Create a trace file by going to ``Tools/UdonSharpProfiler/Save Unity`` Log or ``Tools/UdonSharpProfiler/Save VRChat Log``
9. Load the file into [Perfetto](https://ui.perfetto.dev/)