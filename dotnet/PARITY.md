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
`Directory.Packages.props` pins the versions.

## The gallery

| Dart test file | C# class | Dart declared | Dart run | C# ported | Added in C# | Status |
| --- | --- | --- | --- | --- | --- | --- |
| `every_example_test.dart` | `EveryExampleTests` | 5 | 210 | 144 | 0 | in progress. The Dart runs 5 tests over 42 examples. Here 4 run over the 36 in `Orblit.Examples`. The settings test comes with the window, and the 6 examples left come with their parts of row 2 and with the Runner. The texture check also fails an example outside the named set that draws its own, so a new one cannot go unchecked |
| `code_view_test.dart` | `CodeViewTests` | 11 | 11 | 7 | 1 | in progress. The Dart colours Dart, and here the same checks colour C#. The paragraph test runs at 40 columns, where flowing each line alone breaks them differently. The added test cuts a word longer than a line. The TSX check waits for the two script examples, and the 3 panel tests come with the window |
