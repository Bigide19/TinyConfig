# TinyConfig

The simplest way to manage configuration in .NET. One unified interface for INI, JSON, XML, and Windows Registry.

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
// Pick any backend — the API is the same
var config = TinyConfig.FromFile("settings.ini");   // INI
var config = TinyConfig.FromJson("settings.json");  // JSON
var config = TinyConfig.FromXml("settings.xml");    // XML
var config = TinyConfig.FromRegistry(@"SOFTWARE\MyApp"); // Registry
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
```

## Providers

### INI

```csharp
var config = TinyConfig.FromFile("settings.ini");
config.Set("Server", "Host", "localhost");
```

```ini
[Server]
Host=localhost
```

### JSON

```csharp
var config = TinyConfig.FromJson("settings.json");
config.Set("Database", "Port", 5432);
```

```json
{
  "Database": {
    "Port": "5432"
  }
}
```

### XML

```csharp
var config = TinyConfig.FromXml("settings.xml");
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
var config = TinyConfig.FromRegistry(@"SOFTWARE\MyApp");

// Use a different root hive
var config = TinyConfig.FromRegistry(@"SOFTWARE\MyApp", RegistryRoot.LocalMachine);
```

Available `RegistryRoot` values: `CurrentUser` (default), `LocalMachine`, `ClassesRoot`, `Users`, `CurrentConfig`

## Type Conversion

`Get<T>` converts string values using `TypeDescriptor`. Supports `int`, `bool`, `double`, `float`, `DateTime`, `enum`, `TimeSpan`, and any type with a registered `TypeConverter`.

```csharp
var timeout = config.Get<TimeSpan>("App", "Timeout", TimeSpan.FromSeconds(30));
var mode = config.Get<MyEnum>("App", "Mode", MyEnum.Default);
```

If conversion fails, the default value is returned safely.

## Section Mapping

The first parameter (`section`) maps to different concepts depending on the provider:

| Provider | Section maps to |
|----------|----------------|
| INI | `[Section]` header |
| JSON | Top-level object |
| XML | Child element of `<Config>` |
| Registry | Sub-key under root path |

## Target Framework

.NET Standard 2.0 — compatible with .NET Framework 4.6.1+, .NET Core 2.0+, and .NET 5 through 10+.

## License

[MIT](LICENSE)
