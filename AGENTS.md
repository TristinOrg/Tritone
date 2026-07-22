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

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

When the user types `/graphify`, use the installed graphify skill or instructions before doing anything else.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- Dirty graphify-out/ files are expected after hooks or incremental updates; dirty graph files are not a reason to skip graphify. Only skip graphify if the task is about stale or incorrect graph output, or the user explicitly says not to use it.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
