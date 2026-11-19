# HCI Project 2 – Team 15: Augmented Scuba Diving

A hands-only VR diving assistant for the Meta Quest, built in Unity for Intro to HCI.
The diver gets an AR-style interface on the wrists and in the visor: oxygen and battery, a radio to call the crew, navigation to crewmates, an SOS, and a flashlight that still works when a mud storm hits.

Course page: [class wiki](https://hciintro.plopes.org/wiki/doku.php?id=project:vr:start)

## Status

This branch (`all_setting_done`) is the version we plan to demo.
All mandatory features and the graduate-student mandatory features are in and have been checked, so the demo can be run from this version as it is.

We may still add a few more fish and extra divers before the demo. Those would only be additions to the scene and should not change any of the controls below.

## What is in this version

- **Wrist layout.** The round menu (INFO, SOS, CALL, FIND, CREW) sits on the left forearm and is pressed with the right hand. The flashlight button sits on the right wrist and is pressed with the left hand.
- **Oxygen and battery always on top.** Both bars are at the very top of the view, so you never have to open a menu to read them. The battery bar turns orange and plays a warning at 15%.
- **Swimming sea life.** Animated fish (Royal Gramma, Butterfly Fish, Lionfish and the original fish), a turtle, a submarine, coral and seaweed. Elina swims next to the submarine as a diver you can call and navigate to.
- **Clearer water.** The underwater fog is much lighter and the surface uses a clear lagoon material, so the reef and the crew are visible from further away.
- **Thumbs up / thumbs down.** Underwater, a right-hand thumbs up swims up and a thumbs down swims down. It only works once your head is below the surface.
- **New call voices.** Elina and Eliza now have their own recorded voices for both incoming and outgoing calls, next to Pedro, Romain and Suzie.
- **Flashlight for the mud storm.** With the light on, the hand pointer gets brighter and thicker, so you can still see where your hand is pointing when the mud storm takes away visibility.

## How to run it

1. Install Unity **6000.3.27f2** with the Android build module.
2. Clone the repo and add the folder in Unity Hub (the folder that contains `Assets`, `Packages` and `ProjectSettings`).
3. Wait for the first import, then open `Assets/Diving Simulation/Scenes/Simulation.unity`.
4. Optional: run **HCI > Validate Hands Demo** from the top menu. It only reads the scene and reports missing references in the Console.
5. For the headset: **File > Build Profiles > Android**, pick the connected Quest as the run device and press **Build and Run**.

The XR setup is for an Android build installed on the Quest. Pressing Play on the PC is only meant for a quick check with keyboard and mouse.

Do not use the controllers. Put them down and hold your hands out in front of the headset until they are tracked.

## Controls

### Hands (Quest)

| What you want to do | Gesture |
|---|---|
| Swim forward | Make a fist with the **left hand** and hold it. You move where you are looking. Open the hand to stop. |
| Go up / go down | Underwater, **right-hand thumbs up / thumbs down**. |
| Forward and up/down together | Left fist and right thumb at the same time. |
| Press a button close to you | Touch it with your index finger. |
| Press a button far away | Point the hand ray at it and close your open hand into a fist once. |
| Open / close the screen menu | Left palm facing out, swipe right / left. |
| Arm menu | Press INFO, SOS, CALL, FIND or CREW on the left forearm with the right hand. |
| Flashlight | Press the button on the right wrist with the left hand. |
| Make a call | Open CALL and tap a contact, or choose FREQ / PHONE, type the number and press CALL. |
| Answer / hang up | ANSWER / END on the call popup. |

If a hand is lost by the tracking, its input goes back to neutral. Nothing gets clicked just because the hand shows up again.

### Keyboard and mouse (PC Play mode)

| Input | Action |
|---|---|
| W / A / S / D | Move relative to the view |
| Hold right mouse button | Look around |
| Left click | Press buttons |
| E or U / Q or J | Up / down (underwater only) |
| M | Open / close the screen menu |

## Getting into the water

You start on the land side, so going straight down does not work. Look towards the sea (+X / +Z in the scene) and swim forward until your head is under the surface. The reef, the fish, the turtle, the submarine and Elina are in the area right in front of the start point. FIND > Elina gives you an arrow to her.

Some models are loaded from `Resources` when the scene starts, so a few prefabs look empty in Edit mode. That is expected.

## Calling the crew

| Crew | PHONE extension | FREQ |
|---|---|---|
| Pedro | 2001 | shown in the contact list |
| Romain | 2002 | shown in the contact list |
| Suzie | 2003 | shown in the contact list |
| Eliza | 2004 | shown in the contact list |
| Elina | 2005 | shown in the contact list |

Frequencies are assigned between 100 and 999 every time the scene starts, the extensions stay fixed. **1001 is SOS** in both modes. SOS plays the rescue message and then restarts the scene, so try it last.

Calls are recorded voice lines played inside the simulation. There is no real phone network or live microphone.

## Project layout

Scene hierarchy of `Simulation.unity`:

| Root object | What is inside |
|---|---|
| `00_Systems` | Dive information, calls, UI input, pointer, top HUD |
| `01_XR_Hands_Rig` | Head and hand tracking, movement |
| `02_Hand_UI` | Screen menu, left-arm menu, right-wrist flashlight, call and info panels |
| `03_Reef_and_Marine_Life` | Fish, turtle, coral, seaweed, submarine, swimming Elina |
| `04_Environment` | Terrain, water surface, lighting |
| `05_Crew_Boats` | Crew boats |
| `99_Original_References_Inactive` | Old UI and creatures kept as a reference, all inactive |

Our scripts are in `Assets/Diving Simulation/Scripts`. The ones most worth reading first:

- `HandsInput.cs`, `HandUIInteractor.cs` – gesture detection and button selection
- `SimpleMovement.cs`, `ThumbsUpAscend.cs` – swimming
- `DiveStatusHUD.cs` – oxygen and battery at the top
- `HandPointerLight.cs`, `MudStorm.cs` – flashlight and mud storm
- `SimpleDial.cs`, `CallTowerManager.cs`, `CallDashboardManager.cs`, `IncomingCallPopup.cs` – calls
- `World/CreatureSwim.cs`, `World/ClearWaterLook.cs`, `World/ReefModel.cs` – sea life and water

`README_KO.md` and the `Docs/` folder are my working notes in Korean (code walkthrough, call setup, world setup, checklists). They were written before the headset run, so some of them still say the device test is pending. That test was done afterwards on the Quest build.

`final-tips/`, `tutorial-arm-hand/` and `tutorial-images/` are the original course material and are left unchanged.

## Assets and credits

- Coral reef, seaweed and submarine: [Grab3D](https://grab3d.com/license/), CC0 1.0. The original license files are kept in `Assets/ThirdParty/Grab3D`.
- Fish and turtle: from the ocean assets that were already in the project.
- Voices: recorded by the team members.
- The Elina diver is built from basic Unity shapes.

## Known limits

- The turtle follows a path and turns smoothly, but it has no flipper animation because the model has no bones.
- Sea life follows fixed paths. There is no flocking or obstacle avoidance.
- Thumbs up / down does nothing above the surface. This is on purpose.
