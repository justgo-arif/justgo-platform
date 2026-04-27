---
name: definition-of-done
description: "Produce a structured Definition of Done summary for the current task: files changed, tests, OWASP A01-A10 coverage, and verification commands."
argument-hint: "Optional: brief description of the completed task or feature"
---

Produce a Definition of Done summary for the work just completed.

## Instructions

Review the changes made in this session (or the task described in the argument) and emit a structured completion report.

If an argument is provided, use it as context for the task description. Otherwise, infer the task from the changes visible in the conversation or working tree.

## Output Format

Emit the following sections in order. Do not skip any section.

---

### ✅ Task Summary

One or two sentences describing what was implemented or changed.

---

### 📁 Files Changed

List every file that was created, modified, or deleted. Group by action.

| Action | File |
|--------|------|
| Created | `path/to/file` |
| Modified | `path/to/file` |
| Deleted | `path/to/file` |

---

### 🧪 Tests

List all tests added or updated. If no tests were added, state the reason (e.g., "no behavior change", "covered by existing tests").

| Test File | Test Name / Description | Type (Unit / Integration / Architecture) |
|-----------|------------------------|------------------------------------------|
| `path/to/test` | `MethodName_Scenario_ExpectedResult` | Unit |

---

### 🔒 OWASP A01–A10 Coverage

For each OWASP Top 10 category, state whether the changes are **Relevant** or **Not applicable**, and briefly describe the control or why it does not apply.

| # | Category | Status | Notes |
|---|----------|--------|-------|
| A01 | Broken Access Control | Relevant / Not applicable | e.g. `[CustomAuthorize]` enforces ABAC; ownership checked via tenant context |
| A02 | Cryptographic Failures | Relevant / Not applicable | e.g. No secrets exposed; credentials remain encrypted via `IUtilityService.DecryptData()` |
| A03 | Injection | Relevant / Not applicable | e.g. Parameterized SQL via `IReadRepository<T>`; no string concatenation |
| A04 | Insecure Design | Relevant / Not applicable | e.g. Input validated via FluentValidation before handler executes |
| A05 | Security Misconfiguration | Relevant / Not applicable | e.g. No new configuration keys; existing defaults unchanged |
| A06 | Vulnerable and Outdated Components | Relevant / Not applicable | e.g. No new NuGet packages added |
| A07 | Identification and Authentication Failures | Relevant / Not applicable | e.g. Endpoint protected; no anonymous access introduced |
| A08 | Software and Data Integrity Failures | Relevant / Not applicable | e.g. No deserialization of untrusted data |
| A09 | Security Logging and Monitoring Failures | Relevant / Not applicable | e.g. Errors flow through `ExceptionSink`; no sensitive data logged |
| A10 | Server-Side Request Forgery (SSRF) | Relevant / Not applicable | e.g. No outbound HTTP calls introduced |

---

### 🛠 Verification Commands

Provide the exact commands used (or that should be run) to verify the changes locally.

```bash
dotnet build
dotnet test
dotnet test tests/JustGo.ArchitectureTests
# Add any module-specific or endpoint-specific commands:
# dotnet test src/Modules/<Module>/tests/<Module>.UnitTests
# curl -X GET http://localhost:5152/api/v1/...
```

---

## Behavior

- Do not modify any files in this prompt.
- Fill in every table row — do not leave rows blank. Use "Not applicable" with a reason when a category does not apply.
- Be concise but specific. Avoid generic filler text.
- If a section cannot be completed due to missing context, state what information is needed.
