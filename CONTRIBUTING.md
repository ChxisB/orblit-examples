# Contributing

Worked examples of what Orblit can do. A new example that shows something the
others do not is as welcome as a fix to an existing one.

Talk about anything large first, in an issue or on the
[Discord](https://discord.gg/5DH7HuDUtJ). Small fixes need no ceremony: just
open the pull request. The engine itself lives in
[ChxisB/orblit](https://github.com/ChxisB/orblit), and its
[CONTRIBUTING](https://github.com/ChxisB/orblit/blob/main/CONTRIBUTING.md) has
the longer version of this.

## Checking your work

```sh
./tool/link_local.sh   # point the examples at a local orblit checkout
./tool/check.sh
```

Re-run the platform setup after new `.mat` files land, or the examples will
pick up stale materials.

## Licence

This repository is under MPL-2.0. Opening a pull request means you are
offering your change under that same licence, and that you wrote it or
otherwise have the right to contribute it.

There is no CLA to sign and no copyright to assign. You keep the copyright on
what you write. It is simply licensed the same way as the rest of the
repository.
