# Spartacus Changelog

## v2.1.2

* `[New]` Added `--action exports` to `--mode proxy` that lists a file's exports, functionality similar to `dumpbin.exe /exports`.
* `[Fix]` Fixed `--only` parameter which was ignored when generating a proxy solution without using Ghidra.

## v2.1.1

* `[Update]` Added support for `NTAPI` prototypes.

## v2.1.0

* `[New]` Added `--action prototypes` to `--mode proxy` that supports the parsing of `*.h` files in order to generate pre-existing function prototypes.
* `[New]` Included `./Assets/prototypes.csv` with a pre-generated list of function prototypes.
* `[Update]` Updated `--mode com --acl` functionality to check the parent folder's permissions as well when checking for misconfigured COM registry entries.

## v2.0.0

* `[New]` Implement support for identifying COM Hijacking.
* `[New]` Added option to support external resources for solution generation.
* `[Update]` Improve Visual Studio solution generation.
* `[Update]` Simplified and reduced command line arguments.
* `[Removed]` Removed the individual `*.cpp` proxy skeleton file generation, replaced with full solutions.

## v1.2.0

* `[New]` Implement replication of `VERSIONINFO` and timestomping to match source file during solution compilation. (Issue #1)

## v1.1.1

* `[Fix]` Allow digits/symbols in --only-proxy command

## v1.1.0

* `[New]` Implement new functionality to create proxies for functions other than DllMain, as described here: https://www.redteam.cafe/red-team/dll-sideloading/dll-sideloading-not-by-dllmain

## v1.0.0

* `[New]` Public Release.
