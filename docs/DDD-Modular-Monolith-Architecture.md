---
title: DDD Modular Monolith Architecture Plan
tags: [architecture, ddd, modular-monolith]
date: 2026-04-26
status: approved
---

# DDD Modular Monolith Architecture — JustGo Platform

## Context

**Current state:** 11 modules in `src/Modules/`, each with API / Application / Domain / Infrastructure layers. The existing `AuthModule` conflates 3 DDD domains (Identity + Tenancy + Authorization) into one module. There is no Contracts layer, no SharedKernel separation, and cross-cutting concerns live in `src/JustGo.Authentication/` — a shared infrastructure project, not a proper module.

**Goal:** Restructure to a true modular monolith where:

- Each **module = one DDD domain**
- **Bounded Contexts = internal namespaces** (subfolders) within the domain's Domain project
- **Inter-module communication** goes through a `Contracts` project only — never across internal types
- Each module registers itself via a single `services.Add{Domain}Module()` extension

---

## Domain → Module Mapping

| Domain | Type | Module project prefix | BCs inside |
| ------ | ---- | --------------------- | ---------- |
| Identity | Supporting | `JustGo.Identity` | Authentication · AccountManagement · MFA |
| Tenancy | Supporting | `JustGo.Tenancy` | TenantManagement |
| Authorization | Supporting | `JustGo.Authorization` | PolicyEvaluation · UiPermissions |
| Asset Management | **Core** | `JustGo.AssetManagement` | Catalogue · Register · Leasing · Licensing · OwnershipTransfer · Credentials · Operations · AuditReporting |
| Booking | **Core** | `JustGo.Booking` | Catalogue · ClassSchedule · Attendee · PricingDiscounts · Transfer · ProfileBooking |
| Credentialing | **Core** | `JustGo.Credentialing` | MemberCredentialing |
| Field Management | Supporting | `JustGo.FieldManagement` | ExtensionSchema · ExtensionData · UserPreferences |
| Content | Generic (shared) | `JustGo.Content` | FileStorage · Attachments · Notes |
| Reference Data | Generic (shared) | `JustGo.ReferenceData` | Lookup |

---

## What Changes vs Current

| Current | Target | Action |
| ------- | ------ | ------ |
| `AuthModule` (mega-module) | `Identity` + `Tenancy` + `Authorization` | Split |
| No Contracts project | `JustGo.{Domain}.Contracts` per module | Add |
| `JustGo.Authentication` shared infra | Absorbed into Identity + Authorization + SharedKernel | Restructure |
| `JustGoAPI.Shared` | `JustGo.SharedKernel` | Rename + expand |
| Flat domain layer | BC = subfolder inside Domain project | Reorganise |
| `MobileAppsModule` | Not a domain — delivery channel only | Remove |
| `JustGo.Credential.*` | Rename to `JustGo.Credentialing.*` | Rename |

---

## Full Solution Structure

```text
JustGoAPI.sln
│
├── src/
│   │
│   ├── Host/
│   │   └── JustGoAPI.API/                            ← entry point only; no business logic
│   │       ├── Program.cs
│   │       ├── appsettings.json
│   │       ├── ApiVersioning/
│   │       └── SwaggerConfig/
│   │
│   ├── SharedKernel/
│   │   │
│   │   ├── JustGo.SharedKernel/                      ← domain primitives + base classes
│   │   │   ├── Abstractions/
│   │   │   │   ├── AggregateRoot.cs
│   │   │   │   ├── Entity.cs
│   │   │   │   ├── ValueObject.cs
│   │   │   │   └── IDomainEvent.cs
│   │   │   ├── Pagination/                           ← offset + keyset (moved from JustGo.Authentication)
│   │   │   └── Results/                              ← Result<T>, Error types
│   │   │
│   │   ├── JustGo.Content.Module/                    ← Generic subdomain: File · Attachment · Notes
│   │   │   ├── Contracts/
│   │   │   │   └── IContentService.cs
│   │   │   ├── Domain/
│   │   │   │   ├── FileStorage/                      ← BC — AR: StoredFile
│   │   │   │   ├── Attachments/                      ← BC — AR: EntityAttachment
│   │   │   │   └── Notes/                            ← BC — AR: EntityNote
│   │   │   ├── Application/
│   │   │   ├── Infrastructure/                       ← Azure Blob (from JustGo.Authentication/FileSystemManager)
│   │   │   └── ContentModule.cs
│   │   │
│   │   └── JustGo.ReferenceData.Module/              ← Generic subdomain: Lookup (read-only)
│   │       ├── Domain/
│   │       ├── Infrastructure/
│   │       └── ReferenceDataModule.cs
│   │
│   └── Modules/
│       │
│       ├── Identity/
│       │   ├── JustGo.Identity.Contracts/            ← only thing other modules may reference
│       │   │   ├── IIdentityService.cs
│       │   │   ├── Queries/
│       │   │   │   └── GetUserByIdQuery.cs
│       │   │   └── IntegrationEvents/
│       │   │       └── UserAuthenticatedEvent.cs
│       │   ├── JustGo.Identity.Domain/
│       │   │   ├── Authentication/                   ← BC — AR: AuthSession
│       │   │   │   ├── Aggregates/
│       │   │   │   │   └── AuthSession.cs
│       │   │   │   ├── Entities/
│       │   │   │   │   └── RefreshToken.cs
│       │   │   │   ├── ValueObjects/
│       │   │   │   │   ├── SessionId.cs
│       │   │   │   │   ├── AccessToken.cs
│       │   │   │   │   ├── TokenClaims.cs
│       │   │   │   │   ├── AuthenticationMethod.cs
│       │   │   │   │   └── SessionStatus.cs
│       │   │   │   ├── Events/
│       │   │   │   │   ├── UserAuthenticated.cs
│       │   │   │   │   ├── TokenRefreshed.cs
│       │   │   │   │   ├── SessionRevoked.cs
│       │   │   │   │   └── TokenReuseDetected.cs
│       │   │   │   └── Exceptions/
│       │   │   ├── AccountManagement/                ← BC — AR: User
│       │   │   │   ├── Aggregates/User.cs
│       │   │   │   ├── ValueObjects/
│       │   │   │   └── Events/
│       │   │   └── MFA/                              ← BC — AR: MfaConfiguration
│       │   │       ├── Aggregates/MfaConfiguration.cs
│       │   │       ├── ValueObjects/
│       │   │       └── Events/
│       │   ├── JustGo.Identity.Application/
│       │   │   ├── Authentication/
│       │   │   │   └── Commands/
│       │   │   ├── AccountManagement/
│       │   │   │   └── Commands/
│       │   │   └── MFA/
│       │   │       └── Commands/
│       │   ├── JustGo.Identity.Infrastructure/
│       │   │   ├── Persistence/
│       │   │   │   └── IdentityDbContext.cs          ← schema: identity
│       │   │   ├── JwtAuthentication/                ← moved from JustGo.Authentication/JwtAuthentication
│       │   │   └── Repositories/
│       │   ├── JustGo.Identity.API/
│       │   │   └── Controllers/
│       │   └── IdentityModule.cs                     ← services.AddIdentityModule()
│       │
│       ├── Tenancy/
│       │   ├── JustGo.Tenancy.Contracts/
│       │   │   ├── ITenancyService.cs
│       │   │   └── IntegrationEvents/
│       │   │       └── TenantCreatedEvent.cs
│       │   ├── JustGo.Tenancy.Domain/
│       │   │   └── TenantManagement/                 ← BC — AR: Tenant
│       │   │       ├── Aggregates/Tenant.cs
│       │   │       └── ValueObjects/
│       │   ├── JustGo.Tenancy.Application/
│       │   ├── JustGo.Tenancy.Infrastructure/
│       │   │   └── Persistence/TenancyDbContext.cs   ← schema: tenancy
│       │   ├── JustGo.Tenancy.API/
│       │   └── TenancyModule.cs
│       │
│       ├── Authorization/
│       │   ├── JustGo.Authorization.Contracts/
│       │   │   └── IAuthorizationService.cs
│       │   ├── JustGo.Authorization.Domain/
│       │   │   ├── PolicyEvaluation/                 ← BC — AR: Policy
│       │   │   │   └── Aggregates/Policy.cs
│       │   │   └── UiPermissions/                    ← BC — AR: UiPermissionSet
│       │   │       └── Aggregates/UiPermissionSet.cs
│       │   ├── JustGo.Authorization.Application/
│       │   ├── JustGo.Authorization.Infrastructure/
│       │   │   ├── AbacAuthorization/                ← moved from JustGo.Authentication/AbacAuthorization
│       │   │   └── Persistence/AuthorizationDbContext.cs  ← schema: authz
│       │   ├── JustGo.Authorization.API/
│       │   └── AuthorizationModule.cs
│       │
│       ├── AssetManagement/
│       │   ├── JustGo.AssetManagement.Contracts/
│       │   ├── JustGo.AssetManagement.Domain/
│       │   │   ├── AssetCatalogue/                   ← BC — AR: AssetType
│       │   │   ├── AssetRegister/                    ← BC — AR: AssetRegister
│       │   │   ├── AssetLeasing/                     ← BC — AR: Lease
│       │   │   ├── AssetLicensing/                   ← BC — AR: License
│       │   │   ├── AssetOwnershipTransfer/           ← BC — AR: OwnershipTransfer
│       │   │   ├── AssetCredentials/                 ← BC — AR: AssetCredential
│       │   │   ├── AssetOperations/                  ← BC — AR: Asset
│       │   │   └── AssetAuditReporting/              ← BC — AR: AuditEntry (read model)
│       │   ├── JustGo.AssetManagement.Application/
│       │   ├── JustGo.AssetManagement.Infrastructure/
│       │   │   └── Persistence/AssetManagementDbContext.cs  ← schema: assets
│       │   ├── JustGo.AssetManagement.API/
│       │   └── AssetManagementModule.cs
│       │
│       ├── Booking/
│       │   ├── JustGo.Booking.Contracts/
│       │   ├── JustGo.Booking.Domain/
│       │   │   ├── BookingCatalogue/                 ← BC — AR: CatalogueItem
│       │   │   ├── ClassScheduleManagement/          ← BC — AR: BookingClass
│       │   │   ├── AttendeeManagement/               ← BC — AR: Attendee
│       │   │   ├── PricingDiscounts/                 ← BC — AR: PricingChartDiscount
│       │   │   ├── TransferManagement/               ← BC — AR: TransferRequest
│       │   │   └── ProfileBooking/                   ← BC — AR: ProfileBooking
│       │   ├── JustGo.Booking.Application/
│       │   ├── JustGo.Booking.Infrastructure/
│       │   │   └── Persistence/BookingDbContext.cs   ← schema: booking
│       │   ├── JustGo.Booking.API/
│       │   └── BookingModule.cs
│       │
│       ├── Credentialing/
│       │   ├── JustGo.Credentialing.Contracts/
│       │   ├── JustGo.Credentialing.Domain/
│       │   │   └── MemberCredentialing/              ← BC — AR: MemberCredential
│       │   ├── JustGo.Credentialing.Application/
│       │   ├── JustGo.Credentialing.Infrastructure/
│       │   │   └── Persistence/CredentialingDbContext.cs  ← schema: credential
│       │   ├── JustGo.Credentialing.API/
│       │   └── CredentialingModule.cs
│       │
│       ├── FieldManagement/
│       │   ├── JustGo.FieldManagement.Contracts/
│       │   ├── JustGo.FieldManagement.Domain/
│       │   │   ├── ExtensionSchema/                  ← BC — AR: ExtensionSchema
│       │   │   ├── ExtensionData/                    ← BC — AR: ExtensionData
│       │   │   └── UserPreferences/                  ← BC — AR: WebletPreference
│       │   ├── JustGo.FieldManagement.Application/
│       │   ├── JustGo.FieldManagement.Infrastructure/
│       │   │   └── Persistence/FieldManagementDbContext.cs  ← schema: fieldmgmt
│       │   ├── JustGo.FieldManagement.API/
│       │   └── FieldManagementModule.cs
│       │
│       └── _Placeholder/                             ← TBD: source not yet available
│           ├── JustGo.Finance.Module/
│           ├── JustGo.MemberProfile.Module/
│           ├── JustGo.Membership.Module/
│           ├── JustGo.Organisation.Module/
│           └── JustGo.Result.Module/
│
└── tests/
    ├── JustGo.ArchitectureTests/                     ← enforce boundary rules (NetArchTest)
    ├── Identity/
    │   ├── JustGo.Identity.UnitTests/
    │   └── JustGo.Identity.IntegrationTests/
    ├── Tenancy/
    │   └── JustGo.Tenancy.UnitTests/
    ├── Authorization/
    │   └── JustGo.Authorization.UnitTests/
    ├── AssetManagement/
    │   ├── JustGo.AssetManagement.UnitTests/
    │   └── JustGo.AssetManagement.IntegrationTests/
    ├── Booking/
    │   ├── JustGo.Booking.UnitTests/
    │   └── JustGo.Booking.IntegrationTests/
    ├── Credentialing/
    │   └── JustGo.Credentialing.UnitTests/
    └── FieldManagement/
        └── JustGo.FieldManagement.UnitTests/
```

---

## Module Anatomy (per domain)

Every domain module follows the same 5-project pattern:

```text
JustGo.{Domain}.Contracts        ← public interface; the ONLY thing other modules reference
JustGo.{Domain}.Domain           ← pure C#; BCs as subfolders; no infrastructure deps
JustGo.{Domain}.Application      ← commands, queries, handlers (MediatR)
JustGo.{Domain}.Infrastructure   ← DbContext, repositories, external service adapters
JustGo.{Domain}.API              ← controllers only; delegates to Application layer
{Domain}Module.cs                ← services.Add{Domain}Module() DI entry point
```

### Project dependency rules (enforced by ArchitectureTests)

```text
API          → Application, Contracts
Application  → Domain, Contracts (of other modules)
Domain       → SharedKernel only
Infrastructure → Domain, SharedKernel
Host         → all Module.cs entry points only
```

---

## Module Registration — Program.cs

```csharp
// src/Host/JustGoAPI.API/Program.cs
builder.Services
    .AddSharedKernel()
    .AddContentModule(builder.Configuration)
    .AddReferenceDataModule()
    .AddIdentityModule(builder.Configuration)     // JWT config needed
    .AddTenancyModule()
    .AddAuthorizationModule()
    .AddAssetManagementModule()
    .AddBookingModule()
    .AddCredentialingModule()
    .AddFieldManagementModule();
```

---

## Inter-Module Communication Rules

| Rule | How |
| ---- | --- |
| Module A needs data from Module B | Calls `IModuleBService` defined in `JustGo.B.Contracts` |
| Module A reacts to Module B event | Subscribes to `IntegrationEvent` from `JustGo.B.Contracts` |
| No cross-module domain type leakage | `JustGo.B.Domain` is never referenced outside module B |
| SharedKernel is the only shared code | `AggregateRoot`, `Entity`, `ValueObject`, `IDomainEvent` |

---

## Database Isolation

Same physical SQL Server database — separate schemas per module, separate `DbContext` per module.

| Module | Schema | DbContext |
| ------ | ------ | --------- |
| Identity | `identity` | `IdentityDbContext` |
| Tenancy | `tenancy` | `TenancyDbContext` |
| Authorization | `authz` | `AuthorizationDbContext` |
| AssetManagement | `assets` | `AssetManagementDbContext` |
| Booking | `booking` | `BookingDbContext` |
| Credentialing | `credential` | `CredentialingDbContext` |
| FieldManagement | `fieldmgmt` | `FieldManagementDbContext` |
| Content | `content` | `ContentDbContext` |
| ReferenceData | `refdata` | `ReferenceDataDbContext` |

---

## Migration Path from Current Codebase

### AuthModule split (biggest change)

| Current location | Destination |
| ---------------- | ----------- |
| `AuthModule.Domain` — auth/user/MFA entities | `JustGo.Identity.Domain` |
| `AuthModule.Domain` — tenant entities | `JustGo.Tenancy.Domain` |
| `AuthModule.Domain` — ABAC/policy entities | `JustGo.Authorization.Domain` |
| `AuthModule.Application` | Split across Identity / Tenancy / Authorization Application |
| `AuthModule.Infrastructure` | Split across Identity / Tenancy / Authorization Infrastructure |
| `AuthModule.API` — account/user/MFA controllers | `JustGo.Identity.API` |
| `AuthModule.API` — tenant controllers | `JustGo.Tenancy.API` |
| `AuthModule.API` — ABAC/permissions controllers | `JustGo.Authorization.API` |

### JustGo.Authentication shared infra split

| Current location | Destination |
| ---------------- | ----------- |
| `JwtAuthentication/` | `JustGo.Identity.Infrastructure/JwtAuthentication/` |
| `AbacAuthorization/` | `JustGo.Authorization.Infrastructure/AbacAuthorization/` |
| `FileSystemManager/` | `JustGo.Content.Module/Infrastructure/` |
| `Caching/` | `JustGo.SharedKernel/Caching/` |
| `Pagination/` | `JustGo.SharedKernel/Pagination/` |
| `CustomErrors/` | `JustGo.SharedKernel/Errors/` |
| `Behaviors/` | `JustGo.SharedKernel/Behaviors/` |

### Other modules

| Current | Action |
| ------- | ------ |
| `JustGo.AssetManagement.*` | Keep — add Contracts project + BC subfolders to Domain |
| `JustGo.Booking.*` | Keep — add Contracts project + BC subfolders to Domain |
| `JustGo.Credential.*` | Rename to `JustGo.Credentialing.*` + add Contracts + BC subfolders |
| `JustGo.FieldManagement.*` | Keep — add Contracts project + BC subfolders to Domain |
| `JustGoAPI.Shared` | Merge into `JustGo.SharedKernel` |
| `MobileAppsModule` | Remove — not a domain; route mobile traffic through existing API modules |
| `JustGo.RuleEngine` | Evaluate — likely belongs in Authorization.Infrastructure |

---

## Architecture Test Assertions

`tests/JustGo.ArchitectureTests/` (NetArchTest) must enforce:

1. No module project references another module's `Domain` or `Application` project directly.
2. All cross-module dependencies go through a `*.Contracts` project.
3. `*.Domain` projects reference only `JustGo.SharedKernel`.
4. `*.Application` projects do not reference `*.Infrastructure` projects.
5. `*.Infrastructure` projects do not reference `*.API` projects.
6. Each module folder contains exactly one `*Module.cs` registration class.

---

## Related Documents

- [[SRS/index|SRS Index]]
- [[SRS/2.2-product-functions|2.2 Product Functions]] — domain source
- [[SRS/3-system-features|3. System Features]] — BC source
- [[SRS/6.4-appendix-a|Appendix A: API Surface Summary]]
