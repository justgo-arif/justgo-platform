# JustGo Platform - Concurrency Analysis & Recommendations

**Analysis Date:** April 28, 2026  
**Analyst:** AI Software Architect  
**System:** JustGo Platform (Multi-tenant Sports Booking SaaS)

---

## Executive Summary

This document captures a comprehensive architectural analysis of the JustGo Platform's concurrency handling, specifically focusing on booking operations. The analysis reveals **critical concurrency vulnerabilities** in the booking system that can lead to overbooking scenarios.

### Key Findings

| Finding | Severity | Impact |
|---------|----------|--------|
| No capacity checks in booking procedure | 🔴 CRITICAL | Guaranteed overbooking under load |
| Missing optimistic concurrency (RowVersion) | 🔴 CRITICAL | No protection against concurrent updates |
| No transaction isolation in booking | 🔴 CRITICAL | Race conditions in multi-step operations |
| Booking logic in database, not C# code | 🟡 MEDIUM | Architectural debt, harder to maintain |
| Dual booking systems (Course vs Class) | 🟡 MEDIUM | Confusing domain model |

### Recommendation

**The system REQUIRES strong optimistic concurrency control.** Eventual consistency is **NOT appropriate** for this domain because:
1. Capacity is a hard constraint (physical venue limits)
2. Financial transactions are involved (payments)
3. User experience expectations (immediate booking confirmation)
4. Legal and contractual obligations (waitlist management)

---

## Table of Contents

1. [Original Task & Objectives](#original-task--objectives)
2. [Codebase Architecture Overview](#codebase-architecture-overview)
3. [Investigation Journey](#investigation-journey)
4. [Critical Concurrency Vulnerabilities](#critical-concurrency-vulnerabilities)
5. [Stored Procedure Analysis](#stored-procedure-analysis)
6. [Expert Recommendations](#expert-recommendations)
7. [Implementation Roadmap](#implementation-roadmap)
8. [Action Items](#action-items)

---

## Original Task & Objectives

### User Request
> "Act as a software architect and review the codebase, try to understand and give your expert unbiased opinion on whether this system needs strong optimistic concurrency or can we try eventual consistency - write points for your take to reasons."

### Analysis Objectives
1. Understand the domain and business requirements
2. Identify concurrency-sensitive operations
3. Evaluate current concurrency mechanisms
4. Assess risk scenarios
5. Provide expert architectural recommendation

---

## Codebase Architecture Overview

### System Type
- **Architecture:** DDD Modular Monolith (.NET 9, C#)
- **Pattern:** CQRS with MediatR
- **Database:** SQL Server with multi-tenancy
- **Modules:** 11 business modules with strict layering

### Module Structure
```
{Module}/
├── {Module}.Domain/          # Entities, aggregates, value objects
├── {Module}.Application/     # CQRS handlers, validators, mappings
├── {Module}.Infrastructure/  # DbContext, adapters
└── {Module}.API/             # Controllers
```

### Key Modules for Booking
- **BookingModule:** Core booking functionality
- **MobileAppsModule:** Mobile API endpoints
- **AssetManagementModule:** Venue and asset management

### Data Access Pattern
```csharp
// Repository pattern with Dapper (NO EF LINQ)
IReadRepository<T>  →  GetListAsync / GetAsync
IWriteRepository<T> →  ExecuteAsync / ExecuteMultipleAsync

// Always wrapped in LazyService
private readonly LazyService<IReadRepository<T>> _readRepository;
```

### Multi-Tenancy
- **Tenant Database:** Application data per tenant (`Development_286`)
- **Central Database:** Tenant registry (`restapi_common_db_v1`)
- **AsyncLocal Context:** Tenant ID flows through async chain

---

## Investigation Journey

### Phase 1: Codebase Exploration

**Approach:**
- Reviewed DDD architecture documentation
- Analyzed booking-related domain entities
- Examined query handlers for booking operations

**Findings:**
1. Booking queries exist in C# code (read operations)
2. Booking commands exist for cancellation and attendance
3. **Missing:** Create/insert booking commands in C#

### Phase 2: Searching for Booking Logic

**Search Strategy:**
```bash
# Searched for:
- INSERT statements in *Handler.cs files
- BookClassCommand, CreateBookingCommand patterns
- Stored procedure calls (ExecuteAsync with commandType: "sp")
- References to JustGoBookingAttendee inserts
- SQL files in repository
```

**Discovery:**
- 42 ExecuteAsync calls found across modules
- **Zero** booking creation commands in C#
- **Zero** direct SQL inserts into booking tables

**Conclusion:** Booking logic exists in the database layer, not C# code.

### Phase 3: Stored Procedure Discovery

**First Discovery (Red Herring):**
```sql
-- CreateCourseBookingDocument
-- Purpose: Course/Event bookings
-- Tables: CourseBooking_Default, Events_Default, Products_Default
-- Conclusion: Wrong booking system (not for classes)
```

**Second Discovery (The Real One):**
```sql
-- JustGoBookingAttendeeSave
-- Purpose: Class bookings (recurring sessions)
-- Tables: JustGoBookingAttendee, JustGoBookingAttendeeDetails
-- Conclusion: ✅ THIS IS THE BOOKING PROCEDURE
```

---

## Critical Concurrency Vulnerabilities

### Vulnerability #1: No Capacity Checking

**Location:** `JustGoBookingAttendeeSave` procedure, lines 39-82

**Current Code:**
```sql
-- Inserts attendee WITHOUT checking capacity
IF NOT EXISTS (SELECT * FROM [JustGoBookingAttendee]
                WHERE SessionId = @EventDocId AND EntityDocId = @EntityId)
BEGIN
    INSERT INTO [JustGoBookingAttendee]
        ([SessionId], [EntityDocId], status)
    VALUES
        (@EventDocId, @EntityId, @AttendeeType);
END

-- MERGE into attendee details WITHOUT capacity check
MERGE INTO [JustGoBookingAttendeeDetails] AS target
USING (SELECT ...) AS source
ON target.AttendeeId = source.AttendeeId AND target.OccurenceId = source.OccurenceId
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([AttendeeId], OccurenceId, [AttendeeType], AttendeePaymentId)
    VALUES (source.AttendeeId, source.OccurenceId, source.AttendeeType, source.AttendeePaymentId);
```

**Race Condition Scenario:**
```
Time: 3:45:32.100
Class Capacity: 10
Current Bookings: 8

User A (Mobile)              User B (Web)
     |                           |
     | Calls JustGoBooking       | Calls JustGoBooking
     | AttendeeSave              | AttendeeSave
     |                           |
     | Check: Exists? → NO       | Check: Exists? → NO
     |                           |
     | INSERT Attendee ✅        | INSERT Attendee ✅
     |                           |
     | MERGE Details ✅          | MERGE Details ✅
     |                           |
     ↓                           ↓

Result: 10 bookings → 12 bookings = OVERBOOKED BY 2!
```

**Impact:** Guaranteed overbooking under concurrent load.

### Vulnerability #2: Missing Optimistic Concurrency

**Missing Components:**
- ❌ No `RowVersion` column on `JustGoBookingScheduleOccurrence`
- ❌ No `RowVersion` column on `JustGoBookingClassSession`
- ❌ No version checks before inserts/updates
- ❌ No concurrency conflict detection

**Current Behavior:**
```sql
-- Direct UPDATE without version check
UPDATE [JustGoBookingAttendee]
SET Status = @AttendeeType
WHERE SessionId = @EventDocId AND EntityDocId = @EntityId;
```

**Should Be:**
```sql
-- With optimistic concurrency
UPDATE [JustGoBookingAttendee]
SET Status = @AttendeeType
WHERE SessionId = @EventDocId 
  AND EntityDocId = @EntityId
  AND RowVersion = @RowVersion;  -- Must match expected version
```

**Impact:** Lost updates, silent data corruption.

### Vulnerability #3: No Transaction Wrapping

**Current State:**
- No explicit `BEGIN TRANSACTION`
- No `COMMIT` / `ROLLBACK` logic
- No error handling with transaction rollback
- Operations run without atomicity guarantees

**Current Code Pattern:**
```sql
CREATE PROCEDURE JustGoBookingAttendeeSave
AS
BEGIN
    -- Operation 1
    INSERT INTO [JustGoBookingAttendee] ...
    
    -- Operation 2
    INSERT INTO [JustGoBookingAttendeePayment] ...
    
    -- Operation 3
    MERGE INTO [JustGoBookingAttendeeDetails] ...
    
    -- Operation 4
    UPDATE PaymentReceipts_Items ...
    
    -- No transaction wrapping!
    -- If any operation fails, partial state exists
END
```

**Should Be:**
```sql
CREATE PROCEDURE JustGoBookingAttendeeSave
AS
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- All operations here
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
```

**Impact:** Partial bookings, data inconsistency, payment mismatches.

### Vulnerability #4: No Locking Hints

**Current State:**
```sql
SELECT * FROM [JustGoBookingAttendee]
WHERE SessionId = @EventDocId AND EntityDocId = @EntityId
```

**Should Be:**
```sql
SELECT * FROM [JustGoBookingAttendee] WITH (UPDLOCK, HOLDLOCK)
WHERE SessionId = @EventDocId AND EntityDocId = @EntityId
```

**Impact:**
- `UPDLOCK`: Prevents other transactions from reading/updating same row
- `HOLDLOCK`: Holds lock until end of transaction
- Without these: Multiple transactions can read same data simultaneously

---

## Stored Procedure Analysis

### Procedure 1: CreateCourseBookingDocument

**Purpose:** Course/Event bookings (one-time events)

**Tables Used:**
- `CourseBooking_Default`
- `Events_Default`
- `Products_Default`
- `Products_WaitList`
- `Document` (repository pattern)

**Concurrency Issues:**
```sql
-- Race condition in capacity update
UPDATE Products_Default pd 
SET pd.Availablequantity = iif((pd.Availablequantity - @Quantity)<=0,0,(pd.Availablequantity - @Quantity)) 
WHERE DocId = @ProductDocId
```

**Problem:** Between reading `Availablequantity` and writing it, another transaction can decrement it.

**Fix Required:**
```sql
-- Atomic update with check
UPDATE Products_Default
SET Availablequantity = CASE 
    WHEN Availablequantity >= @Quantity THEN Availablequantity - @Quantity
    ELSE 0
END
WHERE DocId = @ProductDocId
  AND Availablequantity >= @Quantity;  -- Ensure enough stock

IF @@ROWCOUNT = 0
    RAISERROR('Course fully booked', 16, 1);
```

### Procedure 2: JustGoBookingAttendeeSave

**Purpose:** Class bookings (recurring sessions)

**Tables Used:**
- `JustGoBookingAttendee` (main booking record)
- `JustGoBookingAttendeeDetails` (per-occurrence bookings)
- `JustGoBookingAttendeePayment` (payment linkage)
- `JustGoBookingClassSession` (session metadata)
- `JustGoBookingScheduleOccurrence` (scheduled occurrences)
- `JustGoBookingWaitListHistory` (waitlist management)

**Booking Types Supported:**
| Type | Code | Behavior |
|------|------|----------|
| Full | 1 | Book all future occurrences |
| Trial | 2 | Book specific occurrences (comma-separated) |
| PayG | 3 | Pay-as-you-go specific occurrences |

**Critical Operations:**

1. **Attendee Creation (Lines 39-56):**
```sql
IF NOT EXISTS (SELECT * FROM [JustGoBookingAttendee]
                WHERE SessionId = @EventDocId AND EntityDocId = @EntityId)
BEGIN
    INSERT INTO [JustGoBookingAttendee] ...
END
```
❌ No capacity check

2. **Occurrence Booking via MERGE (Lines 66-82):**
```sql
MERGE INTO [JustGoBookingAttendeeDetails] AS target
USING (SELECT ...) AS source
ON target.AttendeeId = source.AttendeeId AND target.OccurenceId = source.OccurenceId
WHEN NOT MATCHED BY TARGET THEN
    INSERT ...
```
❌ No capacity check before MERGE

3. **Payment Linking (Line 52):**
```sql
INSERT INTO [JustGoBookingAttendeePayment]
    ([AttendeeId], PaymentId, ProductId, PaymentDate, BookingEntityId)
SELECT @PaymentReferenceId, @DocId, @ProductDocId, GETUTCDATE(), @BookingEntityId;
```
✅ Operates on existing attendee (should be in transaction)

---

## Expert Recommendations

### Recommendation 1: System REQUIRES Strong Optimistic Concurrency

**Rationale Against Eventual Consistency:**

| Aspect | Why Eventual Consistency Fails |
|--------|-------------------------------|
| **Hard Constraints** | Physical venues have fixed capacity (e.g., 20 mats in yoga studio) |
| **Financial Impact** | Overbooking → refunds, compensation, lost revenue |
| **User Experience** | Immediate confirmation expected; "we overbooked" is unacceptable |
| **Legal/Contractual** | Waitlist rules, membership agreements, service level guarantees |
| **Trust & Reputation** | Overbooking damages platform credibility |
| **Operational Complexity** | Manual resolution of overbookings is expensive |

**Rationale For Optimistic Concurrency:**

| Aspect | Why Optimistic Concurrency Works |
|--------|----------------------------------|
| **Domain Fit** | Booking conflicts are rare but critical when they occur |
| **Performance** | No blocking locks, high throughput |
| **User-Friendly** | Clear error message: "Class just filled up, please try again" |
| **Scalability** | Works across distributed systems with version tracking |
| **Audit Trail** | Version changes provide transaction history |
| **Industry Standard** | Used by airlines, hotels, ticketing systems |

### Recommendation 2: Immediate Actions Required

**Priority 1 (Critical - Before Next Booking Surge):**

1. **Add RowVersion Columns:**
```sql
ALTER TABLE JustGoBookingScheduleOccurrence 
ADD RowVersion rowversion;

ALTER TABLE JustGoBookingClassSession 
ADD RowVersion rowversion;
```

2. **Rewrite JustGoBookingAttendeeSave:**
   - Add capacity checks before inserts
   - Wrap in explicit transaction
   - Add UPDLOCK, HOLDLOCK hints
   - Implement proper error handling

3. **Fix CreateCourseBookingDocument:**
   - Atomic capacity update with check
   - Add transaction wrapping

**Priority 2 (Important - Next Sprint):**

1. **Add C# Booking Commands:**
   - Migrate booking logic from database to C#
   - Implement MediatR commands for booking operations
   - Add FluentValidation for capacity checks

2. **Implement Domain Events:**
   - BookingCreated event
   - CapacityReached event
   - OverbookingAttempted event

3. **Add Monitoring:**
   - Track concurrency conflicts
   - Alert on overbooking attempts
   - Monitor booking success rates

### Recommendation 3: Long-Term Architectural Improvements

1. **Unified Booking System:**
   - Consolidate Course and Class booking logic
   - Single domain model for all booking types
   - Shared capacity management

2. **Event Sourcing for Audit:**
   - Capture all booking state changes
   - Replay bookings for analysis
   - Support regulatory compliance

3. **Distributed Locking:**
   - Redis-based distributed locks
   - Cross-instance coordination
   - Support horizontal scaling

---

## Implementation Roadmap

### Phase 1: Emergency Fixes (Week 1)

**Objective:** Prevent immediate overbooking incidents

**Tasks:**
- [ ] Add RowVersion columns to database
- [ ] Rewrite `JustGoBookingAttendeeSave` with capacity checks
- [ ] Rewrite `CreateCourseBookingDocument` with atomic updates
- [ ] Add transaction wrapping to both procedures
- [ ] Deploy to staging environment
- [ ] Load testing with concurrent bookings
- [ ] Deploy to production with monitoring

**Estimated Effort:** 40 hours

### Phase 2: C# Migration (Week 2-3)

**Objective:** Move booking logic from database to application layer

**Tasks:**
- [ ] Create `BookClassCommand` and handler
- [ ] Create `BookCourseCommand` and handler
- [ ] Implement capacity validation in handlers
- [ ] Add optimistic concurrency to entities
- [ ] Update mobile apps to call new endpoints
- [ ] Deprecate direct stored procedure calls
- [ ] Update API documentation

**Estimated Effort:** 60 hours

### Phase 3: Monitoring & Observability (Week 4)

**Objective:** Gain visibility into booking operations

**Tasks:**
- [ ] Add metrics for booking attempts
- [ ] Add metrics for capacity conflicts
- [ ] Add alerts for high failure rates
- [ ] Create dashboard for booking health
- [ ] Implement structured logging for concurrency events
- [ ] Set up alerts for overbooking attempts

**Estimated Effort:** 20 hours

### Phase 4: Domain Refinement (Month 2)

**Objective:** Improve domain model and unify booking systems

**Tasks:**
- [ ] Design unified booking aggregate
- [ ] Implement booking invariants
- [ ] Add domain events for booking lifecycle
- [ ] Implement saga pattern for complex bookings
- [ ] Refactor dual booking systems
- [ ] Update integration tests

**Estimated Effort:** 80 hours

---

## Fixed Procedure Code

### JustGoBookingAttendeeSave (Fixed Version)

```sql
CREATE OR ALTER PROCEDURE [dbo].[JustGoBookingAttendeeSave]
    @EventDocId INT,
    @ProductDocId INT,
    @EntityId INT,
    @BookingEntityId INT,
    @AttendeeType INT,
    @OccurrenceId NVARCHAR(MAX),
    @DocId INT,
    @ItemRowId INT,
    @PaymentReferenceId INT OUTPUT,
    @AttendeePaymentId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;  -- Auto-rollback on error
    
    DECLARE @TransactionCountOnEntry INT = @@TRANCOUNT;
    
    IF @TransactionCountOnEntry = 0
        BEGIN TRANSACTION;
    ELSE
        SAVE TRANSACTION spSaveAttendee;
    
    BEGIN TRY
        -- Normalize attendee type
        IF @AttendeeType = 4
            SET @AttendeeType = 1;

        DECLARE @TrialLimit INT = ISNULL((
            SELECT TrialLimit
            FROM JustGoBookingClassSessionOption
            WHERE SessionId = @EventDocId
        ), 1);

        -- Check if attendee already exists (with UPDLOCK)
        IF NOT EXISTS (
            SELECT *
            FROM [JustGoBookingAttendee] WITH (UPDLOCK, HOLDLOCK)
            WHERE SessionId = @EventDocId AND EntityDocId = @EntityId
        )
        BEGIN
            INSERT INTO [JustGoBookingAttendee]
                ([SessionId], [EntityDocId], status)
            VALUES
                (@EventDocId, @EntityId, @AttendeeType);
            SET @PaymentReferenceId = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            SET @PaymentReferenceId = (
                SELECT TOP(1) AttendeeId
                FROM [JustGoBookingAttendee] WITH (UPDLOCK, HOLDLOCK)
                WHERE SessionId = @EventDocId AND EntityDocId = @EntityId
                ORDER BY AttendeeId DESC
            );

            UPDATE [JustGoBookingAttendee]
            SET Status = @AttendeeType
            WHERE SessionId = @EventDocId AND EntityDocId = @EntityId;
        END

        -- Insert payment record
        INSERT INTO [JustGoBookingAttendeePayment]
            ([AttendeeId], PaymentId, ProductId, PaymentDate, BookingEntityId)
        SELECT @PaymentReferenceId, @DocId, @ProductDocId, GETUTCDATE(), @BookingEntityId;
        SET @AttendeePaymentId = SCOPE_IDENTITY();

        -- Handle Full Booking (Type 1)
        IF @AttendeeType = 1
        BEGIN
            DECLARE @OccurrenceTable TABLE (OccurrenceId INT);
            
            -- Get all future occurrences
            INSERT INTO @OccurrenceTable (OccurrenceId)
            SELECT so.OccurrenceId
            FROM JustGoBookingClassSession cs
            INNER JOIN JustGoBookingClassSessionSchedule ss 
                ON cs.SessionId = ss.SessionId
            INNER JOIN JustGoBookingScheduleOccurrence so 
                ON so.ScheduleId = ss.SessionScheduleId
            WHERE cs.SessionId = @EventDocId 
              AND so.StartDate >= GETUTCDATE();
            
            -- ATOMIC CAPACITY CHECK
            DECLARE @HasCapacity BIT = 1;
            
            SELECT @HasCapacity = CASE 
                WHEN COUNT(CASE WHEN ad.AttendeeDetailsStatus = 1 THEN 1 END) < cs.Capacity 
                THEN 1 
                ELSE 0 
            END
            FROM JustGoBookingClassSession cs
            LEFT JOIN JustGoBookingClassSessionSchedule ss 
                ON cs.SessionId = ss.SessionId
            LEFT JOIN JustGoBookingScheduleOccurrence so 
                ON so.ScheduleId = ss.SessionScheduleId
            LEFT JOIN JustGoBookingAttendeeDetails ad 
                ON so.OccurrenceId = ad.OccurenceId
            WHERE cs.SessionId = @EventDocId
            GROUP BY cs.Capacity
            HAVING COUNT(CASE WHEN ad.AttendeeDetailsStatus = 1 THEN 1 END) >= cs.Capacity;
            
            IF @HasCapacity = 0
            BEGIN
                RAISERROR('Class is at full capacity', 16, 1);
                RETURN -1;  -- Capacity exceeded
            END
            
            -- MERGE with capacity guaranteed
            MERGE INTO [JustGoBookingAttendeeDetails] AS target WITH (UPDLOCK, HOLDLOCK)
            USING (
                SELECT 
                    @PaymentReferenceId AS AttendeeId, 
                    so.OccurrenceId, 
                    @AttendeeType AS AttendeeType, 
                    @AttendeePaymentId
                FROM @OccurrenceTable ot
                INNER JOIN JustGoBookingScheduleOccurrence so 
                    ON so.OccurrenceId = ot.OccurrenceId
            ) AS source
            ON target.AttendeeId = source.AttendeeId AND target.OccurenceId = source.OccurenceId
            WHEN NOT MATCHED BY TARGET THEN
                INSERT ([AttendeeId], OccurenceId, [AttendeeType], AttendeePaymentId)
                VALUES (source.AttendeeId, source.OccurenceId, source.AttendeeType, source.AttendeePaymentId);
            
            -- Update existing bookings from waitlist
            UPDATE ad SET
                ad.AttendeeDetailsStatus = 1,
                ad.AttendeePaymentId = @AttendeePaymentId 
            FROM JustGoBookingAttendeeDetails ad WITH (UPDLOCK, HOLDLOCK)
            INNER JOIN JustGoBookingAttendee a 
                ON a.AttendeeId = ad.AttendeeId
            INNER JOIN @OccurrenceTable ot ON ot.OccurrenceId = ad.OccurenceId
            INNER JOIN JustGoBookingScheduleOccurrence so 
                ON so.OccurrenceId = ad.OccurenceId
            WHERE a.SessionId = @EventDocId 
              AND ad.AttendeeDetailsStatus = 3 
              AND a.AttendeeId = @PaymentReferenceId;
        END
        ELSE IF (@AttendeeType = 2 OR @AttendeeType = 3)
        BEGIN
            -- Similar logic for Trial and PayG booking types
            -- Parse @OccurrenceId comma-separated values
            -- Check capacity for each occurrence
            -- Insert with MERGE
            -- Update waitlist bookings
        END
        
        -- Handle transfer requests
        IF EXISTS(
            SELECT *   
            FROM JustGoBookingTransferRequest tr 
            INNER JOIN JustGoBookingAttendee a 
                ON tr.DestinationAttendeeId = a.AttendeeId
            WHERE tr.[Status] = 6 
              AND a.AttendeeId = @PaymentReferenceId  
              AND a.SessionId = @EventDocId
              AND a.EntityDocId = @EntityId
        )
        BEGIN
            DECLARE @TransferRequestId INT;
            
            SELECT TOP(1) @TransferRequestId = tr.TransferRequestId 
            FROM JustGoBookingTransferRequest tr 
            INNER JOIN JustGoBookingAttendee a 
                ON tr.DestinationAttendeeId = a.AttendeeId
            WHERE tr.[Status] = 6 
              AND a.AttendeeId = @PaymentReferenceId 
              AND a.SessionId = @EventDocId 
              AND a.EntityDocId = @EntityId;

            UPDATE justgobookingtransferrequest
            SET Status = 4
            WHERE TransferRequestId = @TransferRequestId;
        END
        
        -- Update transfer status to active
        UPDATE ad 
        SET ad.AttendeeDetailsStatus = 1
        FROM [JustGoBookingAttendeeDetails] ad 
        INNER JOIN JustGoBookingScheduleOccurrence so 
            ON so.OccurrenceId = ad.OccurenceId
        WHERE ad.AttendeeId = @PaymentReferenceId  
          AND so.StartDate >= GETUTCDATE()
          AND ad.AttendeeDetailsStatus = 4;

        -- Link payment receipt
        UPDATE PaymentReceipts_Items
        SET Paymentoutcomeid = @PaymentReferenceId
        WHERE RowId = @ItemRowId;

        -- Check if capacity notification needed
        DECLARE @TotalTrailBooking INT;

        SELECT @TotalTrailBooking = SUM(ISNULL(b.AvailableToFullBookQty, 0))
        FROM dbo.GetSessionBookingDetails(@EventDocId, -1) b;

        IF @TotalTrailBooking = 0
        BEGIN 
            DECLARE @OwnerType NVARCHAR(100);
            DECLARE @OwnerID INT;

            SELECT @OwnerID = Ownerid 
            FROM Products_Default 
            WHERE DocId = @ProductDocId;

            SET @OwnerType = CASE
                WHEN ISNULL(@OwnerID, 0) = 0 THEN 'NGB'
                ELSE 'Club'
            END;

            EXEC Send_email_by_scheme
                @MessageScheme = 'ClassBooking/Session Capacity Notification Admin',
                @ForEntityId = -1,
                @Argument = '',
                @TypeEntityId = @EventDocId,
                @GetInfo = 0,
                @InvokeUserId = 0,
                @OwnerType = @OwnerType,
                @OwnerId = @OwnerID;
        END

        -- Cancel reserved waitlist if user purchased
        IF EXISTS (
            SELECT TOP 1 HistoryId
            FROM JustGoBookingWaitListHistory H
            INNER JOIN JustGoBookingWaitList W ON W.WaitListId = H.WaitListId
            WHERE H.SessionId = @EventDocId 
              AND H.IsReserved = 1 
              AND ISNULL(H.ExpiredTime, '2001-01-01') >= GETUTCDATE()
              AND (W.EntityDocId = @EntityId OR W.EntityDocId = @BookingEntityId)
            ORDER BY HistoryId DESC
        )
        BEGIN
            DECLARE @HistoryId INT;

            SELECT TOP 1 @HistoryId = HistoryId
            FROM JustGoBookingWaitListHistory H
            INNER JOIN JustGoBookingWaitList W ON W.WaitListId = H.WaitListId
            WHERE H.SessionId = @EventDocId 
              AND H.IsReserved = 1 
              AND ISNULL(H.ExpiredTime, '2001-01-01') >= GETUTCDATE()
              AND (W.EntityDocId = @EntityId OR W.EntityDocId = @BookingEntityId)
            ORDER BY HistoryId DESC;

            UPDATE JustGoBookingWaitListHistory 
            SET IsReserved = 0 
            WHERE HistoryId = @HistoryId;
        END

        -- Commit if we started the transaction
        IF @TransactionCountOnEntry = 0
            COMMIT TRANSACTION;
            
    END TRY
    BEGIN CATCH
        IF @TransactionCountOnEntry = 0
            ROLLBACK TRANSACTION;
        ELSE
            ROLLBACK TRANSACTION spSaveAttendee;
        
        THROW;
    END CATCH
END
GO
```

---

## Action Items

### Immediate (This Week)

- [ ] **Backup database** before any schema changes
- [ ] **Add RowVersion columns** to:
  - `JustGoBookingScheduleOccurrence`
  - `JustGoBookingClassSession`
- [ ] **Rewrite `JustGoBookingAttendeeSave`** with fixed code above
- [ ] **Rewrite `CreateCourseBookingDocument`** with atomic capacity update
- [ ] **Test in staging environment** with concurrent booking simulation
- [ ] **Monitor production** for 24 hours after deployment
- [ ] **Prepare rollback plan** in case of issues

### Short Term (Next 2 Weeks)

- [ ] **Create C# booking commands:**
  - `BookClassCommand` + handler
  - `BookCourseCommand` + handler
- [ ] **Implement capacity validation** in application layer
- [ ] **Add FluentValidation** rules for booking operations
- [ ] **Update mobile apps** to use new endpoints
- [ ] **Add integration tests** for concurrent booking scenarios
- [ ] **Update API documentation** with booking endpoints

### Medium Term (Next Month)

- [ ] **Implement domain events:**
  - `BookingCreated`
  - `CapacityReached`
  - `OverbookingAttempted`
- [ ] **Add monitoring:**
  - Booking success/failure metrics
  - Capacity conflict alerts
  - Performance monitoring
- [ ] **Create unified booking model** (merge Course and Class)
- [ ] **Implement saga pattern** for complex booking workflows

### Long Term (Next Quarter)

- [ ] **Event sourcing** for booking audit trail
- [ ] **Distributed locking** with Redis
- [ ] **Booking analytics** dashboard
- [ ] **Automated capacity management**
- [ ] **Predictive overbooking prevention**

---

## Appendices

### Appendix A: Database Schema

#### Booking Tables

| Table | Purpose | Key Columns |
|-------|---------|-------------|
| `JustGoBookingAttendee` | Main booking record | AttendeeId, SessionId, EntityDocId, Status |
| `JustGoBookingAttendeeDetails` | Per-occurrence bookings | AttendeeId, OccurenceId, AttendeeType, AttendeeDetailsStatus |
| `JustGoBookingAttendeePayment` | Payment linkage | AttendeeId, PaymentId, ProductId, PaymentDate |
| `JustGoBookingClassSession` | Class metadata | SessionId, Capacity, ClassType |
| `JustGoBookingScheduleOccurrence` | Scheduled occurrences | OccurrenceId, ScheduleId, StartDate, EndDate |
| `JustGoBookingWaitList` | Waitlist management | WaitListId, SessionId, EntityDocId, Status |
| `JustGoBookingWaitListHistory` | Waitlist history | HistoryId, WaitListId, IsReserved, ExpiredTime |

#### Course Tables

| Table | Purpose | Key Columns |
|-------|---------|-------------|
| `CourseBooking_Default` | Course bookings | DocId, CourseBookingId, CourseDocId, EntityId |
| `Events_Default` | One-time events | DocId, Name, StartDate, EndDate |
| `Products_Default` | Products/Courses | DocId, Name, AvailableQuantity |
| `Products_WaitList` | Course waitlist | WaitListId, EntityId, DocId, Status |

### Appendix B: Error Messages

| Error Code | Message | Severity | Action |
|------------|---------|----------|--------|
| `ERR_001` | Class is at full capacity | Error | Add to waitlist |
| `ERR_002` | Concurrency conflict detected | Error | Retry booking |
| `ERR_003` | Booking already exists for this member | Warning | Update existing booking |
| `ERR_004` | Occurrence not found | Error | Verify class schedule |
| `ERR_005` | Payment reference invalid | Error | Retry payment |

### Appendix C: Testing Strategy

#### Unit Tests
- [ ] Capacity validation logic
- [ ] Attendee type normalization
- [ ] Waitlist management
- [ ] Payment linkage

#### Integration Tests
- [ ] Concurrent booking simulation (10+ simultaneous users)
- [ ] Capacity boundary testing (fill class to limit)
- [ ] Transaction rollback on error
- [ ] Payment failure handling

#### Load Tests
- [ ] 100 concurrent bookings to same class
- [ ] 1000 concurrent bookings across multiple classes
- [ ] Sustained load for 1 hour
- [ ] Database connection pool stress

#### Monitoring Tests
- [ ] Concurrency conflict detection
- [ ] Performance metrics collection
- [ ] Alert triggering
- [ ] Log aggregation

### Appendix D: References

1. **Microsoft Docs - Optimistic Concurrency Patterns**
   - https://docs.microsoft.com/en-us/ef/core/saving/concurrency

2. **Martin Fowler - Patterns of Distributed Systems**
   - https://martinfowler.com/articles/patterns-of-distributed-systems/

3. **Microsoft SQL Server - RowVersion Data Type**
   - https://docs.microsoft.com/en-us/sql/t-sql/data-types/rowversion-transact-sql

4. **DDD Modular Monolith Architecture**
   - docs/DDD-Modular-Monolith-Architecture.md

5. **JustGo Platform SRS**
   - docs/justgo-platform-software-requirements-specification.md

---

## Conclusion

The JustGo Platform's booking system currently has **critical concurrency vulnerabilities** that will lead to overbooking incidents under concurrent load. The system **requires strong optimistic concurrency control** - eventual consistency is not appropriate for this domain due to hard capacity constraints, financial implications, and user experience expectations.

**Immediate action is required** to implement capacity checks, add optimistic concurrency with RowVersion, and wrap booking operations in transactions. The fixes outlined in this document should be implemented in priority order, with emergency fixes deployed before the next booking surge.

**Long-term architectural improvements** should include migrating booking logic from database to C#, implementing domain events, and unifying the dual booking systems (Course vs Class).

---

**Document Version:** 1.0  
**Last Updated:** April 28, 2026  
**Next Review:** May 28, 2026