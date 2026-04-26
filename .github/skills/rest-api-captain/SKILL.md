---
name: rest-api-captain
description: Describe what this skill does and when to use it. Include keywords that help agents identify relevant tasks.
---

<!-- Tip: Use /create-skill in chat to generate content with agent assistance -->

---

name: rest-api-captain
description: Generate REST API design plans and implementation from requirements, user stories, or markdown specs. Supports design-first workflow with approval gating. Keywords: REST API, backend, endpoints, controllers, DTO, service, architecture
-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

# REST API Captain Skill

This skill processes requirements and executes a structured backend workflow using REST principles.

It supports two modes:

* `/rest-api design`
* `/rest-api implement`

---

## 🔹 Command Usage

### 1. Design Mode

/rest-api design

Use this to analyze requirements and produce a full REST API design.

### 2. Implementation Mode

/rest-api implement

Use this ONLY after approval to generate code from the design.

---

## 🔹 MODE: DESIGN

When user runs:

/rest-api design

### Responsibilities:

Analyze the requirement and produce:

## 1. Domain Model

* Entities with fields and types
* Relationships

## 2. API Endpoints

For each endpoint include:

* HTTP Method
* Route
* Description

## 3. Request / Response Contracts

* JSON structure
* Example payloads

## 4. Validation Rules

* Required fields
* Constraints

## 5. Business Logic

* Key rules
* Default behaviors

## 6. Assumptions

* External dependencies
* Missing details

---

### ❗ Strict Rules

* DO NOT generate code
* DO NOT assume frameworks
* Keep output structured and deterministic
* Follow REST best practices

---

### ✅ Output must end with:

STATUS: WAITING_FOR_APPROVAL

---

## 🔹 MODE: IMPLEMENT

When user runs:

/rest-api implement

### Preconditions:

* Input MUST contain: APPROVED: YES
* A valid design must exist in context or be provided

---

### Responsibilities:

Generate production-ready backend code including:

## 1. Controllers

* REST endpoints
* Proper routing

## 2. DTOs (Request/Response)

* Strongly typed models

## 3. Services

* Business logic layer

## 4. Validation

* Input validation rules

---

### ❗ Strict Rules

* MUST follow approved design EXACTLY
* DO NOT change API contracts
* DO NOT introduce new endpoints
* Keep structure clean and maintainable

---

## 🔹 Behavior Constraints

* Never mix DESIGN and IMPLEMENT
* Always respect approval gate
* Prefer clarity over verbosity
* Output must be clean and structured

---

## 🔹 Example Flow

### Step 1

/rest-api design

<requirement>

---

### Step 2 (AI Output)

STATUS: WAITING_FOR_APPROVAL

---

### Step 3

APPROVED: YES
/rest-api implement

---

### Step 4

→ Code is generated
