FOREST SANCTUARY

Open Assets/ForestExperience/Scenes/Forest Sanctuary.unity.
The scene is already enabled as the project's build scene.

Experience
- A stationary first-person viewer in a forest clearing inspired by the two supplied photos.
- 150 scanned foreground/middle-distance trees, 12 distant forest groups, alpine mountain geometry, moss and leaf-litter terrain, grass, ferns and rocks.
- Clear daylight, canopy shadows, atmospheric haze and a gentle breeze.
- The pack's wind recording loops quietly. Occasional synthesized birdsong plays at varying positions in the canopy.
- Head rotation and vertical tracking remain active. Horizontal headset translation is constrained. No walking, teleporting, joystick locomotion or artificial turning.
- A floor-based tracking origin is requested from the runtime. When floor tracking is unavailable, a device-relative height offset of 1.65m is used; adjust ForestRig.fallbackEyeHeight to calibrate that runtime.
- The centre of the clearing is free of large objects for seated and reclining viewpoints.

Touch
- Reach with a tracked controller or, on supported runtimes, a tracked hand.
- 96 nearby grass/fern touch surfaces are available.
- Foliage bends away from fingertips/controllers and returns as the hand moves away.
- Moving contact plays soft rustling. Controllers receive a brief haptic impulse where supported.
- Touching the ground creates small leaf-litter motes, rustling and a slightly stronger controller pulse. The terrain itself remains solid.
- Hand tracking is enabled through XR Hands/OpenXR. Actual availability depends on the headset/runtime. Controller hand visuals are simple glove proxies.

Desktop preview
- Press Play.
- Hold the right mouse button and move the mouse to look around.
- Page Up / Page Down changes eye height between 0.15m and 2.1m.
- Left-click nearby vegetation or ground (within 4m) to simulate touch.

XR configuration
- OpenXR loader enabled for Standalone and Android.
- Controller profiles and OpenXR hand tracking enabled.
- Single-pass instanced stereo, forward rendering and 4x MSAA configured.
- GPU Resident Drawer is disabled for compatibility with the custom foliage shader and editor preview rendering.
- PC VR is the assumed target because no headset was specified. Use Windows build support and the headset's active OpenXR runtime for PC VR.
- Android settings are prepared, but a standalone Quest device build and performance profile have not been verified.
- This macOS editor session provides desktop previews and simulated tracking checks; hardware stereo rendering, hand tracking, haptics and sustained headset frame rate still need validation on the target headset.

Tools
- Forest Experience > Build forest clearing rebuilds this scene deterministically.
- Forest Experience > Save preview images saves standing, seated and ground views into ForestReports.
- Forest Experience > Validate scene checks material/shader references, terrain, audio, rig, touch surfaces and XR loader settings.
- Scene-specific scripts and materials live in Assets/ForestExperience. Asset-pack originals are reused without modifying their meshes/textures.
- Validation reports, image previews and Play mode simulation results are in ForestReports at the project root.
