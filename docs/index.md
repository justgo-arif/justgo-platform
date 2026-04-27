---
title: JustGo Platform SRS Index
tags:
  - justgo
  - srs
  - functional-requirements
---

# JustGo Platform SRS Index

Source: [[justgo-platform-software-requirements-specification#3. System Features and Functional Requirements|System Features and Functional Requirements]]

## Functional Requirements Summary

The SRS defines 502 functional requirements across 11 feature areas. The platform covers tenant-aware identity and authorization, member profiles, organisations, memberships, bookings, assets, credentials, extensible fields, finance, mobile workflows, and sports results.

| Feature Area | FR Range | Concise Scope |
| --- | --- | --- |
| [[justgo-platform-software-requirements-specification#3.1. User Management and Authentication|User Management and Authentication]] | FR-01 to FR-80 | Login, accounts, authorization, UI permissions, MFA, tenants, users, files, notes, lookup data, cache invalidation, and system settings. |
| [[justgo-platform-software-requirements-specification#3.2. Member Profile Management|Member Profile Management]] | FR-81 to FR-120 | Member identity details, addresses, family relationships, notes, preferences, emergency contacts, and member search/retrieval. |
| [[justgo-platform-software-requirements-specification#3.3. Organisation and Club Management|Organisation and Club Management]] | FR-121 to FR-129 | Organisation records, club-level structures, and administrative organisation lifecycle workflows. |
| [[justgo-platform-software-requirements-specification#3.4. Membership Management|Membership Management]] | FR-130 to FR-142 | Membership records, membership purchase flows, validation, status changes, and retrieval. |
| [[justgo-platform-software-requirements-specification#3.5. Booking Management|Booking Management]] | FR-143 to FR-175 | Booking catalog, classes, terms, pricing discounts, transfers, profile class bookings, and course bookings. |
| [[justgo-platform-software-requirements-specification#3.6. Asset Management|Asset Management]] | FR-176 to FR-250 | Asset registers, categories, types, credentials, leases, licenses, metadata, ownership transfers, checkout, audit, reports, clubs, and workflows. |
| [[justgo-platform-software-requirements-specification#3.7. Credential Management|Credential Management]] | FR-251 to FR-252 | Credential creation, access, and management for authorized workflows. |
| [[justgo-platform-software-requirements-specification#3.8. Field and Preference Management|Field and Preference Management]] | FR-253 to FR-270 | Entity extension fields, configurable metadata, preferences, lookup behavior, and validation. |
| [[justgo-platform-software-requirements-specification#3.9. Finance and Payment Management|Finance and Payment Management]] | FR-271 to FR-335 | Balances, finance grids, installments, payment accounts, payment console, payments, products, and subscriptions. |
| [[justgo-platform-software-requirements-specification#3.10. Mobile Application Services|Mobile Application Services]] | FR-336 to FR-435 | Mobile classes, clubs, events, attendance-oriented operations, general settings, MFA, and two-factor authentication. |
| [[justgo-platform-software-requirements-specification#3.11. Results and Competition Management|Results and Competition Management]] | FR-436 to FR-502 | Events, result validation, result status changes, sports results, uploads, rankings, competition data, and result-file lifecycle. |

## Feature Breakdown

### [[justgo-platform-software-requirements-specification#3.1. User Management and Authentication|3.1 User Management and Authentication]]

- Authorization: FR-01 to FR-04
- Accounts: FR-05 to FR-16
- Cache Invalidation: FR-17 to FR-22
- Files: FR-23 to FR-36
- Lookup: FR-37 to FR-41
- Multi-Factor Authentication: FR-42 to FR-56
- Notes: FR-57 to FR-62
- System Settings: FR-63
- Tenants: FR-64 to FR-71
- User Interface Permissions: FR-72 to FR-74
- Users: FR-75 to FR-80

### [[justgo-platform-software-requirements-specification#3.2. Member Profile Management|3.2 Member Profile Management]]

- Address Pickers: FR-81
- Member Basic Details: FR-82 to FR-86
- Member Family: FR-87 to FR-95
- Member Notes: FR-96 to FR-99
- Members: FR-100 to FR-108
- Preferences: FR-109 to FR-114
- User Emergency Contacts: FR-115 to FR-120

### [[justgo-platform-software-requirements-specification#3.3. Organisation and Club Management|3.3 Organisation and Club Management]]

- Organisations: FR-121 to FR-129

### [[justgo-platform-software-requirements-specification#3.4. Membership Management|3.4 Membership Management]]

- Memberships: FR-130 to FR-136
- Memberships Purchase: FR-137 to FR-142

### [[justgo-platform-software-requirements-specification#3.5. Booking Management|3.5 Booking Management]]

- Booking Catalog: FR-143 to FR-148
- Booking Class: FR-149 to FR-159
- Booking Pricing Chart Discount: FR-160 to FR-165
- Booking Transfer Request: FR-166
- Class Management: FR-167 to FR-168
- Class Term: FR-169 to FR-170
- Profile Class Booking: FR-171 to FR-172
- Profile Course Booking: FR-173 to FR-175

### [[justgo-platform-software-requirements-specification#3.6. Asset Management|3.6 Asset Management]]

- Asset Audit: FR-176
- Asset Categories: FR-177
- Asset Checkout: FR-178 to FR-179
- Asset Credentials: FR-180 to FR-185
- Asset Leases: FR-186 to FR-195
- Asset Licenses: FR-196 to FR-209
- Asset Metadata: FR-210 to FR-220
- Asset Ownership Transfers: FR-221 to FR-227
- Asset Registers: FR-228 to FR-240
- Asset Reports: FR-241 to FR-242
- Asset Types: FR-243 to FR-244
- Clubs: FR-245 to FR-249
- Workflows: FR-250

### [[justgo-platform-software-requirements-specification#3.7. Credential Management|3.7 Credential Management]]

- Credentials: FR-251 to FR-252

### [[justgo-platform-software-requirements-specification#3.8. Field and Preference Management|3.8 Field and Preference Management]]

- Entity Extensions: FR-253 to FR-270

### [[justgo-platform-software-requirements-specification#3.9. Finance and Payment Management|3.9 Finance and Payment Management]]

- Balances: FR-271 to FR-272
- Finance Grid View: FR-273 to FR-278
- Installments: FR-279 to FR-287
- Payment Account: FR-288 to FR-290
- Payment Console: FR-291 to FR-297
- Payments: FR-298 to FR-326
- Products: FR-327
- Subscriptions: FR-328 to FR-335

### [[justgo-platform-software-requirements-specification#3.10. Mobile Application Services|3.10 Mobile Application Services]]

- Classes: FR-336 to FR-382
- Clubs: FR-383 to FR-386
- Events: FR-387 to FR-399
- General Settings: FR-400 to FR-405
- Multi-Factor Authentication: FR-406 to FR-420
- Two-Factor Authentication: FR-421 to FR-435

### [[justgo-platform-software-requirements-specification#3.11. Results and Competition Management|3.11 Results and Competition Management]]

- Events: FR-436 to FR-458
- Results: FR-459 to FR-478
- Sports Results: FR-479 to FR-487
- Upload Result: FR-488 to FR-502

## Cross-Cutting Functional Themes

- Access control and tenant isolation appear throughout protected workflows.
- Most feature areas support create, view, update, remove, search/filter, validation, status change, upload/download, or export operations as applicable.
- Data integrity is emphasized through validation outcomes, traceability, authorization checks, and downstream workflow availability.
- Mobile services mirror core platform capabilities for classes, events, clubs, security, and operational workflows.
