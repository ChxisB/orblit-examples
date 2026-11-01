# Parity

One row per Dart test file that has been ported. A row is added when the file
is built.

Dart declared counts the test declarations in the file. Dart run counts the
tests that execute, because a loop runs one declaration many times. C# ported
counts the C# tests that carry a Dart test over. Added in C# counts tests with
no Dart twin. A file is done when C# ported equals Dart run, less the tests a
decision drops.

The Dart is deleted. The tag `dart-final` holds the last Dart commit.

The rules are the engine's `dotnet/PORTING.md`, in the `orblit` repository. The
engine packages come from a folder. The engine's `tool/pack_local.sh` packs
them, into `~/.orblit/feed` unless `ORBLIT_FEED` names another.
The cloth needs `Orblit.Physics` and `Orblit.Physics.Soft` from the
`orblit-physics` repository. Its `tool/build_native.sh` builds the solver, and
`dotnet pack -c Release -o ~/.orblit/feed` on each of the two projects packs it.
The simulation needs `Orblit.Net` from the `orblit-net` repository, packed the
same way from its `dotnet/Orblit.Net` project.
`Directory.Packages.props` pins the versions.

## The gallery

| Dart test file | C# class | Dart declared | Dart run | C# ported | Added in C# | Status |
| --- | --- | --- | --- | --- | --- | --- |
| `every_example_test.dart` | `EveryExampleTests` | 5 | 210 | 200 | 0 | done. The Dart runs 5 tests over 42 examples. Here the same 5 run over the 40 left: the 37 in `Orblit.Examples`, the gallery's cloth, its native interface and `Interface in the world`. Decision 94 drops the 2 script examples, which is 10 tests. The settings test also checks each setting's label is on screen, where the Dart checked only that nothing threw. The texture check also fails an example outside the named set that draws its own, so a new one cannot go unchecked |
| `code_view_test.dart` | `CodeViewTests` | 11 | 11 | 9 | 1 | done. Decision 94 drops the 2 TSX tests, so the panel colours C# only. The Dart colours Dart, and here the same checks colour C#, so the panel's badge reads `C#`. The paragraph test runs at 40 columns, where flowing each line alone breaks them differently. The added test cuts a word longer than a line |
| `sidebar_test.dart` | `SidebarTests` | 2 | 2 | 2 | 0 | done |
| none, the Dart has no test | `LaunchTests` | 0 | 0 | 0 | 8: the example named by the start of its name, the orbit and an orbit that is not a number, the bare window, how far a drag and a scroll move the view, the view an example opens with and the numbers a launch gives it, a held clock, the settings a launch names, and a launch setting the opening example and saying what it could not set | added |
| none, the Dart has no test | `SettingByLabelTests` | 0 | 0 | 0 | 17: a slider set and settled as when its handle is let go, a choice and a toggle and an entry found by label in any case, a toggle taking the words a person would use, a missing label naming the ones there are, an item that cannot be set, a row with nothing to act on, a caption and a button, and a later item seeing what the earlier one changed | added |
| none, the Dart has no test | `ClothTests` | 0 | 0 | 0 | 7: the pole holds the first column, the gusts flap the flag and a calm one hangs flat, six steps caught up and a jump taking none, the same flag however the frames fall, the sheet handed over once under its versioned name, the note without a renderer, and the gallery's cloth in the real resource table | added |
| `world_ui_test.dart` | `WorldUiSceneTests`, `WorldUiOverlayTests` | 1 | 5 | 5 | 15: the fixture is from committed source, every frame before the first picture and the controls are the fixture's, a control moved before the picture leaves the scene as the fixture shows it (2), no names leaves the room and the crowd, distinct text draws another picture and lets go of the old one, half a name rounds up, the slider sets the sun, letting go releases both pictures and takes the screen out of the scene, a press on the screen does not orbit, a drag along the slider and back, a press the system takes back is not a click and leaves the screen free, the face counts its presses and its slider sets the daylight, taking the overlay off the stage takes the screen down | done. The Dart declared 1 test in a helper and ran it 5 times. The screen is real Avalonia controls drawn by `PanelHost`, where the Dart drew Flutter widgets. The 300 frames and 63 keys the engine fixture holds for this example are frozen into `fixtures/world_ui.json` here, because the engine's tests cannot see this example. They match until the first picture is synced, since the Dart scene lacked the pictures too. A pointer reaches the screen only after the host has drawn it a frame or two, so the tests render twice first. The cost test keeps the Dart's loose bounds: a frame under 25 ms, a press to a picture under a second and two hundred names under two |
| `interface_script_test.dart` | none | 6 | 6 | 0 | 0 | not ported. Decision 94 takes Orblit Script out of the C# engine |
| `native_interface_test.dart` | `NativeInterfaceTests` | 2 | 2 | 1 | 3: halves round up in the percent and the bar, the settings are what the display shows with each number rounded as the slider shows it, and the stage changes the controls it has rather than making new ones | done. Decision 95 drops the vocabulary test, because the display is Avalonia controls. The layout test also checks the bar's width |
| none, the Dart has no test | `StageTests` | 0 | 0 | 0 | 5: what a model holds and the first fault the renderer reports, which examples draw over their scene, the reason shown with no renderer, a file read in the background handed over on the UI thread, and the device given to the example with what it could not load | added |
| none, the Dart has no test | `OverlayTests` | 0 | 0 | 0 | 17: the framing names the live shot, the guides shade and box, six keys named by their W3C codes, a held key let go on focus loss, a drag looks and a click digs and a right click puts one back, five frame costs each in its colour, the cost read four times a second, and the note card | added |
| `runner_test.dart` in `orblit`, its keyboard test | `OverlayTests` | 1 | 1 | 1 | 3: a swipe steers, jumps or slides and a tap means go, focus going elsewhere pauses unless the autopilot plays, and the score and the cards | done. The Dart checked the ready card and the phase, never where a key moved the runner. Here a twin game played by direct calls matches the runner's body at every frame, for every key. Keys are physical where the Dart's were logical, so A and D sit in the same place on any layout. The other 8 tests of `runner_test` are ported in `orblit` |
| none, the Dart has no test | `SettingsTests` | 0 | 0 | 0 | 1: an entry for a file has a button that says whether one is chosen | added |

## The simulation

| Dart test file | C# class | Dart declared | Dart run | C# ported | Added in C# | Status |
| --- | --- | --- | --- | --- | --- | --- |
| `simulation_test.dart` | `SimulationTests` | 4 | 4 | 4 | 0 | done. The thousand-body test also checks the last body moved. The Dart checked only the count, so a wrong row stride passed |

## The viewport

| Dart test file | C# class | Dart declared | Dart run | C# ported | Added in C# | Status |
| --- | --- | --- | --- | --- | --- | --- |
| `widget_test.dart` | `ViewportWindowTests` | 2 | 2 | 2 | 1: the slider resizes the stage's column | done. The Dart view drew the native code's own scene. Here the host hands the renderer a turning cube each frame |
