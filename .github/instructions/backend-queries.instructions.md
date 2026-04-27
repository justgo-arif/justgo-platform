---
applyTo: "src/Modules/**/*.Application/**/Features/**/*Handler.cs"
description: "Use when editing MediatR handlers to enforce safe SQL, correct commandType usage, and LazyService repository injection."
---

# Backend Query Rules For Application Handlers

Apply these rules in handler classes under Application Features.

## Repository Injection

- Inject repositories as `LazyService<IReadRepository<T>>` and `LazyService<IWriteRepository<T>>`.
- Do not inject `IReadRepository<T>` or `IWriteRepository<T>` directly.
- Keep repository fields readonly and constructor-injected.

## SQL Safety

- Use parameterized SQL only. Pass parameters via anonymous objects.
- Do not concatenate user input into SQL strings.
- Do not use EF LINQ in handlers. Use repository methods with SQL or stored procedures.

## commandType Rules

- Stored procedures can use default behavior (`commandType: "sp"`).
- Raw SQL must explicitly pass `commandType: "text"`.
- If the query text starts with `SELECT`, `INSERT`, `UPDATE`, or `DELETE`, set `commandType: "text"`.

## Handler Expectations

- Pass the `CancellationToken` through all async repository calls.
- Keep handlers focused on orchestration and persistence calls.
- Put input/business validation in FluentValidation validators, not ad-hoc checks inside SQL strings.
