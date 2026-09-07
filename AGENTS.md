# AGENTS.md

Guidance for AI coding agents working in this repository.

## What this project is

TinyConfig is a .NET configuration library. One interface, `ITinyConfig`, over four
backends: INI files, JSON files, XML files, and the Windows Registry. It is published
to NuGet as `TinyConfig`.

The design goal is that adding the package pulls in nothing else. On .NET Framework
and .NET the dependency count is zero; only the `netstandard2.0` fallback references
`Microsoft.Win32.Registry`.

## Layout

```
TinyConfig/                 Library
  Config.cs                 Entry point: FromFile, FromJson, FromXml, FromRegistry
  TinyConfig.cs             Obsolete entry point, kept for compatibility
  ITinyConfig.cs            Public interface
  RegistryRoot.cs           Hive enum
  Internal/
    ValueConverter.cs       String to T via TypeDescriptor
    MiniJson.cs             Hand-written JSON reader/writer
  Providers/                One file per backend
TinyConfig.Tests/           NUnit, net10.0 only
TinyConfig.Sample.Wpf/      WPF sample, net10.0-windows
```

## Constraints that are easy to break

**`LangVersion` is 7.3.** The library multi-targets down to `net461` and
`netstandard2.0`. Switch expressions, target-typed `new`, `using` declarations,
records, and nullable reference types are all unavailable. The compiler will tell
you, but reach for C# 7.3 idioms from the start.

**JSON must not take a dependency.** `System.Text.Json` 8.x supports net462 and
later only, which is why `Internal/MiniJson.cs` exists. Do not reintroduce a JSON
package reference to make something easier.

**`MiniJson` value mapping is a compatibility contract.** Files written by version
1.2.0 and earlier used `System.Text.Json`, so reads must keep matching what
`JsonElement.ToString()` returned: `true` reads as `"True"`, `null` as an empty
string, numbers as written without normalizing. `MiniJsonTests` pins this.

**Registry is Windows-only.** `RegistryProvider` throws
`PlatformNotSupportedException` off Windows and carries
`[SupportedOSPlatform("windows")]` on net5.0+ targets. Keep both when touching it.

**Tests only run on net10.0.** The net4x paths are compile-verified, not
run-verified. A change that behaves differently per target framework will not be
caught by `dotnet test`.

## Comments

Method and type summaries as XML doc (`///`) only. No inline `//` comments
explaining what the code does. Comments are in English. Write for someone consuming
the library, not for someone reading the implementation.

## Build and test

```bash
dotnet build                 # 10 target frameworks, must stay at 0 warnings
dotnet test                  # 90 tests
dotnet pack -c Release       # produces .nupkg and .snupkg
```

`GenerateDocumentationFile` is on, so a public member without a summary breaks the
zero-warning state.

## Release flow

Version lives in `TinyConfig/TinyConfig.csproj`. Pushing a branch that touches
`TinyConfig/**` publishes a prerelease (`<version>-test.N`, numbered per version).
Merging such a PR to `main` publishes the release to NuGet. Both are automatic, so
treat a version bump as a deployment.
