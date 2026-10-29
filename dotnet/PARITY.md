# Parity

One row per Dart test file that has been ported. A row is added when the file
is built.

Dart declared counts the test declarations in the file. Dart run counts the
tests that execute, because a loop runs one declaration many times. C# ported
counts the C# tests that carry a Dart test over. Added in C# counts tests with
no Dart twin. A file is done when C# ported equals Dart run, less the tests a
decision drops.

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
| `every_example_test.dart` | `EveryExampleTests` | 5 | 210 | 190 | 0 | in progress. The Dart runs 5 tests over 42 examples. Here the same 5 run over the 36 in `Orblit.Examples`, the gallery's cloth and its interface in C#. Decision 94 drops the 2 script examples. The 2 examples left come with their parts of row 2 and with the Runner. The settings test also checks each setting's label is on screen, where the Dart checked only that nothing threw. The texture check also fails an example outside the named set that draws its own, so a new one cannot go unchecked |
| `code_view_test.dart` | `CodeViewTests` | 11 | 11 | 9 | 1 | done. Decision 94 drops the 2 TSX tests, so the panel colours C# only. The Dart colours Dart, and here the same checks colour C#, so the panel's badge reads `C#`. The paragraph test runs at 40 columns, where flowing each line alone breaks them differently. The added test cuts a word longer than a line |
| `sidebar_test.dart` | `SidebarTests` | 2 | 2 | 2 | 0 | done |
| none, the Dart has no test | `LaunchTests` | 0 | 0 | 0 | 4: the example named by the start of its name, the orbit and an orbit that is not a number, the bare window, and how far a drag and a scroll move the view | added |
| none, the Dart has no test | `ClothTests` | 0 | 0 | 0 | 7: the pole holds the first column, the gusts flap the flag and a calm one hangs flat, six steps caught up and a jump taking none, the same flag however the frames fall, the sheet handed over once under its versioned name, the note without a renderer, and the gallery's cloth in the real resource table | added |
| `interface_script_test.dart` | none | 6 | 6 | 0 | 0 | not ported. Decision 94 takes Orblit Script out of the C# engine |
| `native_interface_test.dart` | `NativeInterfaceTests` | 2 | 2 | 1 | 3: halves round up in the percent and the bar, the settings are what the display shows with each number rounded as the slider shows it, and the stage changes the controls it has rather than making new ones | done. Decision 95 drops the vocabulary test, because the display is Avalonia controls. The layout test also checks the bar's width |
| none, the Dart has no test | `StageTests` | 0 | 0 | 0 | 5: what a model holds and the first fault the renderer reports, which examples draw over their scene, the reason shown with no renderer, a file read in the background handed over on the UI thread, and the device given to the example with what it could not load | added |
| none, the Dart has no test | `OverlayTests` | 0 | 0 | 0 | 17: the framing names the live shot, the guides shade and box, six keys named by their W3C codes, a held key let go on focus loss, a drag looks and a click digs and a right click puts one back, five frame costs each in its colour, the cost read four times a second, and the note card | added |
| none, the Dart has no test | `SettingsTests` | 0 | 0 | 0 | 1: an entry for a file has a button that says whether one is chosen | added |

## The simulation

| Dart test file | C# class | Dart declared | Dart run | C# ported | Added in C# | Status |
| --- | --- | --- | --- | --- | --- | --- |
| `simulation_test.dart` | `SimulationTests` | 4 | 4 | 4 | 0 | done. The thousand-body test also checks the last body moved. The Dart checked only the count, so a wrong row stride passed |

## The viewport

| Dart test file | C# class | Dart declared | Dart run | C# ported | Added in C# | Status |
| --- | --- | --- | --- | --- | --- | --- |
| `widget_test.dart` | `ViewportWindowTests` | 2 | 2 | 2 | 1: the slider resizes the stage's column | done. The Dart view drew the native code's own scene. Here the host hands the renderer a turning cube each frame |
