# Endpoint to SRS Generation Guideline

This guideline explains what developers should do when adding, changing, or removing API endpoints so that the JustGo Platform SRS remains current and business-readable.

## Purpose

The SRS file is generated from the current backend API surface:

```text
docs/justgo-platform-software-requirements-specification.md
```

The generator reads module controllers under `src/Modules` and converts observed endpoint capabilities into business-oriented Functional Requirements.

## When to Regenerate the SRS

Regenerate the SRS whenever a change affects the platform's API capability surface, including:

- A new controller is added.
- A new endpoint is added.
- An existing endpoint is renamed, moved, or removed.
- An endpoint changes its business purpose.
- A module gains a new business workflow.
- Authorization or user-facing behavior changes in a way that affects requirements.

## Regeneration Command

From the repository root, run:

```powershell
python scripts\generate_srs.py
```

This updates:

```text
docs/justgo-platform-software-requirements-specification.md
```

## Developer Checklist

Before committing endpoint changes:

- [ ] Use business-meaningful controller and action names.
- [ ] Keep routes consistent with existing module conventions.
- [ ] Ensure the endpoint belongs to the correct business module.
- [ ] Avoid direct cross-module dependencies; use MediatR where cross-module communication is required.
- [ ] Regenerate the SRS using `python scripts\generate_srs.py`.
- [ ] Review the generated SRS section for the affected module.
- [ ] Confirm the Functional Requirement text is business-readable and not overly technical.
- [ ] Improve `scripts/generate_srs.py` mappings if generated wording is awkward.
- [ ] Run architecture tests before committing.

## Required Validation

Run architecture tests before committing module or endpoint changes:

```powershell
dotnet test tests/JustGo.ArchitectureTests
```

For broader changes, also run relevant module tests:

```powershell
dotnet test src/Modules/<Module>/tests/<Suite>
```

## Writing Endpoint Names for Better SRS Output

The SRS generator infers requirement wording from controller names, action names, routes, and related CQRS request names. Prefer names that express the business capability.

Good examples:

```csharp
GetMemberFamilySummary
UpdateEmergencyContact
CreateAssetLease
ApproveTransferRequest
ValidateBookingEligibility
DownloadMembershipCertificate
```

Avoid vague names:

```csharp
Process
DoAction
SubmitData
GetInfo
UpdateStatus
```

If a generic name is unavoidable in code, update the generator's wording rules in:

```text
scripts/generate_srs.py
```

## SRS Review Expectations

After regeneration, review the affected section in:

```text
docs/justgo-platform-software-requirements-specification.md
```

Check that:

- The new requirement appears under the correct module.
- The requirement describes business value, not technical implementation.
- The wording uses `SHALL`, `SHOULD`, or `MAY` appropriately.
- No endpoint route, HTTP verb, controller file path, or source line number appears in the Functional Requirements table.
- The generated wording is understandable to product owners, support, operations, and implementation teams.

## PR Checklist Addition

Add this checklist to pull requests that change endpoints:

```text
- [ ] If endpoints changed, regenerated SRS using python scripts\generate_srs.py
- [ ] Reviewed generated SRS wording for affected modules
- [ ] Confirmed no technical API details leaked into FR tables
- [ ] Ran dotnet test tests/JustGo.ArchitectureTests
```

## Important Note

The generated SRS is a baseline draft from code observation. It should be treated as implementation-derived documentation until reviewed by business stakeholders. If the generated text does not capture the true business intent, update the generator rules or refine the source naming so future generations stay accurate.
