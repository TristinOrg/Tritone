# Configuration table authoring

Tritone compiles self-describing CSV and TSV sources into strongly typed C#
rows and runtime JSON as one transaction. A failed table prevents every output
from being committed.

## Configure source discovery

Create `Assets/Tritone/Tables.json`:

```json
{
  "Namespace": "Game.Tables",
  "OutputPath": "Assets/Generated/Tritone/Tables",
  "DataOutputPath": "Assets/Resources/Tables",
  "SourceDirectories": [
    "Assets/GameData/Tables"
  ]
}
```

Run **Tritone > Generate > Tables**. Source directories and files are processed
in deterministic path order, independent of their order in the schema.

## Author a source

The first row contains field names, the second contains field types, and the
remaining rows contain data. The first field is inferred as the key:

```csv
Id,Name,Enabled
int,string,bool
1001,Tristin,true
1002,Aigis,false
```

Built-in scalar types are `bool`, `int`, `long`, `float`, `double`, and
`string`. CSV quoted fields support delimiters and escaped quotes. TSV uses the
same schema with tab delimiters.

For `Assets/GameData/Tables/Items/Weapons.csv`, the compiler produces:

- `Assets/Generated/Tritone/Tables/WeaponsTable.Generated.cs`
- `Assets/Resources/Tables/Items/Weapons.json`
- runtime asset path `Tables/Items/Weapons`

Generated row classes implement `ITableRow<TKey>`, and generated table metadata
exposes the runtime path:

```csharp
var weapons = LoadTable<int, WeaponsRow>(WeaponsTable.Path);
var sword   = weapons.Get(1001);
```

## Diagnostics and transactions

Diagnostics contain a stable code, source path, one-based row, and one-based
column whenever a source cell is responsible. Important validation includes:

- malformed CSV or TSV quoting;
- invalid or duplicated headers;
- missing required fields;
- unregistered inferred field types;
- invalid values and duplicate keys;
- duplicate table names;
- conflicting inferred or explicit field schemas;
- generated output path collisions.

Duplicate names with matching fields report `TRT-TABLE-2103`. Duplicate names
with different ordered fields report `TRT-TABLE-2106` and identify both source
files. Type errors inferred from a source point to the second row and exact
column.

Outputs are staged in memory and committed together. Validation or generation
failure leaves the previously generated files unchanged. Running the compiler
again without source changes performs no writes.

## Extend the compiler

Use `TableCompilerBuilder` when the default CSV/TSV-to-C#/JSON pipeline is not
enough:

```csharp
var compiler = new TableCompilerBuilder()
    .AddSourceReader(new ProjectTableSourceReader())
    .AddDefaultFieldTypes()
    .AddFieldType(new ProjectIdFieldType())
    .AddValidator(new ProjectReferenceValidator())
    .UseCodeGenerator(new CSharpTableCodeGenerator())
    .UseDataWriter(new JsonTableDataWriter())
    .Build();
```

Source readers, scalar field types, validators, code generators, and data
writers are independent extension points. Keep project validation deterministic
and report failures through `TableDiagnosticCollection`.

## CI fixture

The repository test project contains a complete fixture under
`.ci/TestProject/Assets`: the source CSV, `Tables.json`, generated C# row, and
Resources JSON. It compiles with the package during Unity EditMode and PlayMode
jobs and provides a reviewable example of expected output.

