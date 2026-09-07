# CODELY.md

## Project Overview

**Excel导表工具 (Excel Data Export Tool)** — a C# console application that converts Excel spreadsheets (`.xlsx`/`.xls`) into structured data files (JSON and/or binary `.bytes`) while simultaneously generating matching C# data classes. Designed to feed Unity game projects with typed, ready-to-use configuration data.

- **Language / Runtime:** C# / .NET 6.0 (SDK-style project)
- **Key Dependency:** [NPOI](https://www.nuget.org/packages/NPOI) 2.5.6 (Excel reading)
- **Target Consumer:** Unity projects (generated classes reference `UnityEngine.Vector2`, `Vector3`, `Color`)
- **Namespace (Tool):** `PerillaTable`
- **Namespace (Generated Classes):** `Database`

### Architecture

| File | Responsibility |
|---|---|
| `Program.cs` | Entry point — orchestrates enum generation, file discovery, and per-file export |
| `Config.cs` | Singleton (`Config.I`) that parses `config.txt` into runtime settings |
| `ExcelTool.cs` | Core engine — reads Excel via NPOI, generates C# class source, serializes data to JSON and/or Bytes |
| `FileTool.cs` | File I/O utilities — recursive directory scan, read/write strings and bytes |
| `DataMgr.cs` | Runtime support — `DataItem` base class and `DataLoader` for indexed binary data loading |
| `Logger.cs` | Thin `Console.WriteLine` wrapper with color-coded error output |

## Building and Running

### Build

```bash
dotnet build DataTable/DataTable.csproj
```

Or open `DataTable/DataTable.sln` in Visual Studio and build.

### Run

```bash
dotnet run --project DataTable/DataTable.csproj
```

Or execute the compiled `DataTable.exe` directly from `bin/Debug/`.

### Configuration

Create `config.txt` in the working directory (semicolon-delimited, `#` for comments):

```ini
#excel存放路径;
excelPath:./Excel/;
#数据保存路径;
dataPath:./DataTable/;
#c#类保存路径;
classPath:./CSharp/;
#输出类型 (多个用&连接，如 Json&Bytes);
exportType:Json;
isExportServer:False
```

### Testing

No automated test suite exists. Verification is manual — run the tool and inspect generated output in `dataPath` and `classPath`.

## Excel Format Convention

Each Excel sheet follows a fixed row layout:

| Row | Content |
|---|---|
| Row 0 | Comment / description (ignored) |
| Row 1 | Variable names — first cell must be `#name` |
| Row 2 | Data types — first cell must be `#type` |
| Row 3+ | Data rows (rows whose first column starts with `#` are skipped) |

### Class Naming

- **Single-sheet file:** The Excel filename (without extension) becomes the class name.
- **Multi-sheet file:** The class name is taken from the first cell in the format `#{ClassName}`. Sheets whose names contain "sheet" (case-insensitive) are skipped when more than one sheet exists.

### Supported Types

| Category | Types |
|---|---|
| Primitives | `int`, `float`, `bool`, `string` |
| Arrays | `int[]`, `float[]`, `bool[]`, `string[]` |
| Dictionary (KV) | `dic<keyType,valueType>` — column header uses `variableName:key` format |
| Enum | Auto-generated from `Enum.xlsx`; enum values start at 0; type name is `Enum` + variable name |
| Unity types | `vector3`, `vector2`, `color` |

### Enum Table (`Enum.xlsx`)

Place in the `excelPath` directory. Each column defines one enum — row 0 is the enum name, subsequent rows are members. The tool generates `EnumType.cs` containing all enums before processing other tables.

## Export Formats

### JSON

- Output path: `{dataPath}/Json/D{ClassName}.json`
- Custom serialization (README mentions a modified LitJson library)
- Unity projects should import `JsonExtension.cs` and `UnityTypeBridge.cs` for custom type registration

### Bytes

- Output path: `{dataPath}/Bytes/D{ClassName}.bytes`
- Binary format with an index table appended at the end for O(1) lookup by ID
- Generated C# classes include a `ParseByBytes` method; `DataLoader` handles indexed loading at runtime
- Does **not** support `dic` types (use linked tables instead)

## Development Conventions

- **Singleton pattern:** `Config` uses a lazy singleton via `Config.I`.
- **String template approach:** `ExcelTool` uses placeholder string templates (e.g., `"className"`, `"dataType"`, `"dataName"`) and `Replace()` to generate C# source code.
- **Naming:** Generated property names use PascalCase (first letter uppercased via `UpperFirstLetter`). Generated class files are prefixed with `D` (e.g., `DItem.cs`).
- **No test framework** is configured; changes should be validated by running the tool against sample Excel files.
- **`packages.config`** references legacy .NET Framework 4.8 packages, but the `.csproj` is SDK-style targeting `net6.0` — NuGet packages are managed via `<PackageReference>` in the `.csproj` (only NPOI is actively used).
