# Parity

One row per Dart test file that has been ported. A row is added when the file
is built.

Dart declared counts the test declarations in the file. Dart run counts the
tests that execute, because a loop runs one declaration many times. C# ported
counts the C# tests that carry a Dart test over. Added in C# counts tests with
no Dart twin. A file is done when C# ported equals Dart run.

The rules are the engine's `dotnet/PORTING.md`, in the `orblit` repository. The
engine packages come from a folder. The engine's `tool/pack_local.sh` packs
them, into `~/.orblit/feed` unless `ORBLIT_FEED` names another.
The cloth needs `Orblit.Physics` and `Orblit.Physics.Soft` from the
`orblit-physics` repository. Its `tool/build_native.sh` builds the solver, and
`dotnet pack -c Release -o ~/.orblit/feed` on each of the two projects packs it.
`Directory.Packages.props` pins the versions.

## The gallery

| Dart test file | C# class | Dart declared | Dart run | C# ported | Added in C# | Status |
| --- | --- | --- | --- | --- | --- | --- |
| `every_example_test.dart` | `EveryExampleTests` | 5 | 210 | 185 | 0 | in progress. The Dart runs 5 tests over 42 examples. Here the same 5 run over the 36 in `Orblit.Examples` and the gallery's cloth, and the 5 examples left come with their parts of row 2 and with the Runner. The settings test also checks each setting's label is on screen, where the Dart checked only that nothing threw. The texture check also fails an example outside the named set that draws its own, so a new one cannot go unchecked |
| `code_view_test.dart` | `CodeViewTests` | 11 | 11 | 10 | 1 | in progress. The Dart colours Dart, and here the same checks colour C#, so the panel's badge reads `C#`. The paragraph test runs at 40 columns, where flowing each line alone breaks them differently. The added test cuts a word longer than a line. The TSX check waits for the two script examples |
| `sidebar_test.dart` | `SidebarTests` | 2 | 2 | 2 | 0 | done. The Dart names `Spawning from script` as a folded example. The script examples are not in C# yet, so `Lights` stands in |
| none, the Dart has no test | `LaunchTests` | 0 | 0 | 0 | 4: the example named by the start of its name, the orbit and an orbit that is not a number, the bare window, and how far a drag and a scroll move the view | added |
| none, the Dart has no test | `ClothTests` | 0 | 0 | 0 | 7: the pole holds the first column, the gusts flap the flag and a calm one hangs flat, six steps caught up and a jump taking none, the same flag however the frames fall, the sheet handed over once under its versioned name, the note without a renderer, and the gallery's cloth in the real resource table | added |
