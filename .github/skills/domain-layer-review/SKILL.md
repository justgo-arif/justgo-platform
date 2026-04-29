---
name: domain-layer-review
description: "Review Domain Layer code against Clean Architecture and DDD standards. Use when: reviewing entities, value objects, aggregates, domain services, or domain events; checking for anemic domain model; verifying no infrastructure or application leakage into Domain; auditing invariant enforcement; checking module boundary violations in Domain. Commands: /domain-layer-review or automatic on domain code reviews."
argument-hint: "Optional: path or class name to review (e.g., BookingModule.Domain/Entities/Booking.cs)"
user-invocable: true
---

# Domain Layer Review

## Core Principle

**Review against standard practice first, existing repo convention second.**

Do not force a full rich-domain rewrite. Every new business rule should pull the model one step away from anemic data bags and one step toward real domain behavior. Existing weak patterns are not automatically acceptable — call them out as risk, and do not encourage new code to copy them.

---

## Review Checklist

### 1. Anemic vs. Rich Domain Model

Domain entities should **own their behavior**. Public setters on all properties with no behavior on the entity is an anemic model.

**Flag:**
- All properties have `public set;` with no methods beyond getters
- Business operations are implemented entirely in Application or Infrastructure
- Entities are used as plain data bags passed between layers

**Good direction (no full rewrite required):**
```csharp
// ❌ Anemic — all mutation is external
public class Membership
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CancelledAt { get; set; }
}

// ✅ Encapsulated — domain owns the behavior
public class Membership
{
    public Guid Id { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    public void Cancel(DateTime cancelledAt)
    {
        if (!IsActive) throw new DomainException("Membership is already cancelled.");
        IsActive = false;
        CancelledAt = cancelledAt;
    }
}
```

**Review comment:**
> [Domain Model] Entity exposes mutable public setters with no encapsulated behavior. New business rules for this entity should be implemented as domain methods, not in Application handlers.

---

### 2. Invariant Enforcement

Domain invariants (rules that must always hold) belong in the entity or value object, not scattered across handlers and validators.

**Flag:**
- Business invariant checks live only in Application validators or handlers
- An entity can be put into an invalid state by setting properties directly
- Constructors accept raw primitives for values with known constraints (e.g., negative price, empty name)

**Review comment:**
> [Invariant] This constraint is a business rule that belongs in the domain. Move it into the entity constructor or a domain method so the object can never be in an invalid state.

---

### 3. Value Objects

Use value objects for concepts defined by their attributes, not identity. Examples: `Money`, `Address`, `DateRange`, `Email`, `PhoneNumber`.

**Flag:**
- Repeated primitive groupings (e.g., `decimal Amount + string Currency`) not extracted as value objects
- Equality logic for domain concepts implemented in Application or compared manually with multiple fields

**Review comment:**
> [Value Object] This concept has no identity and is compared by value. Extract it as a `ValueObject` to encapsulate equality, validation, and behavior.

---

### 4. Aggregate Boundaries

An aggregate is a cluster of objects treated as a single unit for data changes. The aggregate root controls all access and modification to its children.

**Flag:**
- Child entities modified directly from outside the aggregate root
- Repository methods that return non-root domain entities
- No clear aggregate root; all entities are accessed independently

**Review comment:**
> [Aggregate] Child entity is modified from outside the aggregate root. Route the operation through the root to enforce consistency.

---

### 5. Domain Services

Use a domain service when business logic involves multiple aggregates or entities and does not naturally belong on any single one.

**Flag:**
- Stateful domain services (domain services should be stateless)
- Domain service methods contain infrastructure calls (DB, HTTP, file)
- Logic that belongs on an entity is placed in a domain service instead

**Review comment:**
> [Domain Service] This service contains infrastructure calls. Domain services must be pure domain logic — move I/O to Application or Infrastructure.

---

### 6. Domain Events

Use domain events to represent something that happened in the domain, enabling side effects without coupling.

**Flag:**
- Side effects after a domain action are implemented as direct calls in Application handlers (e.g., send notification, update audit) instead of via events
- Domain events contain infrastructure types or DTOs as payload

**Review comment:**
> [Domain Event] Side effect is directly called in the handler. Consider raising a domain event from the entity so side effects remain decoupled.

---

### 7. Architecture Boundaries — No Leakage Into Domain

The Domain layer must have **zero dependencies** on:
- Sibling module projects
- `JustGoAPI.Shared`
- Infrastructure concerns (EF attributes beyond basic `[Key]`, HTTP, caching, logging)
- Application DTOs or MediatR types

**Flag:**
- `using` statements referencing sibling module namespaces
- `using JustGoAPI.Shared.*` in Domain projects
- `[Column]`, `[ForeignKey]`, or other ORM attributes driving domain design
- `IMediator`, `ILogger`, or `IHttpContextAccessor` injected into entities or domain services

**Review comment:**
> [Architecture Boundary] Domain project references a sibling module / shared infrastructure concern. Domain must be dependency-free. Remove the reference and represent the concept natively or via a local value object/ID.

---

### 8. Naming Conventions

| Type | Convention | Example |
|------|-----------|---------|
| Entity | Noun, domain language | `Booking`, `ClubMembership` |
| Value Object | Noun describing the concept | `Money`, `DateRange`, `Address` |
| Domain Service | `[Noun]Service` or `[Action]DomainService` | `MembershipEligibilityService` |
| Domain Event | Past-tense noun phrase | `BookingCancelledEvent`, `MemberEnrolledEvent` |
| Aggregate Root | Same as Entity | `Booking` (root of `BookingLineItem`) |

**Flag:**
- Domain events named in present tense (`BookingCancelling`)
- Domain services named as application verbs (`ProcessBookingService`)
- Entities suffixed with `DTO`, `Model`, `Entity` in Domain layer

---

## Severity Tags

| Tag | Meaning |
|-----|---------|
| `[Standard]` | Violates Clean Architecture / DDD practice |
| `[Repo Convention]` | Inconsistent with agreed project naming/style |
| `[Risk]` | Works now but creates maintainability or consistency risk |
| `[Legacy Tolerance]` | Pattern exists in the repo; new code must not expand it |

---

## Common Review Smells

Flag these:

- All entity properties have `public set;` — anemic model
- Business rule checked only in Application validator, not enforced by entity constructor or method
- Primitive obsession: email, money, date range not extracted as value objects
- Entity can be put in invalid state from outside
- Repository returns non-aggregate-root child entities directly
- Domain service has constructor-injected infrastructure services
- Domain event carries a DTO or Application type as payload
- `using` referencing sibling module or `JustGoAPI.Shared` from Domain project
- EF/ORM annotations driving entity design choices (infrastructure leaking into domain)
- Side effects triggered by direct handler calls instead of domain events

---

## Pragmatic Approach

> Don't force a full rich-domain rewrite.

When reviewing existing code, apply this scale:

1. **New code** → must follow these standards
2. **Modified code** → pull it one step closer to the standard
3. **Untouched code** → flag as `[Legacy Tolerance]` if it violates a standard, but don't require a rewrite unless it's a security or data integrity risk
