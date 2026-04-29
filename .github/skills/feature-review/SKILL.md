---
name: feature-review
description: 'Final feature-done gate. Runs all JustGo clean architecture checklists and emits PASS / CONDITIONAL / FAIL with scored findings. Use when: feature implementation complete, before opening a PR, validating a feature branch. Commands: /feature-review <feature-name-or-description>'
argument-hint: 'Feature name or short description (e.g., "CreateBooking endpoint", "Club search with filters")'
user-invocable: true
---

# Feature Evaluation — Orchestrator Skill

## What This Skill Does

Reads every JustGo architecture rulebook and the AGENTS.md Definition of Done, evaluates the changed code against all rules, classifies findings as Blockade / Reservation / Nit, computes a weighted score, and emits a structured report with a final PASS / CONDITIONAL / FAIL verdict.

**Strict rule: any Blockade = FAIL, no exceptions.**

## How To Invoke

```
/feature-review Add club search with pagination
/feature-review CreateBookingCommand + handler + controller
/feature-review  (no arg — will ask for context)
```

## Step-by-Step Execution

Follow these steps in order. Do not skip.

### Step 1 — Collect Context

Ask the user (or infer from args) for:
- Feature name / ID
- Module(s) affected
- Brief scope summary (what was added/changed)
- Changed file list — if not provided, run `git diff --name-only HEAD` or `git status` to discover

### Step 2 — Load Rulebooks

Read ALL of these files completely before evaluating:

```
guardrails/JustGo.DomainLayer.Review.Checklist.Pragmatic.txt
guardrails/JustGo.Application.Review.Checklist.txt
guardrails/JustGo.Infrastructure.Review.Checklist.txt
guardrails/JustGo.Api.Review.Checklist.txt
guardrails/JustGo.ApiHostAndShared.Review.Checklist.txt
AGENTS.md  (Definition of Done section + Critical Anti-Patterns)
```

Read changed source files to gather evidence. Do not evaluate from memory alone.

### Step 3 — Evaluate Each Layer

Evaluate changed files against the corresponding checklist. For each checklist item that is violated or questionable, classify as:

| Class | Description | Verdict impact |
|-------|-------------|----------------|
| **Blockade** | Merge-blocking violation | Forces FAIL |
| **Reservation** | Non-blocking concern | Reduces score |
| **Nit** | Low-priority improvement | No score impact |

**Blockade triggers (non-exhaustive):**
- Cross-module Application dependency (direct reference, not MediatR)
- QueryHandler performing writes (insert/update/delete/audit write)
- SQL built with string interpolation from runtime/user input
- Domain references Infrastructure, API, or sibling module
- Endpoint missing `[CustomAuthorize]` or `[AllowAnonymous]`
- Controller returns domain entity, raw primitive, or `ApiResponse<object, object>`
- Write flow missing required cache invalidation
- ABAC check absent where data sensitivity requires it
- Secrets or tokens in logs, config, or hardcoded
- No tests covering changed behavior

**Reservation triggers (non-exhaustive):**
- Naming deviates from `[Action]Query/Command → [Action]QueryHandler/CommandHandler`
- Repeated mapping logic that should be in a profile
- Validator missing for rule-based or pagination/filter input
- Legacy pattern reused without migration note
- Incomplete edge-case validation
- CancellationToken accepted but not forwarded

### Step 4 — Score Each Layer

Start each layer at full weight. Deduct for Reservations only.

| Layer | Max |
|-------|-----|
| Domain | 15 |
| Infrastructure | 15 |
| Application | 25 |
| API | 25 |
| API Host + Shared | 10 |
| Definition of Done + Anti-Patterns | 10 |
| **Total** | **100** |

Deduction guidance:
- Reservation low impact → -1
- Reservation medium impact → -2
- Reservation high impact → -3

Blockades do not deduct — they trigger FAIL directly.

### Step 5 — Apply Verdict Logic

1. Blockades ≥ 1 → **FAIL**
2. Score ≥ 95 AND reservations ≤ 3 medium-equivalent → **PASS**
3. Score 85–94 → **CONDITIONAL**
4. Score < 85 → **FAIL**

### Step 6 — Emit Report

Use this exact structure:

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
FEATURE EVALUATION REPORT
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

METADATA
  Feature   : <feature-name>
  Module    : <module>
  Reviewer  : Claude Code / feature-review skill
  Date      : <yyyy-mm-dd>
  Files     : <count> reviewed

VERDICT
  ┌─────────────────────────────────────┐
  │  <PASS | CONDITIONAL | FAIL>        │
  └─────────────────────────────────────┘
  Score       : <x>/100
  Blockades   : <n>
  Reservations: <n>
  Nits        : <n>

LAYER SCORES
  Domain                    <x>/15
  Infrastructure            <x>/15
  Application               <x>/25
  API                       <x>/25
  API Host + Shared         <x>/10
  Definition of Done        <x>/10

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
BLOCKADES  (merge-blocking)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[B1] <title>
  Why       : <rule violated>
  Evidence  : <file:line>
  Fix       : <required action to clear gate>

(none — if no blockades)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
RESERVATIONS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[R1] <title>  [low | medium | high]
  Impact    : <description>
  Evidence  : <file>
  Action    : <recommendation>

(none — if no reservations)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
NITS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[N1] <title>
  Note      : <optional improvement>

(none — if no nits)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
REQUIRED FIXES BEFORE MERGE
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
1. <actionable fix — ordered by priority>

(none — if PASS with no blockades)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
CONFIDENCE & ASSUMPTIONS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
- <unknowns, missing evidence, scope limits>
```

## Evidence Rules

- Every Blockade and Reservation must cite at least one file path.
- Findings without evidence go to Assumptions section, not Blockades.
- If a required area has no changed files, note it in Confidence section — do not assume compliance.

## Escalation Rules

- Architecture boundary violation → always Blockade, never Reservation.
- Security, auth, or data safety violation → always Blockade.
- Repeated pattern across multiple files → single Blockade/Reservation with all evidence files listed.

## Layers Not Touched

If a layer has zero changed files, score it full weight and note "No changes in this layer" in Confidence section. Do not penalize untouched layers.

## See Also

- Full orchestrator spec: [guardrails/FEATURE-EVALUATION-ORCHESTRATOR.md](../../../guardrails/FEATURE-EVALUATION-ORCHESTRATOR.md)
- Architecture rules: [tests/JustGo.ArchitectureTests](../../../tests/JustGo.ArchitectureTests)
- Run arch tests: `dotnet test tests/JustGo.ArchitectureTests`
