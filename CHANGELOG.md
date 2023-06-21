# Spartacus Changelog

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
