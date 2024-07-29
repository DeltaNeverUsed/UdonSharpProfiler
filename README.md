# UdonSharp Profiler
Profiling tool for UdonSharp

# How to use
1. Download and install the project from my [VCC Listing](https://deltaneverused.github.io/VRChatPackages/)
2. Add the prefab from the package's sample folder into you scene
3. Enable the profiler by going to ``Tools/UdonSharpProfiler/Enable`` (click again to disable, having it enabled does make your udon programs slower.)
4. Drag the UdonSharp Behaviours into the ``Targets`` array on the Profiler prefab
5. Enter play mode or play through the VRChat client.
6. Click on the cube from the prefab to write the Trace to the log file
7. Create a trace file by going to ``Tools/UdonSharpProfiler/Save Unity`` Log or ``Tools/UdonSharpProfiler/Save VRChat Log``
8. Load the file into [Perfetto](https://ui.perfetto.dev/)