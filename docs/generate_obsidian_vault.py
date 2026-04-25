"""
Generate complete Obsidian vault from JustGoAPI_SRS.pdf.
Produces docs/SRS/ folder with one .md file per section/leaf node.
Usage: python docs/generate_obsidian_vault.py
"""

from pathlib import Path

OUT_DIR = Path(__file__).parent / "SRS"
OUT_DIR.mkdir(exist_ok=True)

# ---------------------------------------------------------------------------
# Helper
# ---------------------------------------------------------------------------

def write(filename: str, content: str) -> None:
    path = OUT_DIR / filename
    path.write_text(content.strip() + "\n", encoding="utf-8")
    print(f"  wrote {path.name}")


def req_table(rows: list[tuple[str, str]]) -> str:
    lines = ["| ID | Requirement |", "|-----|-------------|"]
    for rid, text in rows:
        lines.append(f"| `{rid}` | {text} |")
    return "\n".join(lines)


# ---------------------------------------------------------------------------
# INDEX (root of vault)
# ---------------------------------------------------------------------------

write("index.md", """---
title: JustGo API SRS — Vault Index
tags: [srs, justgo, api, documentation]
---

# JustGo API SRS — Index

> **Source:** `docs/JustGoAPI_SRS.pdf`
> **Version:** 0.1 · First Draft · April 25, 2026
> **Status:** Reverse-engineered from codebase
> **Reference:** `SRS_JustGoAPI_First_Draft_v0.1`

---

## Table of Contents

- [[1-introduction|1. Introduction]]
  - [[1.1-purpose|1.1 Purpose]]
  - [[1.2-document-conventions|1.2 Document Conventions]]
  - [[1.3-intended-audience|1.3 Intended Audience]]
  - [[1.4-project-scope|1.4 Project Scope]]
  - [[1.5-references|1.5 References]]
- [[2-overall-description|2. Overall Description]]
  - [[2.1-product-perspective|2.1 Product Perspective]]
  - [[2.2-product-functions|2.2 Product Functions]]
  - [[2.3-user-classes|2.3 User Classes, Characteristics, and Needs]]
  - [[2.4-operating-environment|2.4 Operating Environment]]
  - [[2.5-design-constraints|2.5 Design and Implementation Constraints]]
  - [[2.6-user-documentation|2.6 User Documentation]]
  - [[2.7-assumptions-dependencies|2.7 Assumptions and Dependencies]]
- [[3-system-features|3. System Features and Functional Requirements]]
  - [[3.1-authentication|3.1 Platform Authentication, Accounts, and User Management]]
  - [[3.2-mfa|3.2 Multi-Factor Authentication]]
  - [[3.3-tenant-management|3.3 Tenant and System Settings Management]]
  - [[3.4-authorization|3.4 Authorization Policies and UI Permissions]]
  - [[3.5-files-attachments|3.5 Files, Attachments, Notes, and Lookup Data]]
  - [[3.6-cache-invalidation|3.6 Cache Invalidation]]
  - [[3.7-asset-management|3.7 Asset Management]]
  - [[3.8-booking|3.8 Booking and Class Management]]
  - [[3.9-credential-management|3.9 Credential Management]]
  - [[3.10-field-management|3.10 Field Management and Entity Extensions]]
  - [[3.11-referenced-modules|3.11 Referenced Modules Requiring Source Validation]]
- [[4-external-interfaces|4. External Interface Requirements]]
  - [[4.1-user-interfaces|4.1 User Interfaces]]
  - [[4.2-hardware-interfaces|4.2 Hardware Interfaces]]
  - [[4.3-software-interfaces|4.3 Software Interfaces]]
  - [[4.4-communications-interfaces|4.4 Communications Interfaces]]
- [[5-non-functional-requirements|5. Non-Functional Requirements]]
  - [[5.1-performance|5.1 Performance Requirements]]
  - [[5.2-security|5.2 Security Requirements]]
  - [[5.3-reliability|5.3 Reliability and Availability]]
  - [[5.4-usability|5.4 Usability and Accessibility]]
  - [[5.5-maintainability|5.5 Maintainability and Portability]]
  - [[5.6-legal-compliance|5.6 Legal and Compliance Requirements]]
  - [[5.7-operational|5.7 Operational Requirements]]
- [[6-other-requirements|6. Other Requirements]]
  - [[6.1-data-migration|6.1 Data Migration]]
  - [[6.2-internationalization|6.2 Internationalization Requirements]]
  - [[6.3-training|6.3 Training Requirements]]
  - [[6.4-appendix-a|6.4 Appendix A: Analysis Models]]
  - [[6.5-appendix-b|6.5 Appendix B: Issues List]]
- [[glossary|Glossary]]

---

## Quick Reference

| # | Section |
|---|---------|
| 1 | [[1-introduction\\|Introduction]] |
| 2 | [[2-overall-description\\|Overall Description]] |
| 3 | [[3-system-features\\|System Features & Functional Requirements]] |
| 4 | [[4-external-interfaces\\|External Interface Requirements]] |
| 5 | [[5-non-functional-requirements\\|Non-Functional Requirements]] |
| 6 | [[6-other-requirements\\|Other Requirements]] |

---

## Related
- [[glossary|Glossary]]
""")

# ---------------------------------------------------------------------------
# GLOSSARY
# ---------------------------------------------------------------------------

write("glossary.md", """---
title: Glossary
tags: [srs, glossary]
---

# Glossary

> [[index|← Back to Index]]

| Term | Definition |
|------|------------|
| ABAC | Attribute-Based Access Control |
| API | Application Programming Interface |
| DTO | Data Transfer Object |
| JWT | JSON Web Token |
| MFA | Multi-Factor Authentication |
| RBAC | Role-Based Access Control |
| SaaS | Software as a Service |
| SLA | Service Level Agreement |
| SRS | Software Requirements Specification |
| TBD | To Be Determined |
""")

# ---------------------------------------------------------------------------
# 1. INTRODUCTION (parent)
# ---------------------------------------------------------------------------

write("1-introduction.md", """---
title: "1. Introduction"
tags: [srs, introduction]
---

# 1. Introduction

> [[index|← Back to Index]]

## Children
- [[1.1-purpose|1.1 Purpose]]
- [[1.2-document-conventions|1.2 Document Conventions]]
- [[1.3-intended-audience|1.3 Intended Audience]]
- [[1.4-project-scope|1.4 Project Scope]]
- [[1.5-references|1.5 References]]
""")

write("1.1-purpose.md", """---
title: "1.1 Purpose"
tags: [srs, introduction]
---

# 1.1 Purpose

> [[index|← Index]] · [[1-introduction|← 1. Introduction]]

This Software Requirements Specification describes the **JustGo multi-tenant SaaS REST API platform** represented by the `JustGoAPI.API` host and its modular API codebase.

The document captures:
- System purpose and product perspective
- Functional requirements
- External interfaces
- Quality attributes
- Known gaps discovered during initial static analysis

The SRS is intended to become the baseline documentation for:
- Architecture analysis
- QA planning
- Business validation
- API documentation
- Engineer onboarding
- Future modernization or product roadmap work

---

> **Related:** [[1.2-document-conventions|Document Conventions]] · [[1.3-intended-audience|Intended Audience]]
""")

write("1.2-document-conventions.md", """---
title: "1.2 Document Conventions"
tags: [srs, introduction, conventions]
---

# 1.2 Document Conventions

> [[index|← Index]] · [[1-introduction|← 1. Introduction]]

## Requirement Priority Terms

| Term | Description |
|------|-------------|
| `SHALL` | Mandatory requirement for the current API platform |
| `SHOULD` | Important requirement that should be implemented or improved where not already present |
| `MAY` | Optional or future requirement |
| `TBD` | Information unavailable from code and requiring stakeholder confirmation |
| `Note` | Clarification or implementation observation |

## Requirement Numbering

| Prefix | Type |
|--------|------|
| `FR-XX` | Functional requirement |
| `NFR-XX` | Non-functional requirement |
| `IR-XX` | Interface requirement |
| `DR-XX` | Data requirement |
| `SR-XX` | Security requirement |

---

> **Related:** [[1.1-purpose|Purpose]] · [[glossary|Glossary]]
""")

write("1.3-intended-audience.md", """---
title: "1.3 Intended Audience"
tags: [srs, introduction, audience]
---

# 1.3 Intended Audience

> [[index|← Index]] · [[1-introduction|← 1. Introduction]]

| Stakeholder | Role |
|-------------|------|
| Product owners | Validate business scope and module behavior |
| Engineering team | Development, refactoring, test coverage, and onboarding |
| QA team | Derive test cases, regression suites, and acceptance criteria |
| DevOps/SRE | Plan deployment, monitoring, scaling, backup, and operational support |
| Security/compliance reviewers | Validate authentication, authorization, audit, privacy, and data protection controls |
| Integration partners | Understand API boundaries and integration expectations |

---

> **Related:** [[1.4-project-scope|Project Scope]] · [[2.3-user-classes|User Classes]]
""")

write("1.4-project-scope.md", """---
title: "1.4 Project Scope"
tags: [srs, introduction, scope]
---

# 1.4 Project Scope

> [[index|← Index]] · [[1-introduction|← 1. Introduction]]

The JustGoAPI platform is a **modular REST API backend** for a multi-tenant SaaS system serving sports clubs, organizations, administrators, members, and related operational workflows.

## In Scope

- Authentication, account, MFA, token, tenant, and user APIs
- Common file attachment, notes, lookup, cache invalidation, and UI permission APIs
- Asset management APIs — registers, categories, ownership, leasing, licensing, credentials, workflows, audit, reports
- Booking APIs — catalog, classes, sessions, attendees, terms, pricing discounts, transfer checks, profile bookings
- Credential APIs — member credential summary, credential metadata, credential categories
- Field management APIs — dynamic entity extensions, schemas, data, attachments, user weblet preferences
- Platform-level: API versioning, Swagger/OpenAPI, JWT authentication, CORS, response compression, SignalR progress hub, Serilog logging, exception handling
- Referenced but source-missing modules: **Finance, MemberProfile, Membership, Organisation, MobileApps, Result** _(require source-level validation)_

## Out of Scope (First Draft)

- Detailed UI requirements outside REST API behavior
- Confirmed business process diagrams for missing-source modules
- Production infrastructure sizing, RTO/RPO, contractual SLA values
- Payment provider business rules beyond observable references
- Legal and compliance commitments beyond recommended requirements

---

> **Related:** [[1.5-references|References]] · [[3-system-features|System Features]] · [[3.11-referenced-modules|Referenced Modules]]
""")

write("1.5-references.md", """---
title: "1.5 References"
tags: [srs, introduction, references]
---

# 1.5 References

> [[index|← Index]] · [[1-introduction|← 1. Introduction]]

| Reference | Description |
|-----------|-------------|
| `Annex-A-Detailed-Software-Requirements-Specification-SRS.pdf` | Sample SRS format supplied by the user |
| `JustGoAPI.API.zip` | Uploaded API codebase archive |
| `JustGoAPI.API/Program.cs` | Main ASP.NET Core API host configuration |
| Controller source files | Primary source for discovered REST API capabilities |
| Project files | Source for target framework, dependencies, and module references |
""")

# ---------------------------------------------------------------------------
# 2. OVERALL DESCRIPTION (parent)
# ---------------------------------------------------------------------------

write("2-overall-description.md", """---
title: "2. Overall Description"
tags: [srs, overview]
---

# 2. Overall Description

> [[index|← Back to Index]]

## Children
- [[2.1-product-perspective|2.1 Product Perspective]]
- [[2.2-product-functions|2.2 Product Functions]]
- [[2.3-user-classes|2.3 User Classes, Characteristics, and Needs]]
- [[2.4-operating-environment|2.4 Operating Environment]]
- [[2.5-design-constraints|2.5 Design and Implementation Constraints]]
- [[2.6-user-documentation|2.6 User Documentation]]
- [[2.7-assumptions-dependencies|2.7 Assumptions and Dependencies]]
""")

write("2.1-product-perspective.md", """---
title: "2.1 Product Perspective"
tags: [srs, overview, architecture]
---

# 2.1 Product Perspective

> [[index|← Index]] · [[2-overall-description|← 2. Overall Description]]

JustGoAPI is an **ASP.NET Core REST API host** targeting **.NET 9**. The API host composes multiple business modules using dependency injection.

## Platform Capabilities

- Versioned HTTP APIs
- Swagger/OpenAPI documentation
- JWT authentication
- Custom authorization middleware
- Centralized exception handling
- CORS
- Static files
- Response compression
- Serilog logging
- SignalR progress-reporting hub at `/progressReportHub`

## Architecture

The solution follows a **modular architecture** with API, Application, Domain, Infrastructure, and Test projects per module.

**Available source modules:** Auth, Asset Management, Booking, Credential, Field Management

**Referenced (no source):** Finance, MemberProfile, Membership, Organisation, MobileApps, Result — compiled assemblies exist in build output.

## Context Diagram (Text)

```
Client Applications / Admins
        │
        ▼
  JustGo REST API
  ├── Authenticates users
  ├── Resolves tenant context
  ├── Executes module services
  ├── Accesses SQL Server & file storage
  ├── Integrates with: email/SMS/payment/blob
  └── Emits logs/audit events
```

---

> **Related:** [[2.4-operating-environment|Operating Environment]] · [[6.4-appendix-a|Context & Module Diagrams]]
""")

write("2.2-product-functions.md", """---
title: "2.2 Product Functions"
tags: [srs, overview, functions]
---

# 2.2 Product Functions

> [[index|← Index]] · [[2-overall-description|← 2. Overall Description]]

| # | Function Area |
|---|---------------|
| 1 | User authentication, token refresh, password reset/change, hash/encryption helpers, user creation and profile lookup |
| 2 | MFA setup, verification, backup codes, OTP delivery, mandatory MFA enforcement, admin MFA management |
| 3 | Tenant CRUD and domain-based tenant lookup |
| 4 | ABAC policy evaluation and UI permission lookup |
| 5 | File upload/download and entity attachment management |
| 6 | Entity notes management |
| 7 | Reference data lookup — countries, counties, genders, regions, club types |
| 8 | Asset type, category, register, credential, lease, license, ownership transfer, workflow, audit, report, status, and club APIs |
| 9 | Booking catalog, classes, occurrences, attendees, pricing discounts, transfer checks, class terms, profile bookings, QR booking links |
| 10 | Credential summary and metadata APIs |
| 11 | Dynamic field/entity extension schema, UI schema, data persistence, attachments, weblet preferences |
| 12 | API versioning, Swagger, JWT, CORS, logging, exception handling, response compression, real-time progress notifications |

---

> **Related:** [[3-system-features|Functional Requirements]] · [[6.4-appendix-a|API Surface Summary]]
""")

write("2.3-user-classes.md", """---
title: "2.3 User Classes, Characteristics, and Needs"
tags: [srs, overview, users]
---

# 2.3 User Classes, Characteristics, and Needs

> [[index|← Index]] · [[2-overall-description|← 2. Overall Description]]

| User Class | Characteristics and Needs |
|------------|--------------------------|
| Platform administrators | Manage tenants, settings, users, policies, MFA, cache, files, and support activities |
| Club/organization administrators | Manage clubs, organization context, members, assets, booking offerings, credentials, and reporting |
| Members/end users | Authenticate, manage profile-linked bookings/assets/credentials, access member-specific class/course data, interact with account services |
| Finance/payment operators | Finance/payment functionality; exact workflows TBD from missing source module |
| Integration clients/mobile apps | Consume REST APIs using JWT and tenant-aware context; mobile-specific APIs referenced but source unavailable |
| Support engineers | Use logs, audit trails, attachment data, and API diagnostics to troubleshoot production issues |

---

> **Related:** [[1.3-intended-audience|Intended Audience]] · [[3.1-authentication|Authentication Requirements]]
""")

write("2.4-operating-environment.md", """---
title: "2.4 Operating Environment"
tags: [srs, overview, infrastructure]
---

# 2.4 Operating Environment

> [[index|← Index]] · [[2-overall-description|← 2. Overall Description]]

| Component | Details |
|-----------|---------|
| **Server** | ASP.NET Core / .NET 9 runtime in a secure environment |
| **Database** | SQL Server — via `Microsoft.Data.SqlClient`, EF Core SQL Server, Dapper, stored-query patterns |
| **File storage** | Azure Blob (referenced); local/static files also enabled |
| **API clients** | Web applications, mobile apps, admin portals, partner integrations over HTTPS |
| **Real-time channel** | SignalR hub at `/progressReportHub` |
| **Documentation** | Swagger/OpenAPI via application configuration |

---

> **Related:** [[2.5-design-constraints|Design Constraints]] · [[4.3-software-interfaces|Software Interfaces]]
""")

write("2.5-design-constraints.md", """---
title: "2.5 Design and Implementation Constraints"
tags: [srs, overview, constraints]
---

# 2.5 Design and Implementation Constraints

> [[index|← Index]] · [[2-overall-description|← 2. Overall Description]]

- The API **SHALL** remain tenant-aware and protect tenant data boundaries.
- The API **SHALL** use versioned endpoints to preserve backward compatibility.
- The API **SHOULD** maintain modular boundaries between API, Application, Domain, and Infrastructure layers.
- The API **SHALL** use JWT authentication and custom authorization middleware before controller execution.
- The API **SHOULD** avoid placing security-sensitive utility endpoints in production unless explicitly required and protected.
- Source for several referenced modules is missing in the supplied archive; requirements for those modules are provisional.

---

> **Related:** [[5.2-security|Security Requirements]] · [[3.11-referenced-modules|Referenced Modules]] · [[6.5-appendix-b|Issues List]]
""")

write("2.6-user-documentation.md", """---
title: "2.6 User Documentation"
tags: [srs, overview, documentation]
---

# 2.6 User Documentation

> [[index|← Index]] · [[2-overall-description|← 2. Overall Description]]

Planned documentation artifacts:

- API onboarding guide
- Swagger/OpenAPI reference
- Tenant setup and operations guide
- Administrator guide — users, MFA, cache, files, notes, policies
- Module guides:
  - Asset Management
  - Booking
  - Credential
  - Field Management
  - Finance _(TBD)_
  - Member Profile _(TBD)_
  - Membership _(TBD)_
  - Organisation _(TBD)_
  - Mobile Apps _(TBD)_
  - Result _(TBD)_
- Operational runbooks — deployment, monitoring, backup, incident response, troubleshooting

---

> **Related:** [[6.3-training|Training Requirements]] · [[5.5-maintainability|Maintainability]]
""")

write("2.7-assumptions-dependencies.md", """---
title: "2.7 Assumptions and Dependencies"
tags: [srs, overview, assumptions]
---

# 2.7 Assumptions and Dependencies

> [[index|← Index]] · [[2-overall-description|← 2. Overall Description]]

## Assumptions

- Tenant isolation is based on domain and/or tenant identifiers resolved by authentication and tenant services.
- Authentication tokens carry claims required by module services and UI permissions.
- SQL Server is the primary operational datastore.
- Azure Blob or equivalent storage is used for file persistence in production.
- Missing source modules are part of the production product although not available in the extracted source tree.

## Dependencies

| Dependency | Purpose |
|------------|---------|
| SQL Server | Connectivity and stored procedures/queries |
| External payment providers | Finance/booking flows requiring payment |
| Email/SMS/OTP providers | MFA, OTP, and password flows |
| Azure Blob / configured file storage | File persistence |
| Swagger/OpenAPI consumers & API gateways | Documentation and routing (if used) |
| Tenant domain/DNS configuration | Tenant resolution |

---

> **Related:** [[4.3-software-interfaces|Software Interfaces]] · [[6.5-appendix-b|Issues List]]
""")

# ---------------------------------------------------------------------------
# 3. SYSTEM FEATURES (parent)
# ---------------------------------------------------------------------------

write("3-system-features.md", """---
title: "3. System Features and Functional Requirements"
tags: [srs, functional-requirements]
---

# 3. System Features and Functional Requirements

> [[index|← Back to Index]]

> Requirements derived from static code inspection. Final wording and priority **SHALL** be validated with business stakeholders.

## Subsections

- [[3.1-authentication|3.1 Platform Authentication, Accounts, and User Management]] _(FR-001–FR-010)_
- [[3.2-mfa|3.2 Multi-Factor Authentication]] _(FR-011–FR-020)_
- [[3.3-tenant-management|3.3 Tenant and System Settings Management]] _(FR-021–FR-028)_
- [[3.4-authorization|3.4 Authorization Policies and UI Permissions]] _(FR-029–FR-034)_
- [[3.5-files-attachments|3.5 Files, Attachments, Notes, and Lookup Data]] _(FR-035–FR-050)_
- [[3.6-cache-invalidation|3.6 Cache Invalidation]] _(FR-046–FR-050)_
- [[3.7-asset-management|3.7 Asset Management]] _(FR-051–FR-064)_
- [[3.8-booking|3.8 Booking and Class Management]] _(FR-065–FR-078)_
- [[3.9-credential-management|3.9 Credential Management]] _(FR-079–FR-082)_
- [[3.10-field-management|3.10 Field Management and Entity Extensions]] _(FR-083–FR-094)_
- [[3.11-referenced-modules|3.11 Referenced Modules Requiring Source Validation]] _(FR-095–FR-100)_
""")

write("3.1-authentication.md", """---
title: "3.1 Platform Authentication, Accounts, and User Management"
tags: [srs, functional-requirements, auth]
---

# 3.1 Platform Authentication, Accounts, and User Management

> [[index|← Index]] · [[3-system-features|← 3. System Features]]

| ID | Requirement |
|----|-------------|
| `FR-001` | The system **SHALL** authenticate users through a REST endpoint and issue access tokens for authorized API access. |
| `FR-002` | The system **SHALL** support authentication by existing token where configured. |
| `FR-003` | The system **SHALL** support refresh token operations. |
| `FR-004` | The system **SHALL** allow users to initiate forgot-password flows. |
| `FR-005` | The system **SHALL** allow authenticated or verified users to change passwords. |
| `FR-006` | The system **SHALL** expose user lookup by login identifier and by user GUID. |
| `FR-007` | The system **SHALL** allow authorized administrators or system processes to create users. |
| `FR-008` | The system **SHALL** allow authorized administrators or system processes to update users. |
| `FR-009` | The system **SHALL** enforce role, group, policy, or claim-based authorization before protected operations. |
| `FR-010` | The system **SHALL** return consistent API responses for successful and failed account operations. |

---

> **Related:** [[3.2-mfa|MFA]] · [[3.3-tenant-management|Tenant Management]] · [[5.2-security|Security Requirements]]
""")

write("3.2-mfa.md", """---
title: "3.2 Multi-Factor Authentication"
tags: [srs, functional-requirements, mfa, auth]
---

# 3.2 Multi-Factor Authentication

> [[index|← Index]] · [[3-system-features|← 3. System Features]]

| ID | Requirement |
|----|-------------|
| `FR-011` | The system **SHALL** provide MFA setup for supported authenticator methods. |
| `FR-012` | The system **SHALL** verify MFA codes submitted by users. |
| `FR-013` | The system **SHALL** support removal of authenticator configuration for authorized users. |
| `FR-014` | The system **SHALL** send OTP codes where the configured MFA channel requires OTP delivery. |
| `FR-015` | The system **SHALL** validate whether a user is subject to mandatory MFA. |
| `FR-016` | The system **SHALL** allow authorized administrators to enable or disable MFA for users. |
| `FR-017` | The system **SHALL** allow users to enable or disable their own MFA where policy permits. |
| `FR-018` | The system **SHALL** provide backup code retrieval or management for users. |
| `FR-019` | The system **SHALL** log MFA actions for audit and troubleshooting. |
| `FR-020` | The system **SHALL** expose supported country phone codes for phone-based MFA configuration. |

---

> **Related:** [[3.1-authentication|Authentication]] · [[5.2-security|Security Requirements]] · [[2.7-assumptions-dependencies|Dependencies (OTP providers)]]
""")

write("3.3-tenant-management.md", """---
title: "3.3 Tenant and System Settings Management"
tags: [srs, functional-requirements, tenant]
---

# 3.3 Tenant and System Settings Management

> [[index|← Index]] · [[3-system-features|← 3. System Features]]

| ID | Requirement |
|----|-------------|
| `FR-021` | The system **SHALL** provide tenant listing and tenant retrieval by numeric identifier. |
| `FR-022` | The system **SHALL** provide tenant retrieval by GUID. |
| `FR-023` | The system **SHALL** provide tenant lookup by domain and tenant GUID lookup by domain. |
| `FR-024` | The system **SHALL** allow authorized administrators to create tenants. |
| `FR-025` | The system **SHALL** allow authorized administrators to update tenants. |
| `FR-026` | The system **SHALL** allow authorized administrators to delete tenants where business rules permit. |
| `FR-027` | The system **SHALL** support system settings persistence through an authorized endpoint. |
| `FR-028` | The system **SHALL** ensure tenant operations are restricted to authorized platform-level users. |

---

> **Related:** [[2.5-design-constraints|Tenant Isolation Constraint]] · [[3.4-authorization|Authorization Policies]] · [[6.5-appendix-b|ISS-002 Tenant Isolation]]
""")

write("3.4-authorization.md", """---
title: "3.4 Authorization Policies and UI Permissions"
tags: [srs, functional-requirements, authorization, abac]
---

# 3.4 Authorization Policies and UI Permissions

> [[index|← Index]] · [[3-system-features|← 3. System Features]]

| ID | Requirement |
|----|-------------|
| `FR-029` | The system **SHALL** evaluate ABAC policies for named policy and action attributes. |
| `FR-030` | The system **SHALL** return policy definitions or policy evaluation metadata by policy name. |
| `FR-031` | The system **SHALL** provide field-level permission metadata for policy-driven UI rendering. |
| `FR-032` | The system **SHALL** provide UI permission lookup by policy name. |
| `FR-033` | The system **SHALL** provide UI permission lookup by policy name and request parameters. |
| `FR-034` | The system **SHALL** ensure authorization decisions are traceable and testable. |

---

> **Related:** [[3.1-authentication|Authentication]] · [[5.2-security|Security Requirements]] · [[6.5-appendix-b|ISS-009 Policy Catalog]]
""")

write("3.5-files-attachments.md", """---
title: "3.5 Files, Attachments, Notes, and Lookup Data"
tags: [srs, functional-requirements, files, attachments]
---

# 3.5 Files, Attachments, Notes, and Lookup Data

> [[index|← Index]] · [[3-system-features|← 3. System Features]]

| ID | Requirement |
|----|-------------|
| `FR-035` | The system **SHALL** support file upload through multipart or equivalent API requests. |
| `FR-036` | The system **SHALL** support Base64 file upload where required by clients. |
| `FR-037` | The system **SHALL** support temporary and permanent file download flows. |
| `FR-038` | The system **SHALL** support public download for explicitly public files only. |
| `FR-039` | The system **SHALL** list entity attachments by entity type, entity identifier, and module. |
| `FR-040` | The system **SHALL** support offset and keyset pagination for attachment lists. |
| `FR-041` | The system **SHALL** allow authorized users to add entity attachments. |
| `FR-042` | The system **SHALL** allow authorized users to delete entity attachments. |
| `FR-043` | The system **SHALL** allow authorized users to download entity attachments. |
| `FR-044` | The system **SHALL** allow authorized users to list, add, update, and delete notes for module entities. |
| `FR-045` | The system **SHALL** provide lookup values for countries, counties, gender, regions, and club types. |

---

> **Related:** [[2.4-operating-environment|Azure Blob Storage]] · [[3.6-cache-invalidation|Cache Invalidation]] · [[5.2-security|Security (FR-010)]]
""")

write("3.6-cache-invalidation.md", """---
title: "3.6 Cache Invalidation"
tags: [srs, functional-requirements, cache]
---

# 3.6 Cache Invalidation

> [[index|← Index]] · [[3-system-features|← 3. System Features]]

| ID | Requirement |
|----|-------------|
| `FR-046` | The system **SHALL** allow authorized users or services to invalidate cache entries by key. |
| `FR-047` | The system **SHALL** allow authorized users or services to invalidate cache entries by tag. |
| `FR-048` | The system **SHALL** allow authorized users or services to invalidate cache entries by pattern. |
| `FR-049` | The system **SHOULD** clearly separate public and protected cache invalidation endpoints. |
| `FR-050` | The system **SHALL** audit cache invalidation requests. |

---

> **Related:** [[5.1-performance|Performance (NFR-004)]] · [[5.2-security|Security Requirements]]
""")

write("3.7-asset-management.md", """---
title: "3.7 Asset Management"
tags: [srs, functional-requirements, assets]
---

# 3.7 Asset Management

> [[index|← Index]] · [[3-system-features|← 3. System Features]]

| ID | Requirement |
|----|-------------|
| `FR-051` | The system **SHALL** list asset categories for a given asset type. |
| `FR-052` | The system **SHALL** list asset types and retrieve asset type details. |
| `FR-053` | The system **SHALL** allow authorized users to create, save, list, view, delete, reinstate, and submit asset registers. |
| `FR-054` | The system **SHALL** support asset register status changes and maintain status history. |
| `FR-055` | The system **SHALL** provide asset register duplicate checking. |
| `FR-056` | The system **SHALL** provide notifications and journey completion steps for asset registers. |
| `FR-057` | The system **SHALL** support asset credential creation, editing, status change, credential product lookup, and permission lookup. |
| `FR-058` | The system **SHALL** support asset lease creation, editing, list views, activity logs, lease history, status changes, details, additional fee lookup, and owner approval metadata. |
| `FR-059` | The system **SHALL** support asset license metadata, creation, editing, cancellation, additional fees, deletion, upgrade metadata, cart validation, status changes, purchasable items, definitions, and permission lookup. |
| `FR-060` | The system **SHALL** support asset ownership transfer creation, history, permission lookup, details, activity logs, status changes, and owner approval metadata. |
| `FR-061` | The system **SHALL** provide asset metadata including credentials, tags, statuses, lease statuses, clubs, members, action reasons, asset details, additional forms, and additional fees. |
| `FR-062` | The system **SHALL** provide asset audit listing. |
| `FR-063` | The system **SHALL** provide asset report retrieval and download. |
| `FR-064` | The system **SHALL** support workflow submission for asset management processes. |

---

> **Related:** [[6.4-appendix-a|Asset API Surface]] · [[6.5-appendix-b|ISS-004 Asset Workflow Validation]]
""")

write("3.8-booking.md", """---
title: "3.8 Booking and Class Management"
tags: [srs, functional-requirements, booking]
---

# 3.8 Booking and Class Management

> [[index|← Index]] · [[3-system-features|← 3. System Features]]

| ID | Requirement |
|----|-------------|
| `FR-065` | The system **SHALL** provide booking catalog endpoints for disciplines, age groups, descriptions, filter metadata, and basic club details. |
| `FR-066` | The system **SHALL** list booking classes and return class details. |
| `FR-067` | The system **SHALL** return occurrence details for booking classes. |
| `FR-068` | The system **SHALL** return attendee and attendee payment information. |
| `FR-069` | The system **SHALL** return payment form information for attendees. |
| `FR-070` | The system **SHALL** return class group details and primary club GUID. |
| `FR-071` | The system **SHALL** allow authorized users to update invited users for class bookings. |
| `FR-072` | The system **SHALL** provide attendee list and session resolution operations. |
| `FR-073` | The system **SHALL** manage pricing chart discounts including create, update, status update, delete, list, and dropdown data. |
| `FR-074` | The system **SHALL** check member plan status for booking transfer requests. |
| `FR-075` | The system **SHALL** provide attendee occurrence calendar views and pro-rata calculations. |
| `FR-076` | The system **SHALL** provide term lookup data and holiday removal for class terms. |
| `FR-077` | The system **SHALL** provide profile class booking list and past class details. |
| `FR-078` | The system **SHALL** provide profile course booking details, cancellation, and booking QR links. |

---

> **Related:** [[6.4-appendix-a|Booking API Surface]] · [[6.5-appendix-b|ISS-005 Booking Finance Gap]] · [[3.11-referenced-modules|Finance Module (TBD)]]
""")

write("3.9-credential-management.md", """---
title: "3.9 Credential Management"
tags: [srs, functional-requirements, credentials]
---

# 3.9 Credential Management

> [[index|← Index]] · [[3-system-features|← 3. System Features]]

| ID | Requirement |
|----|-------------|
| `FR-079` | The system **SHALL** provide credential summary for a member by user GUID. |
| `FR-080` | The system **SHALL** list member credentials using query/filter criteria. |
| `FR-081` | The system **SHALL** provide credential categories. |
| `FR-082` | The system **SHALL** provide credential metadata for a selected credential identifier. |

---

> **Related:** [[3.7-asset-management|Asset Credentials (FR-057)]] · [[6.4-appendix-a|Credential API Surface]]
""")

write("3.10-field-management.md", """---
title: "3.10 Field Management and Entity Extensions"
tags: [srs, functional-requirements, field-management, extensions]
---

# 3.10 Field Management and Entity Extensions

> [[index|← Index]] · [[3-system-features|← 3. System Features]]

| ID | Requirement |
|----|-------------|
| `FR-083` | The system **SHALL** provide UI tab items for owner, entity, and extension context. |
| `FR-084` | The system **SHALL** provide UI tab organizations available to a user. |
| `FR-085` | The system **SHALL** provide dynamic UI schema for entity extension forms. |
| `FR-086` | The system **SHALL** provide arena-specific UI schema where configured. |
| `FR-087` | The system **SHALL** provide backend schema for extension forms. |
| `FR-088` | The system **SHALL** create or update extension schema by tab item identifier. |
| `FR-089` | The system **SHALL** delete configured extension forms where authorized. |
| `FR-090` | The system **SHALL** list field-level attachments for dynamic forms. |
| `FR-091` | The system **SHALL** upload fieldset attachments. |
| `FR-092` | The system **SHALL** retrieve and persist dynamic entity extension data. |
| `FR-093` | The system **SHALL** retrieve and persist form data by extension and entity identifiers. |
| `FR-094` | The system **SHALL** manage user weblet preferences by user and preference type. |

---

> **Related:** [[6.4-appendix-a|FieldManagement API Surface (18 actions)]] · [[3.5-files-attachments|File Attachments]]
""")

write("3.11-referenced-modules.md", """---
title: "3.11 Referenced Modules Requiring Source Validation"
tags: [srs, functional-requirements, tbd, missing-modules]
---

# 3.11 Referenced Modules Requiring Source Validation

> [[index|← Index]] · [[3-system-features|← 3. System Features]]

> ⚠️ These modules are referenced in project configuration and build output but **source code was not available** in the extracted archive.

| ID | Requirement |
|----|-------------|
| `FR-095` | The system **SHALL** document Finance module APIs after source code becomes available. |
| `FR-096` | The system **SHALL** document Member Profile module APIs after source code becomes available. |
| `FR-097` | The system **SHALL** document Membership module APIs after source code becomes available. |
| `FR-098` | The system **SHALL** document Organisation module APIs after source code becomes available. |
| `FR-099` | The system **SHALL** document Mobile Apps module APIs after source code becomes available. |
| `FR-100` | The system **SHALL** document Result module APIs after source code becomes available. |

---

> **Related:** [[1.4-project-scope|Project Scope]] · [[6.5-appendix-b|ISS-001 Missing Source]] · [[2.5-design-constraints|Design Constraints]]
""")

# ---------------------------------------------------------------------------
# 4. EXTERNAL INTERFACE REQUIREMENTS
# ---------------------------------------------------------------------------

write("4-external-interfaces.md", """---
title: "4. External Interface Requirements"
tags: [srs, interfaces]
---

# 4. External Interface Requirements

> [[index|← Back to Index]]

## Children
- [[4.1-user-interfaces|4.1 User Interfaces]]
- [[4.2-hardware-interfaces|4.2 Hardware Interfaces]]
- [[4.3-software-interfaces|4.3 Software Interfaces]]
- [[4.4-communications-interfaces|4.4 Communications Interfaces]]
""")

write("4.1-user-interfaces.md", """---
title: "4.1 User Interfaces"
tags: [srs, interfaces, ui]
---

# 4.1 User Interfaces

> [[index|← Index]] · [[4-external-interfaces|← 4. External Interfaces]]

The codebase represents **backend REST APIs**. User interfaces are external clients.

| Interface | Description |
|-----------|-------------|
| Admin portal | Tenant, user, MFA, policy, cache, file, note, asset, booking, credential, and field management functions |
| Member portal | Account access, member-specific bookings, credentials, assets, course/class data, and attachments |
| Mobile apps | Mobile APIs referenced by project configuration; source validation TBD |
| Partner/integration clients | REST API access using HTTPS, JSON, JWT, tenant context, and versioned endpoints |

---

> **Related:** [[2.3-user-classes|User Classes]] · [[4.4-communications-interfaces|Communications Interfaces]]
""")

write("4.2-hardware-interfaces.md", """---
title: "4.2 Hardware Interfaces"
tags: [srs, interfaces, hardware]
---

# 4.2 Hardware Interfaces

> [[index|← Index]] · [[4-external-interfaces|← 4. External Interfaces]]

No direct hardware interfaces were identified in source.

The system depends on:
- Server infrastructure
- Storage (disk/blob)
- Database server
- Network infrastructure
- Backup infrastructure

---

> **Related:** [[2.4-operating-environment|Operating Environment]]
""")

write("4.3-software-interfaces.md", """---
title: "4.3 Software Interfaces"
tags: [srs, interfaces, software]
---

# 4.3 Software Interfaces

> [[index|← Index]] · [[4-external-interfaces|← 4. External Interfaces]]

| Software Interface | Description |
|-------------------|-------------|
| SQL Server | Primary data access — EF Core SQL Server, `Microsoft.Data.SqlClient`, `System.Data.SqlClient`, Dapper |
| Azure Blob / File Storage | Referenced by file system manager and file APIs |
| JWT provider | Bearer authentication |
| Email/SMS/OTP provider | MFA, OTP, and password workflows; exact provider TBD |
| Payment provider | Adyen assembly present in build output; exact business usage TBD from missing source |
| Swagger/OpenAPI | API documentation and testing interface |
| SignalR | Real-time progress reporting hub at `/progressReportHub` |
| Serilog sinks | Console, event, and exception logging sinks |

---

> **Related:** [[2.7-assumptions-dependencies|Dependencies]] · [[2.4-operating-environment|Operating Environment]] · [[6.5-appendix-b|ISS-001]]
""")

write("4.4-communications-interfaces.md", """---
title: "4.4 Communications Interfaces"
tags: [srs, interfaces, communications]
---

# 4.4 Communications Interfaces

> [[index|← Index]] · [[4-external-interfaces|← 4. External Interfaces]]

| ID | Requirement |
|----|-------------|
| `IR-001` | The system **SHALL** expose RESTful APIs over HTTPS. |
| `IR-002` | The system **SHALL** use JSON as the primary request and response format. |
| `IR-003` | The system **SHALL** expose Swagger/OpenAPI documentation for discoverable APIs. |
| `IR-004` | The system **SHALL** support API versioning for versioned controllers. |
| `IR-005` | The system **SHALL** support SignalR communications for progress reports. |
| `IR-006` | The system **SHALL** use secure JWT bearer authentication for protected APIs. |
| `IR-007` | The system **SHALL** apply configured CORS policies to browser-based clients. |

---

> **Related:** [[5.2-security|Security Requirements]] · [[4.3-software-interfaces|Software Interfaces]]
""")

# ---------------------------------------------------------------------------
# 5. NON-FUNCTIONAL REQUIREMENTS
# ---------------------------------------------------------------------------

write("5-non-functional-requirements.md", """---
title: "5. Non-Functional Requirements"
tags: [srs, nfr]
---

# 5. Non-Functional Requirements

> [[index|← Back to Index]]

## Children
- [[5.1-performance|5.1 Performance Requirements]] _(NFR-001–NFR-005)_
- [[5.2-security|5.2 Security Requirements]] _(NFR-006–NFR-013)_
- [[5.3-reliability|5.3 Reliability and Availability]] _(NFR-014–NFR-018)_
- [[5.4-usability|5.4 Usability and Accessibility]] _(NFR-019–NFR-022)_
- [[5.5-maintainability|5.5 Maintainability and Portability]] _(NFR-023–NFR-027)_
- [[5.6-legal-compliance|5.6 Legal and Compliance Requirements]] _(NFR-028–NFR-031)_
- [[5.7-operational|5.7 Operational Requirements]] _(NFR-032–NFR-036)_
""")

write("5.1-performance.md", """---
title: "5.1 Performance Requirements"
tags: [srs, nfr, performance]
---

# 5.1 Performance Requirements

> [[index|← Index]] · [[5-non-functional-requirements|← 5. NFR]]

| ID | Requirement |
|----|-------------|
| `NFR-001` | The system **SHALL** respond to standard API requests within **3 seconds** under normal load, excluding external provider latency. |
| `NFR-002` | The system **SHALL** paginate large result sets using offset or keyset pagination where supported. |
| `NFR-003` | The system **SHALL** use response compression over HTTPS. |
| `NFR-004` | The system **SHOULD** cache frequently accessed reference data and invalidate cache explicitly when required. |
| `NFR-005` | The system **SHOULD** define benchmark targets for asset lists, booking lists, file operations, and authentication flows. |

---

> **Related:** [[3.6-cache-invalidation|Cache Invalidation]] · [[6.5-appendix-b|ISS-007 NFR Targets TBD]]
""")

write("5.2-security.md", """---
title: "5.2 Security Requirements"
tags: [srs, nfr, security]
---

# 5.2 Security Requirements

> [[index|← Index]] · [[5-non-functional-requirements|← 5. NFR]]

| ID | Requirement |
|----|-------------|
| `NFR-006` | The system **SHALL** require HTTPS in production. |
| `NFR-007` | The system **SHALL** protect APIs with JWT authentication except intentionally public endpoints. |
| `NFR-008` | The system **SHALL** enforce authorization using policies, roles, claims, ABAC, and/or UI permission rules. |
| `NFR-009` | The system **SHALL** support MFA for users and administrators according to configurable policy. |
| `NFR-010` | The system **SHALL** protect file download, attachment, cache, and administrative endpoints from unauthorized access. |
| `NFR-011` | The system **SHALL** avoid exposing hash/encrypt/decrypt helper endpoints in production unless strictly required and strongly restricted. |
| `NFR-012` | The system **SHALL** log security-relevant authentication, authorization, MFA, attachment, and administrative events. |
| `NFR-013` | The system **SHALL** prevent cross-tenant access to data and configuration. |

---

> **Related:** [[3.1-authentication|Authentication]] · [[3.2-mfa|MFA]] · [[3.4-authorization|Authorization]] · [[6.5-appendix-b|ISS-003 Hash Endpoints]] · [[6.5-appendix-b|ISS-009 Policy Catalog]]
""")

write("5.3-reliability.md", """---
title: "5.3 Reliability and Availability"
tags: [srs, nfr, reliability]
---

# 5.3 Reliability and Availability

> [[index|← Index]] · [[5-non-functional-requirements|← 5. NFR]]

| ID | Requirement |
|----|-------------|
| `NFR-014` | The system **SHALL** provide centralized exception handling and consistent problem responses. |
| `NFR-015` | The system **SHALL** log application requests, events, and exceptions. |
| `NFR-016` | The system **SHOULD** define availability targets for production and non-production environments. |
| `NFR-017` | The system **SHOULD** implement retry/circuit-breaker patterns for external dependencies. |
| `NFR-018` | The system **SHOULD** define backup, restore, RTO, and RPO targets for SQL Server and file storage. |

---

> **Related:** [[5.7-operational|Operational Requirements]] · [[6.5-appendix-b|ISS-007 NFR Targets TBD]]
""")

write("5.4-usability.md", """---
title: "5.4 Usability and Accessibility"
tags: [srs, nfr, usability]
---

# 5.4 Usability and Accessibility

> [[index|← Index]] · [[5-non-functional-requirements|← 5. NFR]]

| ID | Requirement |
|----|-------------|
| `NFR-019` | The API **SHALL** provide consistent naming, response structure, and error formats for client usability. |
| `NFR-020` | The API **SHALL** expose Swagger/OpenAPI documentation for discoverability. |
| `NFR-021` | The API **SHOULD** provide clear validation messages for invalid request payloads. |
| `NFR-022` | Client applications **SHOULD** meet WCAG accessibility targets; backend APIs **SHALL** provide data necessary for accessible UI rendering where applicable. |

---

> **Related:** [[4.4-communications-interfaces|Communications Interfaces]] · [[6.5-appendix-b|ISS-006 API Response Schema]]
""")

write("5.5-maintainability.md", """---
title: "5.5 Maintainability and Portability"
tags: [srs, nfr, maintainability]
---

# 5.5 Maintainability and Portability

> [[index|← Index]] · [[5-non-functional-requirements|← 5. NFR]]

| ID | Requirement |
|----|-------------|
| `NFR-023` | The system **SHALL** preserve modular boundaries and dependency injection registration per module. |
| `NFR-024` | The system **SHALL** maintain automated tests for business-critical commands, queries, controllers, and validation rules. |
| `NFR-025` | The system **SHOULD** document module ownership, route maps, data models, and workflows. |
| `NFR-026` | The system **SHOULD** keep configuration outside code and use environment-specific settings. |
| `NFR-027` | The system **SHOULD** support containerized or repeatable deployment. |

---

> **Related:** [[2.5-design-constraints|Design Constraints]] · [[6.3-training|Training Requirements]]
""")

write("5.6-legal-compliance.md", """---
title: "5.6 Legal and Compliance Requirements"
tags: [srs, nfr, compliance, legal]
---

# 5.6 Legal and Compliance Requirements

> [[index|← Index]] · [[5-non-functional-requirements|← 5. NFR]]

| ID | Requirement |
|----|-------------|
| `NFR-028` | The system **SHALL** define data retention and privacy rules for user data, files, MFA logs, notes, audit events, and tenant data. |
| `NFR-029` | The system **SHALL** support auditability of administrative and security-sensitive operations. |
| `NFR-030` | The system **SHALL** respect applicable local privacy, consumer, payment, and sports organization regulations. |
| `NFR-031` | The system **SHALL** document third-party component licenses and provider agreements. |

---

> **Related:** [[5.2-security|Security Requirements]] · [[6.1-data-migration|Data Migration]]
""")

write("5.7-operational.md", """---
title: "5.7 Operational Requirements"
tags: [srs, nfr, operations]
---

# 5.7 Operational Requirements

> [[index|← Index]] · [[5-non-functional-requirements|← 5. NFR]]

| ID | Requirement |
|----|-------------|
| `NFR-032` | The system **SHALL** expose health, logging, monitoring, and diagnostic capabilities suitable for production operations. |
| `NFR-033` | The system **SHALL** document deployment steps, configuration keys, secrets, and infrastructure dependencies. |
| `NFR-034` | The system **SHALL** maintain log retention and secure access to logs. |
| `NFR-035` | The system **SHOULD** provide dashboards or alerts for authentication failures, MFA failures, API errors, slow queries, and storage failures. |
| `NFR-036` | The system **SHALL** document support procedures for tenant setup, user lockout, MFA reset, file recovery, booking/payment incidents, and asset workflow issues. |

---

> **Related:** [[5.3-reliability|Reliability]] · [[6.3-training|Training]] · [[2.6-user-documentation|User Documentation]]
""")

# ---------------------------------------------------------------------------
# 6. OTHER REQUIREMENTS
# ---------------------------------------------------------------------------

write("6-other-requirements.md", """---
title: "6. Other Requirements"
tags: [srs, other]
---

# 6. Other Requirements

> [[index|← Back to Index]]

## Children
- [[6.1-data-migration|6.1 Data Migration]]
- [[6.2-internationalization|6.2 Internationalization Requirements]]
- [[6.3-training|6.3 Training Requirements]]
- [[6.4-appendix-a|6.4 Appendix A: Analysis Models]]
- [[6.5-appendix-b|6.5 Appendix B: Issues List]]
""")

write("6.1-data-migration.md", """---
title: "6.1 Data Migration"
tags: [srs, other, migration]
---

# 6.1 Data Migration

> [[index|← Index]] · [[6-other-requirements|← 6. Other Requirements]]

- The system **SHALL** define migration procedures for tenant, user, membership, asset, booking, credential, attachment, and extension data where applicable.
- The system **SHALL** validate migrated data for completeness, consistency, tenant ownership, and referential integrity.
- The system **SHALL** provide rollback and reconciliation procedures for migration defects.

---

> **Related:** [[5.6-legal-compliance|Legal & Compliance]] · [[6.5-appendix-b|ISS-008 Database Schema Gap]]
""")

write("6.2-internationalization.md", """---
title: "6.2 Internationalization Requirements"
tags: [srs, other, i18n, localization]
---

# 6.2 Internationalization Requirements

> [[index|← Index]] · [[6-other-requirements|← 6. Other Requirements]]

- The API **SHOULD** support localization-ready lookup data and messages where clients require multiple languages.
- Date, time, currency, phone number, and address formats **SHOULD** be normalized and documented.
- Tenant-specific localization and terminology **SHOULD** be supported where product configuration requires it.

---

> **Related:** [[3.5-files-attachments|Lookup Data (FR-045)]] · [[2.3-user-classes|User Classes]]
""")

write("6.3-training.md", """---
title: "6.3 Training Requirements"
tags: [srs, other, training]
---

# 6.3 Training Requirements

> [[index|← Index]] · [[6-other-requirements|← 6. Other Requirements]]

| Audience | Training Topics |
|----------|----------------|
| Developers | Modular architecture, API conventions, authentication, authorization, logging, and testing |
| Administrators | Tenant management, user support, MFA support, files, notes, cache, and module operations |
| QA | Deriving API tests from this SRS and Swagger route maps |
| Operations | Deployment, monitoring, backup, incident response, and provider troubleshooting |

---

> **Related:** [[2.6-user-documentation|User Documentation]] · [[5.5-maintainability|Maintainability]] · [[5.7-operational|Operational Requirements]]
""")

write("6.4-appendix-a.md", """---
title: "6.4 Appendix A: Analysis Models"
tags: [srs, appendix, architecture, api-surface]
---

# 6.4 Appendix A: Analysis Models

> [[index|← Index]] · [[6-other-requirements|← 6. Other Requirements]]

## A.1 Context Diagram

| Element | Description |
|---------|-------------|
| Client Applications | Web portal, admin portal, member portal, mobile apps, partner systems |
| JustGoAPI Host | ASP.NET Core API host, versioning, Swagger, middleware, authentication, logging, SignalR |
| Business Modules | Auth, Asset Management, Booking, Credential, Field Management, and referenced modules |
| Data Stores | SQL Server and file/blob storage |
| External Services | Email/SMS/OTP, payment provider, logging/monitoring infrastructure |

## A.2 Module Diagram

| Module | Observed Responsibility |
|--------|------------------------|
| `JustGoAPI.API` | Composes controllers and modules, configures middleware and hosted API services |
| `AuthModule` | Accounts, users, tenants, MFA, policies, UI permissions, files, notes, lookups, cache |
| `AssetManagementModule` | Asset registers, categories, credentials, leases, licenses, ownership transfers, workflows, audits, reports |
| `BookingModule` | Catalog, classes, occurrences, attendees, payments, pricing discounts, transfer checks, terms, profile bookings |
| `CredentialModule` | Member credential summary, member credential list, categories, metadata |
| `FieldManagementModule` | Dynamic entity extension schema, UI schema, form data, attachments, preferences |
| Referenced modules | Finance, MemberProfile, Membership, Organisation, MobileApps, Result — source validation required |

## A.3 API Surface Summary

| Controller Area | HTTP Actions |
|----------------|-------------|
| AssetManagement / AssetAudit | 1 |
| AssetManagement / AssetCategories | 1 |
| AssetManagement / AssetCheckout | 2 |
| AssetManagement / AssetCredentials | 6 |
| AssetManagement / AssetLeases | 10 |
| AssetManagement / AssetLicenses | 14 |
| AssetManagement / AssetMetadata | 11 |
| AssetManagement / AssetOwnershipTransfers | 7 |
| AssetManagement / AssetRegisters | 13 |
| AssetManagement / AssetReports | 2 |
| AssetManagement / AssetTypes | 2 |
| AssetManagement / Clubs | 5 |
| AssetManagement / Workflows | 1 |
| Auth / AbacAuthorizes | 4 |
| Auth / Accounts | 13 |
| Auth / CacheInvalidation | 6 |
| Auth / Files | 14 |
| Auth / Lookup | 5 |
| Auth / Notes | 6 |
| Auth / Tenants | 8 |
| Auth / UiPermissions | 3 |
| Auth / Users | 6 |
| Booking / BookingCatalog | 6 |
| Booking / BookingClass | 11 |
| Booking / BookingPricingChartDiscount | 6 |
| Booking / BookingTransferRequest | 1 |
| Booking / ClassManagement | 2 |
| Booking / ClassTerm | 2 |
| Booking / ProfileClassBooking | 2 |
| Booking / ProfileCourseBooking | 3 |
| Credential / Credentials | 4 |
| FieldManagement / EntityExtensions | 18 |
| **Total** | **175** |

---

> **Related:** [[3-system-features|Functional Requirements]] · [[2.1-product-perspective|Product Perspective]]
""")

write("6.5-appendix-b.md", """---
title: "6.5 Appendix B: Issues List"
tags: [srs, appendix, issues, tbd]
---

# 6.5 Appendix B: Issues List

> [[index|← Index]] · [[6-other-requirements|← 6. Other Requirements]]

| ID | Issue | Priority | Recommended Action |
|----|-------|----------|-------------------|
| `ISS-001` | Source code for Finance, MemberProfile, Membership, Organisation, MobileApps, and Result modules is referenced but not present in the extracted source tree; only build outputs appear available. | **High** | Obtain full source and rescan. |
| `ISS-002` | Tenant isolation rules are not fully documented. | **High** | Validate tenant resolution, database routing, and cross-tenant authorization rules. |
| `ISS-003` | Production use of hash/encrypt/decrypt helper endpoints needs security review. | **High** | Confirm whether these endpoints are development-only or restrict/remove in production. |
| `ISS-004` | Business workflows for asset status transitions, leases, licenses, transfers, and approvals require stakeholder validation. | **High** | Workshop with product owners and support team. |
| `ISS-005` | Booking payment and finance flows are incomplete due to missing source modules. | **High** | Rescan full Finance and payment source. |
| `ISS-006` | API response schema and error code conventions require formal documentation. | **Medium** | Generate OpenAPI snapshot and define response standard. |
| `ISS-007` | Non-functional targets — availability, throughput, RTO, RPO, retention, and audit retention — are TBD. | **Medium** | Confirm production SLA and compliance requirements. |
| `ISS-008` | Data model, ERD, stored procedures, and database schema were not available in this scan. | **High** | Obtain database schema/scripts and generate DR section. |
| `ISS-009` | Security model uses ABAC/UI permissions/custom middleware, but policy catalog is not documented. | **High** | Export and document policy names, action attributes, and field permissions. |
| `ISS-010` | Swagger may only show available compiled modules at runtime; static source scan cannot confirm missing module routes. | **Medium** | Run API locally with full source and export OpenAPI JSON. |

---

> **Related:** [[3.11-referenced-modules|Referenced Modules]] · [[5.2-security|Security Requirements]] · [[3.4-authorization|Authorization Policies]]
""")

print("\nDone. All files written to docs/SRS/")
