# Tritone Coding Style

- Use four spaces for indentation. Do not use tabs.
- Do not add copyright or author headers to source files; repository ownership already provides that information.
- Write XML documentation in English for every type, field, property, method, and parameter.
- Prefix enum type names with an uppercase `E`.
- Prefix private instance fields with `m` and private static fields with `s`.
- Vertically align consecutive field declarations, assignments, and object initializers when it improves readability.
- When an `if` statement contains exactly one simple statement, place it on the next line without braces.
- Always use braces for multi-statement branches.
- Keep the pure C# Kernel independent of UnityEngine.
- Prefer explicit dependencies and deterministic lifecycle behavior over global state.
- Avoid allocations, LINQ, reflection, and delegate captures on per-frame hot paths.
