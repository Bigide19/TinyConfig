# TinyConfig

[![NuGet](https://img.shields.io/nuget/v/TinyConfig.svg)](https://www.nuget.org/packages/TinyConfig)
[![NuGet Downloads](https://img.shields.io/nuget/dt/TinyConfig.svg)](https://www.nuget.org/packages/TinyConfig)

The simplest way to manage configuration in .NET. One unified interface for INI,
JSON, XML, and Windows Registry, with no package dependencies on .NET Framework.

## Install

```
PM> Install-Package TinyConfig
```

Or via .NET CLI:

```
dotnet add package TinyConfig
```

## Quick Start

```csharp
using TinyConfig;

// Pick any backend — the API is the same
var config = Config.FromFile("settings.ini");   // INI
var config = Config.FromJson("settings.json");  // JSON
var config = Config.FromXml("settings.xml");    // XML
var config = Config.FromRegistry(@"SOFTWARE\MyApp"); // Registry
```

## API

All providers implement `ITinyConfig`:

```csharp
// Read a value (returns default if not found)
string host = config.Get("Server", "Host", "localhost");

// Read with automatic type conversion
int port = config.Get<int>("Server", "Port", 8080);
bool debug = config.Get<bool>("App", "Debug", false);

// Auto-save: writes the default value if the key doesn't exist
string theme = config.Get("UI", "Theme", "dark", autoSave: true);

// Write a value
config.Set("Server", "Host", "192.168.0.1");

// Check if a key exists
bool exists = config.Exists("Server", "Host");

// Try to read - false when the key is missing, empty, or the text does not fit T
if (config.TryGet("Shutdown", "Everyday", out DateTime shutdown))
{
    // shutdown holds a converted value
}
```

## Providers

### INI

```csharp
var config = Config.FromFile("settings.ini");
config.Set("Server", "Host", "localhost");
```

```ini
[Server]
Host=localhost
```

Writing leaves the rest of the file alone. Comments, blank lines, key order and the
spacing around `=` stay as they were, and only the line being changed is rewritten.
A new key is added inside its section, above any trailing comment.

INI files carry no encoding declaration. A file written by the Windows
`WritePrivateProfileString` API is in the system ANSI code page, so pass the
encoding explicitly instead of relying on the UTF-8 default:

```csharp
// .NET Core and later need the code page provider registered once
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var config = Config.FromFile("cfg.ini", Encoding.GetEncoding(949));
```

### JSON

```csharp
var config = Config.FromJson("settings.json");
config.Set("Database", "Port", 5432);
```

```json
{
  "Database": {
    "Port": "5432"
  }
}
```

Values are always written as JSON strings, and a hand-edited file may use numbers,
booleans or `null` instead - those read back fine. JSON parsing is built in rather
than taken from `System.Text.Json`, so the package stays dependency-free and works
on .NET Framework. Writing escapes only what the JSON grammar requires, which
leaves non-ASCII text readable as UTF-8.

### XML

```csharp
var config = Config.FromXml("settings.xml");
config.Set("Logging", "Level", "Info");
```

```xml
<?xml version="1.0"?>
<Config>
  <Logging>
    <Level>Info</Level>
  </Logging>
</Config>
```

### Windows Registry

```csharp
// Default: HKEY_CURRENT_USER
var config = Config.FromRegistry(@"SOFTWARE\MyApp");

// Use a different root hive
var config = Config.FromRegistry(@"SOFTWARE\MyApp", RegistryRoot.LocalMachine);
```

Available `RegistryRoot` values: `CurrentUser` (default), `LocalMachine`, `ClassesRoot`, `Users`, `CurrentConfig`

## Type Conversion

`Get<T>` converts string values using `TypeDescriptor`. Supports `int`, `bool`, `double`, `float`, `DateTime`, `enum`, `TimeSpan`, and any type with a registered `TypeConverter`.

```csharp
var timeout = config.Get<TimeSpan>("App", "Timeout", TimeSpan.FromSeconds(30));
var mode = config.Get<MyEnum>("App", "Mode", MyEnum.Default);
```

If conversion fails, `Get<T>` returns the default value. Use `TryGet<T>` when a format
mismatch has to be told apart from a real default — a value stored as `210000` and read
as `DateTime` returns false rather than silently falling back.

## Section Mapping

The first parameter (`section`) maps to different concepts depending on the provider:

| Provider | Section maps to |
|----------|----------------|
| INI | `[Section]` header |
| JSON | Top-level object |
| XML | Child element of `<Config>` |
| Registry | Sub-key under root path |

## Target Frameworks

`netstandard2.0` and `net461`.

All four providers work on both builds. The `net461` build has no NuGet
dependencies at all: Registry and XML are part of the framework there, and JSON
uses the built-in parser rather than `System.Text.Json`, which supports net462
and later only.

| Build | Providers | Dependencies |
| --- | --- | --- |
| `netstandard2.0` | INI, JSON, XML, Registry | `Microsoft.Win32.Registry` |
| `net461` | INI, JSON, XML, Registry | none |

## License

[MIT](LICENSE)
