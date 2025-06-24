# UdonSharp Profiler
Profiling tool for UdonSharpd

# How to use
1. Download and install the project from my [VCC Listing](https://deltaneverused.github.io/VRChatPackages/)
2. Add the prefab from the package's sample folder into you scene, be sure not to rename it.
3. Enable the profiler by going to ``Tools/UdonSharpProfiler/Enable`` (click again to disable, having it enabled does make your udon programs slower).
4. Enter play mode or play through the VRChat client.
5. Press ``O`` to start recording.
6. Press ``P`` to stop recording. Be patient after pressing, depending on the size, it might take a couple seconds to process all the events.
7. Create a trace file by going to ``Tools/UdonSharpProfiler/Save Unity`` Log or ``Tools/UdonSharpProfiler/Save VRChat Log``
8. Load the file into [Perfetto](https://ui.perfetto.dev/).

By default it won't be recording until you press ``O``. You can tell it to start recording as soon as the game starts but toggling the ``recording`` boolean on the Profiler game object.