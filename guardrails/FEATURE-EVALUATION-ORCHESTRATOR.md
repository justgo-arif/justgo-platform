# Feature Evaluation Orchestrator

## Purpose

Use this orchestrator as the final feature-done gate before merge. It consolidates all existing JustGo clean architecture checklists into one deterministic review process.

Primary outcome:
- PASS
- CONDITIONAL
- FAIL

Strict rule:
- Any Blockade results in FAIL.

## Source Rulebooks

The orchestrator must use all of these sources:

- guardrails/JustGo.Api.Review.Checklist.txt
- guardrails/JustGo.Application.Review.Checklist.txt
- guardrails/JustGo.Infrastructure.Review.Checklist.txt
- guardrails/JustGo.DomainLayer.Review.Checklist.Pragmatic.txt
- guardrails/JustGo.ApiHostAndShared.Review.Checklist.txt
- AGENTS.md (Definition of Done + Critical Anti-Patterns)

## When To Run

Run after implementation and tests are complete, and before opening or merging a PR.

Recommended checkpoints:
- Feature coding complete
- Tests added or updated
- Endpoint and handler wiring complete
- Caching, auth, and error behavior verified

## Inputs Required

Reviewer must provide:
- Feature name or ID
- Module and scope summary
- Changed file list
- Evidence from code, tests, and docs
- Known constraints or intentional deviations

## Rule Taxonomy

### Blockade (Merge-blocking)

If any Blockade is present, verdict is FAIL.

Examples:
- Cross-module Application dependency
- QueryHandler performing write operations
- SQL interpolation from runtime or user input
- Domain referencing Infrastructure or API
- Missing explicit endpoint authorization declaration
- Controller returns domain entity or anonymous/object response type
- Write flow misses required cache invalidation
- ABAC check missing where required
- Secrets or tokens exposed in logs/config
- No minimum required tests for changed behavior

### Reservation (Non-blocking concern)

Reservations reduce confidence and score. If reservations exceed threshold, verdict becomes CONDITIONAL.

Examples:
- Naming inconsistency against convention
- Repeated mapping that should be extracted
- Incomplete edge-case validation
- Overly broad utility/helper usage
- Legacy pattern reused without migration intent

### Nit (Low-priority improvement)

Nits do not affect score or verdict. They are optional improvement notes.

Examples:
- Minor comment clarity
- Small readability improvements
- Non-critical refactor suggestions

## Layer Weights

Total score: 100

- Domain: 15
- Infrastructure: 15
- Application: 25
- API: 25
- API Host + Shared: 10
- Definition of Done and Anti-Patterns: 10

## Scoring Rules

- Start at full score for each layer.
- Deduct points for Reservations based on impact.
- Blockades do not deduct points; they directly force FAIL.
- Nits do not affect score.

Suggested deduction guidance:
- Reservation (low impact): -1
- Reservation (medium impact): -2
- Reservation (high impact): -3

## Verdict Logic

1) If Blockades count is 1 or more: FAIL
2) Else if score is 95 or more and Reservations are below threshold: PASS
3) Else if score is 85 to 94: CONDITIONAL
4) Else: FAIL

Reservation threshold for PASS:
- No more than 3 medium-equivalent reservations

## Evaluation Flow

1) Collect feature context and changed files
2) Evaluate Domain checklist
3) Evaluate Infrastructure checklist
4) Evaluate Application checklist
5) Evaluate API checklist
6) Evaluate API Host and Shared checklist
7) Evaluate Definition of Done and Anti-Patterns from AGENTS
8) Classify findings into Blockade, Reservation, Nit
9) Compute layer scores
10) Apply verdict logic
11) Emit report with required actions

## Output Report Template

Use this exact structure:

Feature Evaluation Report

Metadata
- Feature: <feature-id-or-name>
- Module: <module-name>
- Reviewer: <name-or-agent>
- Date: <yyyy-mm-dd>
- Files Reviewed: <count>

Verdict
- Final Verdict: PASS | CONDITIONAL | FAIL
- Total Score: <x>/100
- Blockades: <count>
- Reservations: <count>
- Nits: <count>

Layer Scores
- Domain: <x>/15
- Infrastructure: <x>/15
- Application: <x>/25
- API: <x>/25
- API Host + Shared: <x>/10
- Definition of Done and Anti-Patterns: <x>/10

Blockades (If Any)
- <finding>
  - Why it is a blockade
  - Evidence files
  - Required fix to clear gate

Reservations
- <finding>
  - Impact
  - Evidence files
  - Recommended action

Nits
- <finding>
  - Optional improvement

Required Fixes Before Merge
- <ordered actionable list>

Confidence and Assumptions
- <unknowns, missing evidence, or scope limits>

## Evidence Rules

- Every Blockade and Reservation must cite at least one file path.
- Findings without evidence must be moved to Assumptions.
- If evidence is insufficient for a required area, mark as Reservation at minimum.

## Escalation Rules

- If architecture boundary is violated, escalate as Blockade.
- If security, auth, or data safety rule is violated, escalate as Blockade.
- If repeated reservations are seen across features, create follow-up governance action.

## Calibration Notes

Use first 3 to 5 feature reviews to calibrate reservation deductions and threshold sensitivity. Keep strict Blockade behavior unchanged.

## Version History

- Version: 1.0
- Date: 2026-04-29
- Change: Initial orchestrator release
- Decision: Any Blockade forces FAIL
