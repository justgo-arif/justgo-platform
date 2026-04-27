---
name: backend-queries-audit
description: "Audit Application handlers for safe SQL patterns, commandType correctness, and LazyService repository injection."
argument-hint: "Optional: module name, folder path, or specific handler file(s) to audit"
---

Audit the targeted Application handler files for backend query convention compliance.

## Scope

- If an argument is provided, audit only that module/path/file.
- Otherwise, audit all handler files matching: `src/Modules/**/*.Application/**/Features/**/*Handler.cs`.

## Rules To Check

1. Repository injection
- Must use `LazyService<IReadRepository<T>>` / `LazyService<IWriteRepository<T>>` in handlers.
- Flag direct injection of `IReadRepository<T>` or `IWriteRepository<T>`.

2. SQL safety
- SQL must be parameterized via repository method parameters.
- Flag SQL string concatenation/interpolation with request/user input.
- Flag dynamic SQL built from unchecked input.

3. commandType usage
- Raw SQL calls must explicitly set `commandType: "text"`.
- Stored procedures may omit commandType or use `"sp"`.
- If SQL starts with `SELECT`, `INSERT`, `UPDATE`, or `DELETE`, require `"text"`.

4. Handler execution hygiene
- Pass `CancellationToken` through async repository calls.
- Flag EF LINQ usage in handlers.

## Output Format

1. Findings table with columns:
- Severity (`high`/`medium`/`low`)
- Rule
- File
- Line
- Evidence
- Fix

2. Summary:
- Total handlers scanned
- Violations by rule
- Files with no issues

3. If no issues are found, state: `No backend query convention violations found.`

## Behavior

- Do not modify files in this prompt.
- If fixes are requested afterward, propose minimal patches grouped by file.
